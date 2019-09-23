using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Sempiler.AST;
using Sempiler.AST.Diagnostics;
using static Sempiler.AST.Diagnostics.DiagnosticsHelpers;
using Sempiler.Diagnostics;
using static Sempiler.Diagnostics.DiagnosticsHelpers;
using Sempiler.Emission;
using Sempiler.Languages;
using Sempiler.Core;
using Sempiler.Inlining;

namespace Sempiler.Bundler
{
    using static BundlerHelpers;


    public class IOSBundler : IBundler
    {
        static readonly string[] DiagnosticTags = new string[] { "bundler", "ios" };

        const string InlinedAppFileName = "App";
        const string EntrypointSymbolName = "_____MAIN_____";

        // [dho] NOTE INCOMPLETE! TODO finish implementation of non inlined output - 31/08/19
        // const bool PerformInlining = true;

        public async Task<Result<OutFileCollection>> Bundle(Session session, Artifact artifact, RawAST ast, CancellationToken token)
        {
            var result = new Result<OutFileCollection>();

            if (artifact.Role != ArtifactRole.Client)
            {
                result.AddMessages(
                    new Message(MessageKind.Error,
                        $"No bundler exists for target role '{artifact.Role.ToString()}' and target platform '{artifact.TargetPlatform}' (specified in artifact '{artifact.Name}')")
                );

                return result;
            }


            var ofc = new OutFileCollection();

            var relResourcePaths = result.AddMessages(AddResourceFiles(session, artifact, ofc, "Resources/"));


            var emittedFiles = default(OutFileCollection);
            // var mainActivity = default(ObjectTypeDeclaration);

            // [dho] emit source files - 21/05/19
            {
                var emitter = default(IEmitter);

                if (artifact.TargetLang == ArtifactTargetLang.Swift)
                {
                    result.AddMessages(SwiftInlining(session, artifact, ast, token));

                    if (HasErrors(result) || token.IsCancellationRequested) return result;

                    emitter = new SwiftEmitter();

                    emittedFiles = result.AddMessages(CompilerHelpers.Emit(emitter, session, artifact, ast, token));

                    foreach (var emittedFile in emittedFiles)
                    {
                        ofc[FileSystem.ParseFileLocation($"./{artifact.Name}/{emittedFile.Path}")] = emittedFile.Emission;
                    }
                }
                else
                {
                    result.AddMessages(
                        new Message(MessageKind.Error,
                            $"No bundler exists for target role '{artifact.Role.ToString()}' and target platform '{artifact.TargetPlatform}' (specified in artifact '{artifact.Name}')")
                    );
                }

                if (HasErrors(result) || token.IsCancellationRequested) return result;
            }

            // [dho] synthesize any requisite files for the target platform - 25/06/19
            {
                // [dho] create each file we need to compile for iOS, and add a reference to it
                // in the target app, so that the membership is set correctly and the file gets included
                // in the build by xcode etc. - 01/07/19
                var initRBContent = new System.Text.StringBuilder();
                {
                    foreach (var emittedFile in emittedFiles)
                    {
                        initRBContent.Append($"target.source_build_phase.add_file_reference(src.new_file('./{artifact.Name}/{emittedFile.Path}'))");
                        initRBContent.AppendLine();
                    }

                    foreach (var relResourcePath in relResourcePaths)
                    {
                        // [dho] TODO CLEANUP make into one call to `add_resources` with a populated array - 19/07/19
                        initRBContent.Append($"target.add_resources([res.new_file('{relResourcePath}')])");
                        initRBContent.AppendLine();
                    }
                }

                var entitlementsRBContent = new System.Text.StringBuilder();
                var entitlementsPListContent = new System.Text.StringBuilder();
                {
                    foreach (var entitlement in session.Entitlements[artifact.Name])
                    {
                        entitlementsRBContent.Append($"'{entitlement.Name}' => {{'enabled' => 1}},");

                        entitlementsPListContent.Append($"<key>{entitlement.Name}</key>");
                        entitlementsPListContent.AppendLine();

                        switch(entitlement.Type)
                        {
                            case EntitlementType.String:{
                                System.Diagnostics.Debug.Assert(entitlement.Values.Length == 1);
                                entitlementsPListContent.Append($"<string>{entitlement.Values[0]}</string>");
                                entitlementsPListContent.AppendLine();
                            }
                            break;

                            case EntitlementType.StringArray:{
                                entitlementsPListContent.Append($"<array>");
                                entitlementsPListContent.AppendLine();
                                foreach(var value in entitlement.Values)
                                {
                                    entitlementsPListContent.Append($"<string>{value}</string>");
                                    entitlementsPListContent.AppendLine();
                                }
                                entitlementsPListContent.Append($"</array>");
                                entitlementsPListContent.AppendLine();
                            }
                            break;

                            default:{
                                result.AddMessages(
                                    new Message(MessageKind.Error, $"Unhandled Entitlement Type for '{((EntitlementType)entitlement.Type).ToString()}' in IOS Bundler" )
                                );
                            }
                            break;
                        }
                    }
                }


                // [dho] entitlements adapted from : https://stackoverflow.com/questions/40673116/ionic-cordova-how-to-add-push-capability-with-fastlane-or-xcodebuild - 15/09/19
                AddRawFileIfMissing(ofc, $"init.rb",
$@"
# try to install xcodeproj if missing
`gem list '^xcodeproj$' -i || sudo gem install xcodeproj`
require 'xcodeproj'

# create project from scratch
project = Xcodeproj::Project.new('./{artifact.Name}.xcodeproj')

# target represents the app artifact being produced by the build
target = project.new_target(:application, '{artifact.Name}', :ios, nil, nil, :swift)

# entitlements inject adapted from 
# entitlement_path = '{artifact.Name}/{artifact.Name}.entitlements'

# file = project.new_file(entitlement_path)


attributes = {{}}
project.targets.each do |target|
    attributes[target.uuid] = {{'SystemCapabilities' => {{ {entitlementsRBContent.ToString()} }} }}
    # target.add_file_references([file])
    puts 'Added to target: ' + target.uuid
end
project.root_object.attributes['TargetAttributes'] = attributes


# grouping the emitted files under a folder with the same name as artifact
src = project.new_group('{artifact.Name}')

res = src.new_group('Resources')


# Note Info.plist is not included in target, but is pointed to by build configuration for target instead
src.new_file('./{artifact.Name}/Info.plist')

src.new_file('./{artifact.Name}/Entitlements.plist')


{initRBContent.ToString() /* include all the emitted files */}


target.build_configurations.each do |config|
    # Make sure the Info.plist is configured for all configs in the target
    config.build_settings['INFOPLIST_FILE'] = './{artifact.Name}/Info.plist'
    config.build_settings['PRODUCT_BUNDLE_IDENTIFIER'] = '{EmittedPackageName}'
    config.build_settings['CODE_SIGN_ENTITLEMENTS'] = './{artifact.Name}/Entitlements.plist'
end

project.save()

`pod install`");

                var podfileContent = new System.Text.StringBuilder();
                {
                    foreach (var dependency in session.Dependencies[artifact.Name])
                    {
                        podfileContent.Append($"pod '{dependency.Name}'");

                        if (dependency.Version != null)
                        {
                            podfileContent.Append($", '~> {dependency.Version}'");
                        }

                        podfileContent.AppendLine();
                    }
                }


                AddRawFileIfMissing(ofc, $"Podfile",
$@"source 'https://cdn.cocoapods.org/'

use_frameworks!

target '{artifact.Name}' do
  platform :ios, '8.0'
  {podfileContent.ToString()}
end");

                var permissionPListContent = new System.Text.StringBuilder();
                {
                    foreach (var permission in session.Permissions[artifact.Name])
                    {
                        permissionPListContent.Append($"<key>{permission.Name}</key>");
                        permissionPListContent.AppendLine();

                        permissionPListContent.Append($"<string>{permission.Description}</string>");
                        permissionPListContent.AppendLine();
                    }
                }


                AddRawFileIfMissing(ofc, $"./{artifact.Name}/Info.plist",
$@"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
<dict>
	<key>CFBundleDevelopmentRegion</key>
	<string>$(DEVELOPMENT_LANGUAGE)</string>
	<key>CFBundleExecutable</key>
	<string>$(EXECUTABLE_NAME)</string>
	<key>CFBundleIdentifier</key>
	<string>$(PRODUCT_BUNDLE_IDENTIFIER)</string>
	<key>CFBundleInfoDictionaryVersion</key>
	<string>6.0</string>
	<key>CFBundleName</key>
	<string>$(PRODUCT_NAME)</string>
	<key>CFBundlePackageType</key>
	<string>$(PRODUCT_BUNDLE_PACKAGE_TYPE)</string>
	<key>CFBundleShortVersionString</key>
	<string>1.0</string>
	<key>CFBundleVersion</key>
	<string>1</string>
	<key>LSRequiresIPhoneOS</key>
	<true/>
	<key>UIApplicationSceneManifest</key>
	<dict>
		<key>UIApplicationSupportsMultipleScenes</key>
		<false/>
		<key>UISceneConfigurations</key>
		<dict>
			<key>UIWindowSceneSessionRoleApplication</key>
			<array>
				<dict>
					<key>UILaunchStoryboardName</key>
					<string>LaunchScreen</string>
					<key>UISceneConfigurationName</key>
					<string>Default Configuration</string>
					<key>UISceneDelegateClassName</key>
					<string>$(PRODUCT_MODULE_NAME).SceneDelegate</string>
				</dict>
			</array>
		</dict>
	</dict>
	<key>UILaunchStoryboardName</key>
	<string>LaunchScreen</string>
	<key>UIRequiredDeviceCapabilities</key>
	<array>
		<string>armv7</string>
	</array>
	<key>UISupportedInterfaceOrientations</key>
	<array>
		<string>UIInterfaceOrientationPortrait</string>
		<string>UIInterfaceOrientationLandscapeLeft</string>
		<string>UIInterfaceOrientationLandscapeRight</string>
	</array>
	<key>UISupportedInterfaceOrientations~ipad</key>
	<array>
		<string>UIInterfaceOrientationPortrait</string>
		<string>UIInterfaceOrientationPortraitUpsideDown</string>
		<string>UIInterfaceOrientationLandscapeLeft</string>
		<string>UIInterfaceOrientationLandscapeRight</string>
	</array>
    {permissionPListContent.ToString()}
</dict>
</plist>");


                AddRawFileIfMissing(ofc, $"./{artifact.Name}/Entitlements.plist",
$@"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
<dict>
    {entitlementsPListContent.ToString()}
</dict>
</plist>");

                result.Value = ofc;
            }


            return result;
        }


        private static Result<object> SwiftInlining(Session session, Artifact artifact, RawAST ast, CancellationToken token)
        {
            var result = new Result<object>();

            var root = ASTHelpers.GetRoot(ast);

            System.Diagnostics.Debug.Assert(root?.Kind == SemanticKind.Domain);

            var domain = ASTNodeFactory.Domain(ast, root);

            var newComponentNodes = new List<Node>();


            var topLevelExpressions = new List<Node>();

            // [dho] the component (eg. file) that contains the entrypoint view for the application - 31/08/19
            var entrypointComponent = default(Component);

            // [dho] the first view to be rendered for the application - 31/08/19
            // var entrypointView = default(ObjectTypeDeclaration);

            // if (PerformInlining)
            // {
                // [dho] a component containing all the inlined constituent components for the compilation,
                // thereby creating a single larger file of all components in one file - 31/08/19
                var inlined = NodeFactory.Component(ast, new PhaseNodeOrigin(PhaseKind.Bundling), InlinedAppFileName);


                var importDecls = new List<Node>();
                // [dho] these statements live outside of the `node1234` object type declaration
                // wrapper that we emit around inlined components - 31/08/19
                var globalStatements = new List<Node>();
                {

                    importDecls.Add(
                        NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Transformation), "import SwiftUI").Node
                    );
                    importDecls.Add(
                        NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Transformation), "import UIKit").Node
                    );

                    var inlinedObjectTypeDecls = new List<Node>();
                    var componentIDsToRemove = new List<string>();

                    foreach (var cNode in domain.Components)
                    {
                        var component = ASTNodeFactory.Component(ast, (DataNode<string>)cNode);

                        // [dho] every component in the AST (ie. every input file) will be turned into a class and inlined - 28/06/19
                        var r = ConvertToInlinedObjectTypeDeclaration(session, artifact, ast, component, token, ref importDecls, ref topLevelExpressions);

                        result.AddMessages(r);

                        if (HasErrors(r))
                        {
                            continue;
                        }

                        // [dho] is this component the entrypoint for the whole artifact - 28/06/19
                        if (BundlerHelpers.IsInferredArtifactEntrypointComponent(session, artifact, component))
                        {
                            // [dho] the SceneDelegate will use this information to wire up the entrypoint - 28/06/19
                            entrypointComponent = component;
                            // entrypointView = r.Value;
                        }
                        // [dho] any code that was outside an artifact root is just emitted without a class wrapper, so we have a way
                        // in the input sources of declaring global symbols, or things like protocols which cannot be nested inside other
                        // declarations in Swift - 18/07/19
                        else if (BundlerHelpers.IsOutsideArtifactInferredSourceDir(session, component))
                        {
                            globalStatements.AddRange(r.Value.Members);
                        }

                        inlinedObjectTypeDecls.Add(r.Value.Node);

                        componentIDsToRemove.Add(component.ID);
                    }

                    if(entrypointComponent == null)
                    {
                        result.AddMessages(
                            new Message(MessageKind.Error, $"Could not create iOS bundle because an entrypoint component was not found {artifact.Name} (expected '{BundlerHelpers.GetNameOfExpectedArtifactEntrypointComponent(session, artifact)}' to exist)")
                        );
                    }

                    if (HasErrors(result) || token.IsCancellationRequested) return result;

                    // [dho] combine the imports - 01/06/19

                    // ASTHelpers.Connect(ast, inlined.ID, importDecls.ToArray(), SemanticRole.None, 0);

                    if (globalStatements.Count > 0)
                    {
                        ASTHelpers.Connect(ast, inlined.ID, globalStatements.ToArray(), SemanticRole.None);
                    }

                    // [dho] inline all the existing components as static classes - 01/06/19
                    ASTHelpers.Connect(ast, inlined.ID, inlinedObjectTypeDecls.ToArray(), SemanticRole.None);

                    // [dho] remove the components from the tree because now they have all been inlined - 01/06/19
                    ASTHelpers.RemoveNodes(ast, componentIDsToRemove.ToArray());
                }

                newComponentNodes.Add(inlined.Node);
            // }
            // else
            // {
            //     foreach (var cNode in domain.Components)
            //     {
            //         var component = ASTNodeFactory.Component(ast, (DataNode<string>)cNode);

            //         ASTHelpers.Connect(ast, component.ID, new[] {
            //             NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Transformation), "import SwiftUI").Node
            //         }, SemanticRole.None, 0 /* [dho] make this the first statement in the component - 31/08/19 */);

            //         // [dho] is this component the entrypoint for the whole artifact - 28/06/19
            //         if (BundlerHelpers.IsInferredArtifactEntrypointComponent(session, artifact, component))
            //         {
            //             // [dho] the SceneDelegate will use this information to wire up the entrypoint - 28/06/19
            //             entrypointComponent = component;
            //             break;
            //         }
            //     }
            // }

            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // var appDelegate = NodeFactory.Component(ast, new PhaseNodeOrigin(PhaseKind.Bundling), "AppDelegate");
            // {
                // [dho] standard imports - 25/06/19
                // ASTHelpers.Connect(ast, appDelegate.ID, new[] {
                //     NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Bundling), @"import UIKit").Node
                // }, SemanticRole.None);

                var appDelegateClass = NodeFactory.ObjectTypeDeclaration(ast, new PhaseNodeOrigin(PhaseKind.Bundling));
                {
                    ASTHelpers.Connect(ast, appDelegateClass.ID, new[] {
                        NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Bundling), "AppDelegate").Node
                    }, SemanticRole.Name);

                    // [dho] add the entrypoint annotation `@UIApplicationMain` - 02/07/19
                    {
                        var uiApplicationMainAnnotation = NodeFactory.Annotation(ast, new PhaseNodeOrigin(PhaseKind.Bundling));
                        var uiApplicationMainOperand = NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Bundling), "UIApplicationMain");

                        ASTHelpers.Connect(ast, uiApplicationMainAnnotation.ID, new[] { uiApplicationMainOperand.Node }, SemanticRole.Operand);

                        ASTHelpers.Connect(ast, appDelegateClass.ID, new[] { uiApplicationMainAnnotation.Node }, SemanticRole.Annotation);
                    }

                    // [dho] app delegate interfaces - 25/06/19
                    {
                        var uiResponder = NodeFactory.NamedTypeReference(ast, new PhaseNodeOrigin(PhaseKind.Bundling));
                        {
                            ASTHelpers.Connect(ast, uiResponder.ID, new[] {
                                NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Bundling), "UIResponder").Node
                            }, SemanticRole.Name);

                            ASTHelpers.Connect(ast, appDelegateClass.ID, new[] { uiResponder.Node }, SemanticRole.Interface);
                        }

                        var uiApplicationDelegate = NodeFactory.NamedTypeReference(ast, new PhaseNodeOrigin(PhaseKind.Bundling));
                        {
                            ASTHelpers.Connect(ast, uiApplicationDelegate.ID, new[] {
                                NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Bundling), "UIApplicationDelegate").Node
                            }, SemanticRole.Name);

                            ASTHelpers.Connect(ast, appDelegateClass.ID, new[] { uiApplicationDelegate.Node }, SemanticRole.Interface);
                        }
                    }

                    var applicationFn = NodeFactory.MethodDeclaration(ast, new PhaseNodeOrigin(PhaseKind.Bundling));
                    {
                        ASTHelpers.Connect(ast, applicationFn.ID, new[] {
                            NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Bundling), "application").Node
                        }, SemanticRole.Name);

                        ASTHelpers.Connect(ast, applicationFn.ID, new[] { 
                            // [dho] cheating with the parameters because I'm feeling very lazy tonight - 25/06/19
                            NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Bundling),
                                "_ application: UIApplication, didFinishLaunchingWithOptions launchOptions: [UIApplication.LaunchOptionsKey: Any]?").Node
                        }, SemanticRole.Parameter);

                        var returnType = NodeFactory.NamedTypeReference(ast, new PhaseNodeOrigin(PhaseKind.Bundling));
                        {
                            ASTHelpers.Connect(ast, returnType.ID, new[] {
                                NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Bundling), "Bool").Node
                            }, SemanticRole.Name);

                            ASTHelpers.Connect(ast, applicationFn.ID, new[] { returnType.Node }, SemanticRole.Type);
                        }


                        topLevelExpressions.Add(
                            NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Bundling), @"return true").Node
                        );

                        var body = NodeFactory.Block(ast, new PhaseNodeOrigin(PhaseKind.Bundling));
                        {
                            ASTHelpers.Connect(ast, body.ID, topLevelExpressions.ToArray(), SemanticRole.Content);
                        }
                        ASTHelpers.Connect(ast, applicationFn.ID, new[] { body.Node }, SemanticRole.Body);
                    }


                    ASTHelpers.Connect(ast, appDelegateClass.ID, new[] {

                        applicationFn.Node,

                        // [dho] just stuffing in the other functions to satisfy the protocols.. we don't
                        // use them ...yet - 25/06/19
                        NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Bundling),
$@"func applicationWillTerminate(_ application: UIApplication) {{
    // Called when the application is about to terminate. Save data if appropriate. See also applicationDidEnterBackground:.
}}

// MARK: UISceneSession Lifecycle
func application(_ application: UIApplication, configurationForConnecting connectingSceneSession: UISceneSession, options: UIScene.ConnectionOptions) -> UISceneConfiguration {{
    // Called when a new scene session is being created.
    // Use this method to select a configuration to create the new scene with.
    return UISceneConfiguration(name: ""Default Configuration"", sessionRole: connectingSceneSession.role)
}}

func application(_ application: UIApplication, didDiscardSceneSessions sceneSessions: Set<UISceneSession>) {{
    // Called when the user discards a scene session.
    // If any sessions were discarded while the application was not running, this will be called shortly after application:didFinishLaunchingWithOptions.
    // Use this method to release any resources that were specific to the discarded scenes, as they will not return.
}}").Node
                    }, SemanticRole.Member);
                }

            //     ASTHelpers.Connect(ast, appDelegate.ID, new[] { appDelegateClass.Node }, SemanticRole.None);
            // }

            // newComponentNodes.Add(appDelegate.Node);
            
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            
            // var sceneDelegate = NodeFactory.Component(ast, new PhaseNodeOrigin(PhaseKind.Bundling), "SceneDelegate");
            // {
                // [dho] standard imports - 25/06/19
//                 ASTHelpers.Connect(ast, sceneDelegate.ID, new[] {
//                     NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Bundling),
// @"import UIKit
// import SwiftUI").Node
//                 }, SemanticRole.None);

                var sceneDelegateClass = NodeFactory.ObjectTypeDeclaration(ast, new PhaseNodeOrigin(PhaseKind.Bundling));
                {
                    ASTHelpers.Connect(ast, sceneDelegateClass.ID, new[] {
                        NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Bundling), "SceneDelegate").Node
                    }, SemanticRole.Name);

                    // [dho] scene delegate interfaces - 25/06/19
                    {
                        var uiResponder = NodeFactory.NamedTypeReference(ast, new PhaseNodeOrigin(PhaseKind.Bundling));
                        {
                            ASTHelpers.Connect(ast, uiResponder.ID, new[] {
                                NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Bundling), "UIResponder").Node
                            }, SemanticRole.Name);

                            ASTHelpers.Connect(ast, sceneDelegateClass.ID, new[] { uiResponder.Node }, SemanticRole.Interface);
                        }

                        var uiWindowSceneDelegate = NodeFactory.NamedTypeReference(ast, new PhaseNodeOrigin(PhaseKind.Bundling));
                        {
                            ASTHelpers.Connect(ast, uiWindowSceneDelegate.ID, new[] {
                                NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Bundling), "UIWindowSceneDelegate").Node
                            }, SemanticRole.Name);

                            ASTHelpers.Connect(ast, sceneDelegateClass.ID, new[] { uiWindowSceneDelegate.Node }, SemanticRole.Interface);
                        }
                    }

                    var windowField = NodeFactory.FieldDeclaration(ast, new PhaseNodeOrigin(PhaseKind.Bundling));
                    {
                        var maybeNullFlag = NodeFactory.Meta(
                            ast,
                            new PhaseNodeOrigin(PhaseKind.Bundling),
                            MetaFlag.Optional
                        );

                        ASTHelpers.Connect(ast, windowField.ID, new[] { maybeNullFlag.Node }, SemanticRole.Meta);

                        ASTHelpers.Connect(ast, windowField.ID, new[] {
                            NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Bundling), "window").Node
                        }, SemanticRole.Name);

                        var windowFieldType = NodeFactory.NamedTypeReference(ast, new PhaseNodeOrigin(PhaseKind.Bundling));
                        {
                            ASTHelpers.Connect(ast, windowFieldType.ID, new[] {
                                NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Bundling), "UIWindow").Node
                            }, SemanticRole.Name);

                            ASTHelpers.Connect(ast, windowField.ID, new[] { windowFieldType.Node }, SemanticRole.Type);
                        }
                    }

                    var sceneFn = NodeFactory.MethodDeclaration(ast, new PhaseNodeOrigin(PhaseKind.Bundling));
                    {
                        ASTHelpers.Connect(ast, sceneFn.ID, new[] {
                            NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Bundling), "scene").Node
                        }, SemanticRole.Name);

                        ASTHelpers.Connect(ast, sceneFn.ID, new[] { 
                            // [dho] cheating with the parameters because I'm feeling very lazy tonight - 25/06/19
                            NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Bundling),
                                "_ scene: UIScene, willConnectTo session: UISceneSession, options connectionOptions: UIScene.ConnectionOptions").Node
                        }, SemanticRole.Parameter);


                        var body = NodeFactory.Block(ast, new PhaseNodeOrigin(PhaseKind.Bundling));
                        {
                            ASTHelpers.Connect(ast, body.ID, new[] {
                                NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Bundling),
    $@"if let windowScene = scene as? UIWindowScene {{
            let window = UIWindow(windowScene: windowScene)
            window.rootViewController = UIHostingController(rootView: {ToInlinedObjectTypeClassIdentifier(ast, entrypointComponent.Node)}.{EntrypointSymbolName}())
            self.window = window
            window.makeKeyAndVisible()
        }}").Node

                            }, SemanticRole.Content);
                        }
                        ASTHelpers.Connect(ast, sceneFn.ID, new[] { body.Node }, SemanticRole.Body);
                    }

                    ASTHelpers.Connect(ast, sceneDelegateClass.ID, new[] {

                        windowField.Node,
                        sceneFn.Node,

                        // [dho] just stuffing in the other functions to satisfy the protocols.. we don't
                        // use them ...yet - 25/06/19
                        NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Bundling),
$@"func sceneDidDisconnect(_ scene: UIScene) {{
    // Called as the scene is being released by the system.
    // This occurs shortly after the scene enters the background, or when its session is discarded.
    // Release any resources associated with this scene that can be re-created the next time the scene connects.
    // The scene may re-connect later, as its session was not neccessarily discarded (see `application:didDiscardSceneSessions` instead).
}}

func sceneDidBecomeActive(_ scene: UIScene) {{
    // Called when the scene has moved from an inactive state to an active state.
    // Use this method to restart any tasks that were paused (or not yet started) when the scene was inactive.
}}

func sceneWillResignActive(_ scene: UIScene) {{
    // Called when the scene will move from an active state to an inactive state.
    // This may occur due to temporary interruptions (ex. an incoming phone call).
}}

func sceneWillEnterForeground(_ scene: UIScene) {{
    // Called as the scene transitions from the background to the foreground.
    // Use this method to undo the changes made on entering the background.
}}

func sceneDidEnterBackground(_ scene: UIScene) {{
    // Called as the scene transitions from the foreground to the background.
    // Use this method to save data, release shared resources, and store enough scene-specific state information
    // to restore the scene back to its current state.
}}").Node
                    }, SemanticRole.Member);
                }

                // ASTHelpers.Connect(ast, sceneDelegate.ID, new[] { sceneDelegateClass.Node }, SemanticRole.None);
            // }

            // newComponentNodes.Add(sceneDelegate.Node);


            ASTHelpers.Connect(ast, inlined.ID, new[] { 
                appDelegateClass.Node,
                sceneDelegateClass.Node 
            }, SemanticRole.None, 0);

            ASTHelpers.Connect(ast, inlined.ID, importDecls.ToArray(), SemanticRole.None, 0);


            ASTHelpers.Connect(ast, domain.ID, newComponentNodes.ToArray(), SemanticRole.Component);


            if (!HasErrors(result))
            {
                // [dho] use SwiftUI API in the AST - 29/06/19
                {
                    var task = new Sempiler.Transformation.IOSSwiftUITransformer().Transform(session, artifact, ast, token);

                    task.Wait();

                    var newAST = result.AddMessages(task.Result);

                    if (HasErrors(result) || token.IsCancellationRequested) return result;

                    if (newAST != ast)
                    {
                        result.AddMessages(
                            new Message(MessageKind.Error, "iOS SwiftUI Transformer unexpectedly returned a different AST that was discarded")
                        );
                    }
                }


                {
                    var task = new Sempiler.Transformation.SwiftNamedArgumentsTransformer().Transform(session, artifact, ast, token);

                    task.Wait();

                    var newAST = result.AddMessages(task.Result);

                    if (HasErrors(result) || token.IsCancellationRequested) return result;

                    if (newAST != ast)
                    {
                        result.AddMessages(new Message(MessageKind.Error, "Swift Named Arguments Transformer unexpectedly returned a different AST that was discarded"));
                    }
                }


                {
                    var task = new Sempiler.Transformation.SwiftEnforceMutatingMethodsTransformer().Transform(session, artifact, ast, token);

                    task.Wait();

                    var newAST = result.AddMessages(task.Result);

                    if (HasErrors(result) || token.IsCancellationRequested) return result;

                    if (newAST != ast)
                    {
                        result.AddMessages(new Message(MessageKind.Error, "Swift Enforce Mutating Methods Transformer unexpectedly returned a different AST that was discarded"));
                    }
                }

                // {
                //     var task = new Sempiler.Transformation.SwiftParameterTransformer().Transform(session, artifact, ast, token);

                //     task.Wait();

                //     var newAST = result.AddMessages(task.Result);

                //     if(HasErrors(result) || token.IsCancellationRequested) return result;

                //     if (newAST != ast)
                //     {
                //         result.AddMessages(new Message(MessageKind.Error, "Swift Parameter Transformer unexpectedly returned a different AST that was discarded"));
                //     }
                // }
            }


            return result;
        }

        private static Result<ObjectTypeDeclaration> ConvertToInlinedObjectTypeDeclaration(Session session, Artifact artifact, RawAST ast, Component component, CancellationToken token, ref List<Node> imports, ref List<Node> topLevelExpressions)
        {
            var result = new Result<ObjectTypeDeclaration>();

            var inlinedObjectTypeDecl = NodeFactory.ObjectTypeDeclaration(ast, new PhaseNodeOrigin(PhaseKind.Bundling));

            {
                var className = NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Bundling),
                    ToInlinedObjectTypeClassIdentifier(ast, component.Node));

                ASTHelpers.Connect(ast, inlinedObjectTypeDecl.ID, new[] { className.Node }, SemanticRole.Name);
            }


            {
                // [dho] make the class public - 29/06/19
                var publicFlag = NodeFactory.Meta(
                    ast,
                    new PhaseNodeOrigin(PhaseKind.Bundling),
                    MetaFlag.WorldVisibility
                );

                ASTHelpers.Connect(ast, inlinedObjectTypeDecl.ID, new[] { publicFlag.Node }, SemanticRole.Meta);
            }


            var prevTopLevelExpressionsCount = topLevelExpressions.Count;

            var members = result.AddMessages(
                GetInlinedMembers(session, artifact, ast, component, token, ref imports, ref topLevelExpressions)
            );
            
            // [dho] NOTE the session entrypoint component is emitted in TOP LEVEL scope in the artifact, so we allow top level expressions in that file
            // because we can stuff them into the `AppDelegate.application` 'main' function. But any top level expressions in files that are NOT
            // the entrypoint component are ILLEGAL for now - 15/09/19
            if (!BundlerHelpers.IsInferredSessionEntrypointComponent(session, component) && topLevelExpressions.Count > prevTopLevelExpressionsCount)
            {
                for(int i = prevTopLevelExpressionsCount; i < topLevelExpressions.Count; ++i)
                {
                    var node = topLevelExpressions[i];

                    // [dho] TODO add support - 29/06/19
                    result.AddMessages(
                        new NodeMessage(MessageKind.Error, $"Could not create bundle because top level expressions are not yet supported", node)
                        {
                            Hint = GetHint(node.Origin),
                            Tags = DiagnosticTags
                        }
                    );
                }
            }

            if (members != null)
            {
                ASTHelpers.Connect(ast, inlinedObjectTypeDecl.ID, members.ToArray(), SemanticRole.Member);
            }


            result.Value = inlinedObjectTypeDecl;

            return result;
        }

        private static Result<List<Node>> GetInlinedMembers(Session session, Artifact artifact, RawAST ast, Component component, CancellationToken token, ref List<Node> imports, ref List<Node> topLevelExpressions)
        {
            var result = new Result<List<Node>>();

            var members = new List<Node>();

            var inlinerInfo = result.AddMessages(
                ClientInlining.GetInlinerInfo(session, ast, component.Node, LanguageSemantics.Swift, token)
            );

            if (inlinerInfo.Entrypoint != null)
            {
                // [dho] extracts the code that should be executed in the body of the 
                // entrypoint function so it can be wrapped up in a different way - 31/08/19
                var userCodeBody = result.AddMessages(
                    ProcessEntrypoint(session, artifact, ast, component, inlinerInfo.Entrypoint, inlinerInfo.EntrypointUserCode, token)
                );

                if (HasErrors(result) || token.IsCancellationRequested) return result;

                var entrypointMethod = NodeFactory.MethodDeclaration(ast, inlinerInfo.Entrypoint.Origin);
                {
                    {
                        var publicFlag = NodeFactory.Meta(
                            ast,
                            new PhaseNodeOrigin(PhaseKind.Bundling),
                            MetaFlag.WorldVisibility
                        );

                        var staticFlag = NodeFactory.Meta(
                            ast,
                            new PhaseNodeOrigin(PhaseKind.Bundling),
                            MetaFlag.Static
                        );

                        ASTHelpers.Connect(ast, entrypointMethod.ID, new[] { publicFlag.Node, staticFlag.Node }, SemanticRole.Meta);
                    }

                    ASTHelpers.Connect(ast, entrypointMethod.ID, new[] {

                        NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), EntrypointSymbolName).Node

                    }, SemanticRole.Name);

                    var returnType = NodeFactory.NamedTypeReference(ast, new PhaseNodeOrigin(PhaseKind.Bundling));
                    {
                        ASTHelpers.Connect(ast, returnType.ID, new[] {
                            NodeFactory.Modifier(ast, new PhaseNodeOrigin(PhaseKind.Transformation), "some").Node
                        }, SemanticRole.Modifier);

                        ASTHelpers.Connect(ast, returnType.ID, new[] {
                            NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Bundling), "View").Node
                        }, SemanticRole.Name);

                        ASTHelpers.Connect(ast, entrypointMethod.ID, new[] { returnType.Node }, SemanticRole.Type);
                    }

                    ASTHelpers.Connect(ast, entrypointMethod.ID, new[] { userCodeBody }, SemanticRole.Body);
                }

                members.Add(entrypointMethod.Node);

            }


            result.AddMessages(
                ProcessImports(session, artifact, ast, component, inlinerInfo.ImportDeclarations, token, ref imports)
            );

            result.AddMessages(
                ProcessExports(session, artifact, ast, component, inlinerInfo.ExportedSymbols, token, ref members)
            );

            result.AddMessages(
                ProcessNamespaces(session, artifact, ast, component, inlinerInfo.NamespaceDeclarations, token)
            );

            if (inlinerInfo.ObjectTypeDeclarations?.Count > 0)
            {
                var objectTypes = new Node[inlinerInfo.ObjectTypeDeclarations.Count];

                for (int i = 0; i < objectTypes.Length; ++i) objectTypes[i] = inlinerInfo.ObjectTypeDeclarations[i].Node;

                members.AddRange(objectTypes);
            }

            if (inlinerInfo.ViewDeclarations?.Count > 0)
            {
                var viewDecls = new Node[inlinerInfo.ViewDeclarations.Count];

                for (int i = 0; i < viewDecls.Length; ++i) viewDecls[i] = inlinerInfo.ViewDeclarations[i].Node;

                members.AddRange(viewDecls);
            }


            if (inlinerInfo.FunctionDeclarations?.Count > 0)
            {
                var fnDecls = new Node[inlinerInfo.FunctionDeclarations.Count];

                for (int i = 0; i < fnDecls.Length; ++i)
                {
                    var methodDecl = result.AddMessages(
                        BundlerHelpers.ConvertToStaticMethodDeclaration(session, ast, inlinerInfo.FunctionDeclarations[i], token)
                    );

                    // [dho] guard against case when conversion has errored - 29/06/19
                    if (methodDecl != null)
                    {
                        fnDecls[i] = methodDecl.Node;
                    }
                }

                members.AddRange(fnDecls);
            }


            // [dho] NOTE doing the exec on loads last in case they depend on invoking one of the functions or 
            // classes - which we make sure we put above these statements, rather than waste effort on rejigging
            // the statements in to order - 14/07/19
            result.AddMessages(
                ProcessExecOnLoads(session, artifact, ast, component, inlinerInfo.ExecOnLoads, token, ref members, ref topLevelExpressions)
            );

            result.Value = members;

            return result;
        }

        private static Result<object> ProcessExports(Session session, Artifact artifact, RawAST ast, Component component, List<ExportDeclaration> exportedSymbols, CancellationToken token, ref List<Node> exports)
        {
            var result = new Result<object>();

            if (exportedSymbols?.Count > 0)
            {
                // [dho] TODO make each symbol public - 29/06/19
                foreach (var exportedSymbol in exportedSymbols)
                {
                    var clauses = exportedSymbol.Clauses;
                    var specifier = exportedSymbol.Specifier;

                    if (specifier != null)
                    {
                        result.AddMessages(
                            CreateUnsupportedFeatureResult(specifier, "Export specifiers")
                        );
                    }

                    if (clauses.Length == 1)
                    {
                        var clause = clauses[0];

                        if (LanguageSemantics.Swift.IsDeclarationStatement(ast, clause))
                        {
                            // [dho] make declaration public - 30/06/19
                            var publicFlag = NodeFactory.Meta(
                                ast,
                                new PhaseNodeOrigin(PhaseKind.Bundling),
                                MetaFlag.WorldVisibility
                            );

                            ASTHelpers.Connect(ast, clause.ID, new[] { publicFlag.Node }, SemanticRole.Meta);

                            exports.Add(clause);
                        }
                        else
                        {
                            result.AddMessages(
                                new NodeMessage(MessageKind.Error, $"Expected export clause to be declaration but found '{clause.Kind}'", clause)
                                {
                                    Hint = GetHint(clause.Origin),
                                    Tags = DiagnosticTags
                                }
                            );
                        }
                    }
                    else
                    {
                        result.AddMessages(
                            CreateUnsupportedFeatureResult(clauses, "Multiple export clauses")
                        );
                    }
                }
            }

            return result;
        }

        private static Result<object> ProcessNamespaces(Session session, Artifact artifact, RawAST ast, Component component, List<NamespaceDeclaration> namespaceDecls, CancellationToken token)
        {
            var result = new Result<object>();

            if (namespaceDecls?.Count > 0)
            {
                foreach (var namespaceDecl in namespaceDecls)
                {
                    result.AddMessages(
                        new NodeMessage(MessageKind.Error, $"Could not create bundle because namespaces are not yet supported", namespaceDecl.Node)
                        {
                            Hint = GetHint(namespaceDecl.Node.Origin),
                            Tags = DiagnosticTags
                        }
                    );
                }
            }

            return result;
        }

        private static Result<Node> ProcessEntrypoint(Session session, Artifact artifact, RawAST ast, Component component, Node entrypoint, Node entrypointUserCode, CancellationToken token)
        {
            var result = new Result<Node>();

            if (entrypoint != null)
            {
                if (BundlerHelpers.IsInferredArtifactEntrypointComponent(session, artifact, component))
                {
                    // [dho] TODO CLEANUP HACK to get function body!! - 29/06/19
                    var userCodeBody = ASTHelpers.GetSingleMatch(ast, entrypointUserCode.ID, SemanticRole.Body);

                    foreach (var explicitExit in LanguageSemantics.Swift.GetExplicitExits(session, ast, userCodeBody, token))
                    {
                        var exitValue = default(Node);

                        // [dho] the entrypoint has to return something that can be rendered - 29/06/19
                        if (explicitExit.Kind == SemanticKind.FunctionTermination)
                        {
                            exitValue = ASTNodeFactory.FunctionTermination(ast, explicitExit).Value;
                        }
                        else if (explicitExit.Kind == SemanticKind.ViewConstruction)
                        {
                            // [dho] assert that we are dealing with a `() => <X>...</X>` situation - 21/06/19
                            System.Diagnostics.Debug.Assert(entrypointUserCode.Kind == SemanticKind.LambdaDeclaration);

                            continue;
                        }
                        else
                        {
                            exitValue = explicitExit;
                        }


                        if (exitValue?.Kind != SemanticKind.ViewConstruction)
                        {
                            // [dho] TODO if(typeOfExpression(returnValue, SemanticKind.ViewConstruction)) - 16/06/19
                            result.AddMessages(
                                new NodeMessage(MessageKind.Error, $"Expected entrypoint return value to be view construction but found '{exitValue.Kind}'", exitValue)
                                {
                                    Hint = GetHint(exitValue.Origin),
                                    Tags = DiagnosticTags
                                }
                            );
                        }
                    }

                    result.Value = userCodeBody;
                }
                else
                {
                    result.AddMessages(
                        new NodeMessage(MessageKind.Error, $"Entrypoint is only supported in Artifact entry file", entrypoint)
                        {
                            Hint = GetHint(entrypoint.Origin),
                            Tags = DiagnosticTags
                        }
                    );
                }
            }

            return result;
        }

        private static Result<object> ProcessImports(Session session, Artifact artifact, RawAST ast, Component component, List<Node> importDeclarations, CancellationToken token, ref List<Node> imports)
        {
            var result = new Result<object>();

            if (importDeclarations?.Count > 0)
            {
                var importsSortedByType = result.AddMessages(
                    ImportHelpers.SortImportDeclarationsByType(session, artifact, ast, component, importDeclarations, LanguageSemantics.Swift, token)
                );

                if (!HasErrors(result) && !token.IsCancellationRequested)
                {
                    foreach (var im in importsSortedByType.SempilerImports)
                    {
                        // [dho] CHECK do we need to do any renaming here though?? eg. from sempiler names like `View`
                        // to SwiftUI names like `VStack`?? - 11/07/19

                        // [dho] remove the "sempiler" import because it is a _fake_
                        // import we just use to be sure that the symbols the user refers
                        // to are for sempiler, and not something in global scope for a particular target platform - 24/06/19
                        ASTHelpers.RemoveNodes(ast, new[] { im.ImportDeclaration.ID });
                    }

                    foreach (var im in importsSortedByType.ComponentImports)
                    {
                        var importedComponentInlinedName = ToInlinedObjectTypeClassIdentifier(ast, im.Component.Node);

                        result.AddMessages(
                            ImportHelpers.QualifyImportReferences(ast, im, importedComponentInlinedName)
                        );

                        // [dho] remove the import because all components are inlined into the same output file - 24/06/19
                        ASTHelpers.RemoveNodes(ast, new[] { im.ImportDeclaration.ID });
                    }

                    foreach (var im in importsSortedByType.PlatformImports)
                    {
                        var specifier = im.ImportDeclaration.Specifier;

                        // [dho] unpack a string constant for the import specifier so it is 
                        // just the raw value of it, because in Swift imports are not wrapped in
                        // quotes - 01/06/19 (ported 29/06/19)
                        var newSpecifier = NodeFactory.CodeConstant(
                            ast,
                            specifier.Origin,
                            im.ImportInfo.SpecifierLexeme
                        );

                        ASTHelpers.Replace(ast, specifier.ID, new[] { newSpecifier.Node });

                        imports.Add(im.ImportDeclaration.Node);
                    }
                }
            }

            return result;
        }

        private static Result<object> ProcessExecOnLoads(Session session, Artifact artifact, RawAST ast, Component component, List<Node> execOnLoads, CancellationToken token, ref List<Node> topLevelStatements, ref List<Node> topLevelExpressions)
        {
            var result = new Result<object>();

            if (execOnLoads?.Count > 0)
            {
                // var topLevelStatements = new List<Node>();

                foreach (var execOnLoad in execOnLoads)
                {
                    if (LanguageSemantics.Swift.IsDeclarationStatement(ast, execOnLoad))
                    {
                        // [dho] need to make the declarations static at file level because we
                        // are putting them inside a class - 14/07/19
                        if ((MetaHelpers.ReduceFlags(ast, execOnLoad) & MetaFlag.Static) == 0)
                        {
                            var staticFlag = NodeFactory.Meta(
                                ast,
                                new PhaseNodeOrigin(PhaseKind.Bundling),
                                MetaFlag.Static
                            );

                            ASTHelpers.Connect(ast, execOnLoad.ID, new[] { staticFlag.Node }, SemanticRole.Meta);
                        }

                        topLevelStatements.Add(execOnLoad);
                    }
                    else if (execOnLoad.Kind == SemanticKind.CodeConstant)
                    {
                        topLevelStatements.Add(execOnLoad);
                    }
                    else
                    {
                        topLevelExpressions.Add(execOnLoad);
                    }

                }
            }

            return result;
        }

        private static string ToInlinedObjectTypeClassIdentifier(RawAST ast, Node node)
        {
            var guid = ASTHelpers.GetRoot(ast).ID;

            var sb = new System.Text.StringBuilder(guid + "$$");

            foreach (byte b in System.Security.Cryptography.SHA256.Create().ComputeHash(System.Text.Encoding.UTF8.GetBytes(node.ID)))
            {
                sb.Append(b.ToString("X2"));
            }

            return sb.ToString();
        }


    }

}