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

        const string AppDelegateClassSymbolName = "AppDelegate";
        const string SceneDelegateClassSymbolName = "SceneDelegate";


        const string MainAppTargetName = "app";
        const string ProjectBundleIdentifier = "com.sempiler"; // [dho] TODO dynamic!! - 19/10/19

        // [dho] NOTE INCOMPLETE! TODO finish implementation of non inlined output - 31/08/19
        // const bool PerformInlining = true;

        public IList<string> GetPreservedDebugEmissionRelPaths() => new string[]{ "Pods" };

        public async Task<Result<OutFileCollection>> Bundle(Session session, Artifact artifact, List<Ancillary> ancillaries, CancellationToken token)
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

            var ofc = result.AddMessages(
                SupportingFiles(session, artifact, ancillaries, token)
            );
            // var ofc = new OutFileCollection();

            // var relResourcePaths = result.AddMessages(AddResourceFiles(session, artifact, ofc, "Resources/"));


//             var emittedFiles = default(OutFileCollection);
//             // var mainActivity = default(ObjectTypeDeclaration);

//             // [dho] emit source files - 21/05/19
//             {
//                 var emitter = default(IEmitter);

//                 if (artifact.TargetLang == ArtifactTargetLang.Swift)
//                 {
//                     result.AddMessages(SwiftInlining(session, artifact, ast, token));

//                     if (HasErrors(result) || token.IsCancellationRequested) return result;

//                     emitter = new SwiftEmitter();

//                     emittedFiles = result.AddMessages(CompilerHelpers.Emit(emitter, session, artifact, ast, token));

//                     foreach (var emittedFile in emittedFiles)
//                     {
//                         ofc[FileSystem.ParseFileLocation($"./{artifact.Name}/{emittedFile.Path}")] = emittedFile.Emission;
//                     }
//                 }
//                 else
//                 {
//                     result.AddMessages(
//                         new Message(MessageKind.Error,
//                             $"No bundler exists for target role '{artifact.Role.ToString()}' and target platform '{artifact.TargetPlatform}' (specified in artifact '{artifact.Name}')")
//                     );
//                 }

//                 if (HasErrors(result) || token.IsCancellationRequested) return result;
//             }

//             // [dho] synthesize any requisite files for the target platform - 25/06/19
//             {
//                 // [dho] create each file we need to compile for iOS, and add a reference to it
//                 // in the target app, so that the membership is set correctly and the file gets included
//                 // in the build by xcode etc. - 01/07/19
//                 var initRBContent = new System.Text.StringBuilder();
//                 {
//                     foreach (var emittedFile in emittedFiles)
//                     {
//                         initRBContent.Append($"target.source_build_phase.add_file_reference(src.new_file('./{artifact.Name}/{emittedFile.Path}'))");
//                         initRBContent.AppendLine();
//                     }

//                     foreach (var relResourcePath in relResourcePaths)
//                     {
//                         // [dho] TODO CLEANUP make into one call to `add_resources` with a populated array - 19/07/19
//                         initRBContent.Append($"target.add_resources([res.new_file('{relResourcePath}')])");
//                         initRBContent.AppendLine();
//                     }
//                 }

//                 var entitlementsRBContent = new System.Text.StringBuilder();
//                 var entitlementsPListContent = new System.Text.StringBuilder();
//                 {
//                     foreach (var entitlement in session.Entitlements[artifact.Name])
//                     {
//                         entitlementsRBContent.Append($"'{entitlement.Name}' => {{'enabled' => 1}},");

//                         entitlementsPListContent.Append($"<key>{entitlement.Name}</key>");
//                         entitlementsPListContent.AppendLine();

//                         PListSerialize(entitlement.Type, entitlement.Values, ref entitlementsPListContent);
//                     }
//                 }


//                 // [dho] entitlements adapted from : https://stackoverflow.com/questions/40673116/ionic-cordova-how-to-add-push-capability-with-fastlane-or-xcodebuild - 15/09/19
//                 AddRawFileIfMissing(ofc, $"init.rb",
// $@"
// # try to install xcodeproj if missing
// `gem list '^xcodeproj$' -i || sudo gem install xcodeproj`
// require 'xcodeproj'

// # create project from scratch
// project = Xcodeproj::Project.new('./{artifact.Name}.xcodeproj')

// # target represents the app artifact being produced by the build
// target = project.new_target(:application, '{artifact.Name}', :ios, nil, nil, :swift)

// # entitlements inject adapted from 
// # entitlement_path = '{artifact.Name}/{artifact.Name}.entitlements'

// # file = project.new_file(entitlement_path)


// attributes = {{}}
// project.targets.each do |target|
//     attributes[target.uuid] = {{'SystemCapabilities' => {{ {entitlementsRBContent.ToString()} }} }}
//     # target.add_file_references([file])
//     puts 'Added to target: ' + target.uuid
// end
// project.root_object.attributes['TargetAttributes'] = attributes


// # grouping the emitted files under a folder with the same name as artifact
// src = project.new_group('{artifact.Name}')

// res = src.new_group('Resources')


// # Note Info.plist is not included in target, but is pointed to by build configuration for target instead
// src.new_file('./{artifact.Name}/Info.plist')

// src.new_file('./{artifact.Name}/Entitlements.plist')


// {initRBContent.ToString() /* include all the emitted files */}


// target.build_configurations.each do |config|
//     # Make sure the Info.plist is configured for all configs in the target
//     config.build_settings['INFOPLIST_FILE'] = './{artifact.Name}/Info.plist'
//     config.build_settings['PRODUCT_BUNDLE_IDENTIFIER'] = '{EmittedPackageName}'
//     config.build_settings['CODE_SIGN_ENTITLEMENTS'] = './{artifact.Name}/Entitlements.plist'
// end

// project.save()

// `pod install`");

//                 var podfileContent = new System.Text.StringBuilder();
//                 {
//                     foreach (var dependency in session.Dependencies[artifact.Name])
//                     {
//                         podfileContent.Append($"pod '{dependency.Name}'");

//                         if (dependency.Version != null)
//                         {
//                             podfileContent.Append($", '~> {dependency.Version}'");
//                         }

//                         podfileContent.AppendLine();
//                     }
//                 }

//                 // [dho] from Pod docs : "Comment the next line if you're not using Swift and don't want to use dynamic frameworks" - 05/10/19
//                 var useFrameworks = artifact.TargetLang == Sempiler.ArtifactTargetLang.Swift ? "use_frameworks!" : "";

//                 AddRawFileIfMissing(ofc, $"Podfile",
// $@"source 'https://cdn.cocoapods.org/'

// target '{artifact.Name}' do
//   {useFrameworks}
//   platform :ios, '13.0'
//   {podfileContent.ToString()}
// end");

                


                result.Value = ofc;
            // }


            return result;
        }

        private static void PListSerialize(ConfigurationPrimitive type, string[] values, ref System.Text.StringBuilder pListContent)
        {
            switch(type)
            {
                case ConfigurationPrimitive.String:{
                    System.Diagnostics.Debug.Assert(values.Length == 1);
                    pListContent.Append($"<string>{values[0]}</string>");
                    pListContent.AppendLine();
                }
                break;

                case ConfigurationPrimitive.StringArray:{
                    pListContent.Append($"<array>");
                    pListContent.AppendLine();
                    foreach(var value in values)
                    {
                        pListContent.Append($"<string>{value}</string>");
                        pListContent.AppendLine();
                    }
                    pListContent.Append($"</array>");
                    pListContent.AppendLine();
                }
                break;

                default:{
                    System.Diagnostics.Debug.Assert(
                        false,
                        $"Unhandled Entitlement Type for '{((ConfigurationPrimitive)type).ToString()}' in IOS Bundler"
                    );
                }
                break;
            }
        }

        private static Result<OutFileCollection> SupportingFiles(Session session, Artifact artifact, List<Ancillary> ancillaries, CancellationToken token)
        {
            var result = new Result<OutFileCollection>();

            var ofc = new OutFileCollection();
            
            
            if(HasErrors(result) || token.IsCancellationRequested) return result;
            
            // [dho] from Pod docs : "Comment the next line if you're not using Swift and don't want to use dynamic frameworks" - 05/10/19
            var useFrameworks = artifact.TargetLang == Sempiler.ArtifactTargetLang.Swift ? "use_frameworks!" : "";

            var podfileContent = new System.Text.StringBuilder();

            var initRBContent = new System.Text.StringBuilder();


            initRBContent.Append(
$@"
# try to install xcodeproj if missing
`gem list '^xcodeproj$' -i || sudo gem install xcodeproj`
require 'xcodeproj'

# create project from scratch
project = Xcodeproj::Project.new('./{artifact.Name}.xcodeproj')

attributes = {{}}


###### https://github.com/CocoaPods/Xcodeproj/issues/408
embed_extensions_phase = project.new(Xcodeproj::Project::Object::PBXCopyFilesBuildPhase)
embed_extensions_phase.name = 'Embed App Extensions'
embed_extensions_phase.symbol_dst_subfolder_spec = :plug_ins
"
            );


            var artifactRelPath = $"./{artifact.Name}";
            var combinedAST = new RawAST();
            {
                var newDomain = NodeFactory.Domain(combinedAST, new PhaseNodeOrigin(PhaseKind.Bundling));

                ASTHelpers.Register(combinedAST, newDomain.Node);
            }
            
            var componentNamesProcessed = new Dictionary<string, (bool, string)>();
        


            foreach(var ancillary in ancillaries)
            {
                var targetInfo = default(TargetInfo);

                // emittedFiles
                if(ancillary.Role == AncillaryRole.MainApp)
                {
                    targetInfo = result.AddMessages(
                        EmitMainAppTarget(session, artifact, ancillary, artifactRelPath, token)
                    );

                    initRBContent.Append(
                        result.AddMessages(
                            EmitMainAppInitRBContent(session, artifact, ancillary, targetInfo,
                            componentNamesProcessed, combinedAST, ofc, artifactRelPath, token)
                        ) ?? System.String.Empty
                    );
                }
                else if(ancillary.Role == AncillaryRole.ShareExtension)
                {
                    targetInfo = result.AddMessages(
                        EmitShareExtensionTargetInfo(session, artifact, ancillary, artifactRelPath, token)
                    );

                    initRBContent.Append(
                        result.AddMessages(
                            EmitShareExtensionInitRBContent(session, artifact, ancillary, targetInfo,
                            componentNamesProcessed, combinedAST, ofc, artifactRelPath, token)
                        ) ?? System.String.Empty
                    );
                }
                else
                {
                    result.AddMessages(
                        new Message(MessageKind.Error,
                            $"Unsupported ancillary role '{ancillary.Role.ToString()} in bundler for artifact role '{artifact.Role.ToString()}' and target platform '{artifact.TargetPlatform}' (specified in artifact '{artifact.Name}')")
                    );
                    
                }




             
                
          
                podfileContent.Append(
$@"target '{targetInfo.Name}' do
  {useFrameworks}
  platform :ios, '13.0'");

                podfileContent.AppendLine();

                foreach (var dependency in ancillary.Dependencies)
                {
                    podfileContent.Append($"pod '{dependency.Name}'");

                    if (dependency.Version != null)
                    {
                        podfileContent.Append($", '~> {dependency.Version}'");
                    }

                    podfileContent.AppendLine();
                }   

                podfileContent.Append("end"); 
                podfileContent.AppendLine();

     
            }


            initRBContent.Append(
$@"
project.root_object.attributes['TargetAttributes'] = attributes

project.save()

`pod install`"
            );



            // [dho] entitlements adapted from : https://stackoverflow.com/questions/40673116/ionic-cordova-how-to-add-push-capability-with-fastlane-or-xcodebuild - 15/09/19
            AddRawFileIfMissing(ofc, $"init.rb", initRBContent.ToString());

               
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            {
                AddRawFileIfMissing(ofc, $"Podfile",
$@"source 'https://cdn.cocoapods.org/'

  {podfileContent.ToString()}
");
            }


            // [dho] TODO REMOVE these transformers.. they should be called in the user's source, not done
            // implicitly by the bundler - 19/10/19

            // [dho] use SwiftUI API in the AST - 29/06/19
            {
                var task = new Sempiler.Transformation.IOSSwiftUITransformer().Transform(session, artifact, combinedAST, token);

                task.Wait();

                var newAST = result.AddMessages(task.Result);

                if (HasErrors(result) || token.IsCancellationRequested) return result;

                if (newAST != combinedAST)
                {
                    result.AddMessages(
                        new Message(MessageKind.Error, "iOS SwiftUI Transformer unexpectedly returned a different AST that was discarded")
                    );
                }
            }


            {
                var task = new Sempiler.Transformation.SwiftNamedArgumentsTransformer().Transform(session, artifact, combinedAST, token);

                task.Wait();

                var newAST = result.AddMessages(task.Result);

                if (HasErrors(result) || token.IsCancellationRequested) return result;

                if (newAST != combinedAST)
                {
                    result.AddMessages(new Message(MessageKind.Error, "Swift Named Arguments Transformer unexpectedly returned a different AST that was discarded"));
                }
            }


            {
                var task = new Sempiler.Transformation.SwiftEnforceMutatingMethodsTransformer().Transform(session, artifact, combinedAST, token);

                task.Wait();

                var newAST = result.AddMessages(task.Result);

                if (HasErrors(result) || token.IsCancellationRequested) return result;

                if (newAST != combinedAST)
                {
                    result.AddMessages(new Message(MessageKind.Error, "Swift Enforce Mutating Methods Transformer unexpectedly returned a different AST that was discarded"));
                }
            }




            var emitter = new SwiftEmitter();

            var emittedFiles = result.AddMessages(CompilerHelpers.Emit(emitter, session, artifact, combinedAST, token));

            if(emittedFiles != null)
            {
                foreach (var emittedFile in emittedFiles)
                {
                    ofc[FileSystem.ParseFileLocation($"{artifactRelPath}/{emittedFile.Path}")] = emittedFile.Emission;
                }
            

                result.Value = ofc;
            }


            return result;

        }   

        struct TargetInfo
        {
            public TargetInfo(string name, Artifact artifact)
            {
                Name = name;
                Artifact = artifact;
                FilesNotReferencedInScheme = new OutFileCollection();
                FilesReferencedInScheme = new OutFileCollection();
                ComponentNamesReferenced = new List<string>();
                InfoPListFileName = null;
                EntitlementsPListFileName = null;
            }
            public readonly string Name;
            public readonly Artifact Artifact;

            // public string AncillaryName {
            //     get => Name + "_target";
            // }
            
            public string SrcName {
                get => Name + "_src";
            }

            public string ResName{
                get => Name + "_res";
            }

            public string RelPath {
                get => $"{Artifact.Name}/{Name}";
            }



            public OutFileCollection FilesNotReferencedInScheme;

            public OutFileCollection FilesReferencedInScheme;

            public List<string> ComponentNamesReferenced;

            public string InfoPListFileName{ get; set; }
            public string EntitlementsPListFileName { get; set; }
        }

        private static Result<TargetInfo> EmitMainAppTarget(Session session, Artifact artifact, Ancillary ancillary, string artifactRelPath, CancellationToken token)
        {
            var result = new Result<TargetInfo>();
            var targetInfo = new TargetInfo(MainAppTargetName, artifact);


            foreach(var cNode in ASTNodeFactory.Domain(ancillary.AST, ASTHelpers.GetRoot(ancillary.AST)).Components)
            {
                targetInfo.ComponentNamesReferenced.Add(
                    ASTNodeFactory.Component(ancillary.AST, (DataNode<string>)cNode).Name
                );
            }


            // // [dho] source files - 17/10/19
            // {
            //     result.AddMessages(
            //         XXXX(session, artifact, ancillary, token)
            //     );

            //     if (HasErrors(result) || token.IsCancellationRequested) return result;

            //     var emitter = new SwiftEmitter();

            //     targetInfo.EmittedSourceFiles = result.AddMessages(
            //         CompilerHelpers.Emit(emitter, session, artifact, ancillary.AST, token)
            //     );
            // }

            // [dho] manifest files - 17/10/19
            {
                var permissionPListContent = new System.Text.StringBuilder();
                {
                    foreach (var permission in ancillary.Permissions)
                    {
                        permissionPListContent.Append($"<key>{permission.Name}</key>");
                        permissionPListContent.AppendLine();

                        permissionPListContent.Append($"<string>{permission.Description}</string>");
                        permissionPListContent.AppendLine();
                    }
                }

                var capabilitiesPListContent = new System.Text.StringBuilder();
                {
                    foreach (var capability in ancillary.Capabilities)
                    {
                        capabilitiesPListContent.Append($"<key>{capability.Name}</key>");
                        capabilitiesPListContent.AppendLine();

                        PListSerialize(capability.Type, capability.Values, ref capabilitiesPListContent);
                    }
                }

                // targetInfo.FilesNotReferencedInScheme = new OutFileCollection();

                AddRawFileIfMissing(targetInfo.FilesNotReferencedInScheme, targetInfo.InfoPListFileName = $"Info.plist",
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
					<string>$(PRODUCT_MODULE_NAME).{SceneDelegateClassSymbolName}</string>
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
    <key>NSAppTransportSecurity</key>    
    <dict>
        <key>NSAllowsLocalNetworking</key>
        <true/>
    </dict>
    {permissionPListContent.ToString()}
    {capabilitiesPListContent.ToString()}
</dict>
</plist>");



                AddRawFileIfMissing(
                    targetInfo.FilesNotReferencedInScheme, 
                    targetInfo.EntitlementsPListFileName = $"Entitlements.plist", 
                    SerializeEntitlements(ancillary.Entitlements)
                );


                targetInfo.FilesReferencedInScheme = new OutFileCollection();

            }




            result.Value = targetInfo;
            return result;
        }

        private static Result<string> EmitMainAppInitRBContent(
            Session session, Artifact artifact, Ancillary ancillary, TargetInfo targetInfo, 
            Dictionary<string, (bool, string)> componentNamesProcessed, RawAST combinedAST, 
            OutFileCollection ofc, string artifactRelPath, CancellationToken token
        )
        {
            var result = new Result<string>();
            var content = new System.Text.StringBuilder();

            result.AddMessages(
                MoveComponentsToCombinedAST(session, artifact, ancillary, combinedAST, componentNamesProcessed, token)
            );

            var relResourcePaths = result.AddMessages(
                AddResourceFiles(session, artifact, ancillary, ofc, $"{targetInfo.RelPath}/Resources/")
            );

            if(HasErrors(result) || token.IsCancellationRequested) return result;

            content.Append(
$@"
# target represents the app artifact being produced by the build
{targetInfo.Name} = project.new_target(:application, '{targetInfo.Name}', :ios, nil, nil, :swift)

{targetInfo.Name}.build_phases << embed_extensions_phase

attributes[{targetInfo.Name}.uuid] = {{'SystemCapabilities' => {{ 
"
            );

            foreach (var entitlement in ancillary.Entitlements)
            {
                content.Append($"'{entitlement.Name}' => {{'enabled' => 1}},");
            }

            content.Append(
$@"
}} }}

# grouping the emitted files under a folder with the same name as artifact
{targetInfo.SrcName} = project.new_group('{targetInfo.Name}')

{targetInfo.ResName} = {targetInfo.SrcName}.new_group('Resources')

# Note Info.plist is not included in target, but is pointed to by build configuration for target instead
{targetInfo.SrcName}.new_file('{targetInfo.RelPath}/{targetInfo.InfoPListFileName}')
{targetInfo.SrcName}.new_file('{targetInfo.RelPath}/{targetInfo.EntitlementsPListFileName}')
"
            );


            
            foreach (var name in targetInfo.ComponentNamesReferenced)
            {
                var willEmit = componentNamesProcessed[name].Item1;

                if(willEmit)
                {
                    var emittedFileName = componentNamesProcessed[name].Item2;
                    
                    var p = $"{artifactRelPath}/{emittedFileName}.swift";

                    content.Append($"{targetInfo.Name}.source_build_phase.add_file_reference({targetInfo.SrcName}.new_file('{p}'))");
                    content.AppendLine();
                }
            }

            foreach(var synthManifestFile in targetInfo.FilesNotReferencedInScheme)
            {
                var p = $"{targetInfo.RelPath}/{synthManifestFile.Path}";

                ofc[FileSystem.ParseFileLocation(p)] = synthManifestFile.Emission;
            }

            foreach (var synthSourceFile in targetInfo.FilesReferencedInScheme)
            {
                var p = $"{targetInfo.RelPath}/{synthSourceFile.Path}";

                ofc[FileSystem.ParseFileLocation(p)] = synthSourceFile.Emission;
            
            
                content.Append($"{targetInfo.Name}.source_build_phase.add_file_reference({targetInfo.SrcName}.new_file('{p}'))");
                content.AppendLine();
            }


            foreach (var relResourcePath in relResourcePaths)
            {
                // [dho] TODO CLEANUP make into one call to `add_resources` with a populated array - 19/07/19
                content.Append($"{targetInfo.Name}.add_resources([{targetInfo.ResName}.new_file('{relResourcePath}')])");
                content.AppendLine();
            }
        


            content.Append(
$@"
{targetInfo.Name}.build_configuration_list.set_setting('INFOPLIST_FILE', '{targetInfo.RelPath}/{targetInfo.InfoPListFileName}')
{targetInfo.Name}.build_configuration_list.set_setting('CODE_SIGN_ENTITLEMENTS', '{targetInfo.RelPath}/{targetInfo.EntitlementsPListFileName}')
{targetInfo.Name}.build_configuration_list.set_setting('PRODUCT_BUNDLE_IDENTIFIER', '{ProjectBundleIdentifier}')
# {targetInfo.Name}.build_configuration_list.set_setting('DEVELOPMENT_TEAM', "")
"
            );


            result.Value = content.ToString();

            return result;
        }

        private static Result<TargetInfo> EmitShareExtensionTargetInfo(Session session, Artifact artifact, Ancillary ancillary, string artifactRelPath, CancellationToken token)
        {
            var result = new Result<TargetInfo>();
            var targetInfo = new TargetInfo(ancillary.Role.ToString().ToLower(), artifact);


            foreach(var cNode in ASTNodeFactory.Domain(ancillary.AST, ASTHelpers.GetRoot(ancillary.AST)).Components)
            {
                targetInfo.ComponentNamesReferenced.Add(
                    ASTNodeFactory.Component(ancillary.AST, (DataNode<string>)cNode).Name
                );
            }

            // // [dho] source files - 17/10/19
            // {
            //     // result.AddMessages(
            //     //     SwiftInlining(session, artifact, ancillary.AST, token)
            //     // );

            //     // if (HasErrors(result) || token.IsCancellationRequested) return result;

            //     var emitter = new SwiftEmitter();

            //     targetInfo.EmittedSourceFiles = result.AddMessages(
            //         CompilerHelpers.Emit(emitter, session, artifact, ancillary.AST, token)
            //     );
            // }

            // [dho] manifest files - 17/10/19
            {
                var permissionPListContent = new System.Text.StringBuilder();
                {
                    foreach (var permission in ancillary.Permissions)
                    {
                        permissionPListContent.Append($"<key>{permission.Name}</key>");
                        permissionPListContent.AppendLine();

                        permissionPListContent.Append($"<string>{permission.Description}</string>");
                        permissionPListContent.AppendLine();
                    }
                }

                var capabilitiesPListContent = new System.Text.StringBuilder();
                {
                    foreach (var capability in ancillary.Capabilities)
                    {
                        capabilitiesPListContent.Append($"<key>{capability.Name}</key>");
                        capabilitiesPListContent.AppendLine();

                        PListSerialize(capability.Type, capability.Values, ref capabilitiesPListContent);
                    }
                }

                // targetInfo.FilesNotReferencedInScheme = new OutFileCollection();

                AddRawFileIfMissing(targetInfo.FilesNotReferencedInScheme, targetInfo.InfoPListFileName = $"Info.plist",
$@"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
<dict>
	<key>CFBundleDevelopmentRegion</key>
	<string>$(DEVELOPMENT_LANGUAGE)</string>
	<key>CFBundleDisplayName</key>
	<string>{targetInfo.Name}</string>
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
	<key>NSExtension</key>
	<dict>
		<key>NSExtensionAttributes</key>
		<dict>
			<key>NSExtensionActivationRule</key>
			<dict>
				<key>NSExtensionActivationSupportsText</key>
				<integer>1</integer>
				<key>NSExtensionActivationSupportsWebURLWithMaxCount</key>
				<integer>1</integer>
			</dict>
			<!-- <key>NSExtensionJavaScriptPreprocessingFile</key>
			<string>GetURL</string> -->
		</dict>
		<key>NSExtensionMainStoryboard</key>
		<string>MainInterface</string>
		<key>NSExtensionPointIdentifier</key>
		<string>com.apple.share-services</string>
	</dict>
    <key>NSAppTransportSecurity</key>    
    <dict>
        <key>NSAllowsLocalNetworking</key>
        <true/>
    </dict>
    {permissionPListContent.ToString()}
    {capabilitiesPListContent.ToString()}
</dict>
</plist>
");




                AddRawFileIfMissing(
                    targetInfo.FilesNotReferencedInScheme, 
                    targetInfo.EntitlementsPListFileName = $"{targetInfo.Name}.entitlements", 
                    SerializeEntitlements(ancillary.Entitlements)
                );



                targetInfo.FilesReferencedInScheme = new OutFileCollection();
                // [dho] TODO dynamic, fully qualified? - 17/10/19
                var shareViewController = "ShareViewController";


                AddRawFileIfMissing(targetInfo.FilesReferencedInScheme, $"MainInterface.storyboard",
$@"<?xml version=""1.0"" encoding=""UTF-8""?>
<document type=""com.apple.InterfaceBuilder3.CocoaTouch.Storyboard.XIB"" version=""3.0"" toolsVersion=""13122.16"" targetRuntime=""iOS.CocoaTouch"" propertyAccessControl=""none"" useAutolayout=""YES"" useTraitCollections=""YES"" useSafeAreas=""YES"" colorMatched=""YES"" initialViewController=""j1y-V4-xli"">
    <dependencies>
        <plugIn identifier=""com.apple.InterfaceBuilder.IBCocoaTouchPlugin"" version=""13104.12""/>
        <capability name=""Safe area layout guides"" minToolsVersion=""9.0""/>
        <capability name=""documents saved in the Xcode 8 format"" minToolsVersion=""8.0""/>
    </dependencies>
    <scenes>
        <!--Share View Controller-->
        <scene sceneID=""ceB-am-kn3"">
            <objects>
                <viewController id=""j1y-V4-xli"" customClass=""{shareViewController}"" customModuleProvider=""target"" sceneMemberID=""viewController"">
                    <view key=""view"" opaque=""NO"" contentMode=""scaleToFill"" id=""wbc-yd-nQP"">
                        <rect key=""frame"" x=""0.0"" y=""0.0"" width=""375"" height=""667""/>
                        <autoresizingMask key=""autoresizingMask"" widthSizable=""YES"" heightSizable=""YES""/>
                        <color key=""backgroundColor"" red=""0.0"" green=""0.0"" blue=""0.0"" alpha=""0.0"" colorSpace=""custom"" customColorSpace=""sRGB""/>
                        <viewLayoutGuide key=""safeArea"" id=""1Xd-am-t49""/>
                    </view>
                </viewController>
                <placeholder placeholderIdentifier=""IBFirstResponder"" id=""CEy-Cv-SGf"" userLabel=""First Responder"" sceneMemberID=""firstResponder""/>
            </objects>
        </scene>
    </scenes>
</document>
");


            }



            result.Value = targetInfo;
            return result;
        }


        private static Result<OutFileCollection> XXXXMainAppPLists(Session session, Artifact artifact, Ancillary ancillary, CancellationToken token)
        {
            var result = new Result<OutFileCollection>();

            var ofc = new OutFileCollection();

            var permissionPListContent = new System.Text.StringBuilder();
            {
                foreach (var permission in ancillary.Permissions)
                {
                    permissionPListContent.Append($"<key>{permission.Name}</key>");
                    permissionPListContent.AppendLine();

                    permissionPListContent.Append($"<string>{permission.Description}</string>");
                    permissionPListContent.AppendLine();
                }
            }

            var capabilitiesPListContent = new System.Text.StringBuilder();
            {
                foreach (var capability in ancillary.Capabilities)
                {
                    capabilitiesPListContent.Append($"<key>{capability.Name}</key>");
                    capabilitiesPListContent.AppendLine();

                    PListSerialize(capability.Type, capability.Values, ref capabilitiesPListContent);
                }
            }


            AddRawFileIfMissing(ofc, $"Info.plist",
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
					<string>$(PRODUCT_MODULE_NAME).{SceneDelegateClassSymbolName}</string>
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
    <key>NSAppTransportSecurity</key>    
    <dict>
        <key>NSAllowsLocalNetworking</key>
        <true/>
    </dict>
    {permissionPListContent.ToString()}
    {capabilitiesPListContent.ToString()}
</dict>
</plist>");



            var entitlementsRBContent = new System.Text.StringBuilder();
            var entitlementsPListContent = new System.Text.StringBuilder();
            {
                foreach (var entitlement in ancillary.Entitlements)
                {
                    entitlementsRBContent.Append($"'{entitlement.Name}' => {{'enabled' => 1}},");

                    entitlementsPListContent.Append($"<key>{entitlement.Name}</key>");
                    entitlementsPListContent.AppendLine();

                    PListSerialize(entitlement.Type, entitlement.Values, ref entitlementsPListContent);
                }
            }


            AddRawFileIfMissing(ofc, $"Entitlements.plist", SerializeEntitlements(ancillary.Entitlements));

            result.Value = ofc;

            return result;
        }

        private static Result<string> EmitShareExtensionInitRBContent(
            Session session, Artifact artifact, Ancillary ancillary, TargetInfo targetInfo, 
            Dictionary<string, (bool, string)> componentNamesProcessed, RawAST combinedAST, 
            OutFileCollection ofc, string artifactRelPath, CancellationToken token
        )
        {
            var result = new Result<string>();
            var content = new System.Text.StringBuilder();

            result.AddMessages(
                MoveComponentsToCombinedAST(session, artifact, ancillary, combinedAST, componentNamesProcessed, token)
            );

            var relResourcePaths = result.AddMessages(
                AddResourceFiles(session, artifact, ancillary, ofc, $"{targetInfo.RelPath}/Resources/")
            );

            if(HasErrors(result) || token.IsCancellationRequested) return result;


            content.Append(
$@"
{targetInfo.SrcName} = project.main_group.find_subpath('ShareExtension', true)

{targetInfo.Name} = project.new_target(:app_extension, '{targetInfo.Name}', :ios, '13.0')

attributes[{targetInfo.Name}.uuid] = {{'SystemCapabilities' => {{ 
"
            );


            foreach (var entitlement in ancillary.Entitlements)
            {
                content.Append($"'{entitlement.Name}' => {{'enabled' => 1}},");
            }

            content.Append(
$@"
}} }}

{targetInfo.ResName} = {targetInfo.SrcName}.new_group('Resources')

# Note Info.plist is not included in target, but is pointed to by build configuration for target instead
{targetInfo.SrcName}.new_file('{targetInfo.RelPath}/{targetInfo.InfoPListFileName}')
{targetInfo.SrcName}.new_file('{targetInfo.RelPath}/{targetInfo.EntitlementsPListFileName}')

{targetInfo.Name}.build_configuration_list.set_setting('INFOPLIST_FILE', '{targetInfo.RelPath}/{targetInfo.InfoPListFileName}')
{targetInfo.Name}.build_configuration_list.set_setting('CODE_SIGN_ENTITLEMENTS', '{targetInfo.RelPath}/{targetInfo.EntitlementsPListFileName}')
{targetInfo.Name}.build_configuration_list.set_setting('PRODUCT_BUNDLE_IDENTIFIER', '{ProjectBundleIdentifier}.{targetInfo.Name}')
# {targetInfo.Name}.build_configuration_list.set_setting('DEVELOPMENT_TEAM', "")

"
            );

            
            foreach (var name in targetInfo.ComponentNamesReferenced)
            {
                var willEmit = componentNamesProcessed[name].Item1;

                if(willEmit)
                {
                    var emittedFileName = componentNamesProcessed[name].Item2;
                    
                    var p = $"{artifactRelPath}/{emittedFileName}.swift";


                    content.Append($"{targetInfo.Name}.add_file_references([{targetInfo.SrcName}.new_file('{p}')])");
                    content.AppendLine();
                }
            }

            foreach(var synthManifestFile in targetInfo.FilesNotReferencedInScheme)
            {
                var p = $"{targetInfo.RelPath}/{synthManifestFile.Path}";

                ofc[FileSystem.ParseFileLocation(p)] = synthManifestFile.Emission;
            }

            foreach (var synthSourceFile in targetInfo.FilesReferencedInScheme)
            {
                var p = $"{targetInfo.RelPath}/{synthSourceFile.Path}";

                ofc[FileSystem.ParseFileLocation(p)] = synthSourceFile.Emission;
            
                content.Append($"{targetInfo.Name}.add_file_references([{targetInfo.SrcName}.new_file('{p}')])");
                content.AppendLine();
            }


            foreach (var relResourcePath in relResourcePaths)
            {
                // [dho] TODO CLEANUP make into one call to `add_resources` with a populated array - 19/07/19
                content.Append($"{targetInfo.Name}.add_resources([{targetInfo.ResName}.new_file('{relResourcePath}')])");
                content.AppendLine();
            }
        

            content.Append(
$@"
{MainAppTargetName}.add_dependency({targetInfo.Name})
embed_extensions_phase.add_file_reference({targetInfo.Name}.product_reference).settings = {{ 'ATTRIBUTES' => ['RemoveHeadersOnCopy'] }}
"
            );

            result.Value = content.ToString();

            return result;
        }



        public static Result<List<string>> AddResourceFiles(Session session, Artifact artifact, Ancillary ancillary, OutFileCollection ofc, string relResourcesOutputPath)
        {
            var result = new Result<List<string>>();

            var relResourcePaths = new List<string>();

            foreach(var resource in ancillary.Resources)
            {
                switch(resource.Kind)
                {
                    case SourceKind.File:{

                        var sourceFile = (ISourceFile)resource;

                        var srcPath = sourceFile.Location.ToPathString();
                
                        // var relPath = FileSystem.ParseFileLocation($@"./{artifact.Name}/{relResourcesOutputPath}{
                        //     srcPath.Replace($"{session.BaseDirectory.ToPathString()}/{Sempiler.Core.Main.InferredConfig.ResDirName}/{artifact.Name}/", "")
                        // }").ToPathString();

                        // [dho] for now we just strip off the 
                        // parent path components and just use the filename - 20/10/19 
                        var relPath = FileSystem.ParseFileLocation(
                            $@"{relResourcesOutputPath}{sourceFile.Location.Name}.{sourceFile.Location.Extension}"
                        ).ToPathString();

                        /* if(*/AddCopyOfFileIfMissing(ofc, relPath, srcPath);//)
                        // {
                            relResourcePaths.Add(relPath);
                        // }
                        // else
                        // {
                        //     result.AddMessages(
                        //         new Message(MessageKind.Warning, $"'{artifact.Name}' resource '{relPath}' could not be added because a file at the location already exists in the output file collection")
                        //     );
                        // }
                    }
                    break;

                    case SourceKind.Literal:{
                        var sourceLiteral = (ISourceLiteral)resource;
                        var srcPath = sourceLiteral.Location.ToPathString();
                
                        // var relPath = FileSystem.ParseFileLocation($@"./{artifact.Name}/{relResourcesOutputPath}{
                        //     srcPath.Replace($"{session.BaseDirectory.ToPathString()}/{Sempiler.Core.Main.InferredConfig.ResDirName}/{artifact.Name}/", "")
                        // }").ToPathString();

                        // [dho] for now we just strip off the 
                        // parent path components and just use the filename - 20/10/19 
                        var relPath = FileSystem.ParseFileLocation(
                            $@"{relResourcesOutputPath}{sourceLiteral.Location.Name}.{sourceLiteral.Location.Extension}"
                        ).ToPathString();


                        AddRawFileIfMissing(ofc, relPath, sourceLiteral.Text);
                        // if(AddRawFileIfMissing(ofc, relPath, ((ISourceLiteral)resource).Text))
                        // {
                            relResourcePaths.Add(relPath);
                        // }
                        // else
                        // {
                        //     result.AddMessages(
                        //         new Message(MessageKind.Warning, $"'{artifact.Name}' resource '{relPath}' could not be added because a file at the location already exists in the output file collection")
                        //     );
                        // }
                    }
                    break;

                    default:
                    {
                        result.AddMessages(
                            new Message(MessageKind.Error, $"'{artifact.Name}' resource has unsupported kind '{resource.Kind}'")
                        );
                    }
                    break;
                }
            }

            result.Value = relResourcePaths;

            return result;
        }

   
        private static string SerializeEntitlements(IEnumerable<Entitlement> entitlements)
        {
            var entitlementsRBContent = new System.Text.StringBuilder();
                var entitlementsPListContent = new System.Text.StringBuilder();
                {
                    foreach (var entitlement in entitlements)
                    {
                        entitlementsRBContent.Append($"'{entitlement.Name}' => {{'enabled' => 1}},");

                        entitlementsPListContent.Append($"<key>{entitlement.Name}</key>");
                        entitlementsPListContent.AppendLine();

                        PListSerialize(entitlement.Type, entitlement.Values, ref entitlementsPListContent);
                    }
                }


                return (
$@"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
<dict>
    {entitlementsPListContent.ToString()}
</dict>
</plist>"
                );
        }

        /** [dho] `componentNamesProcessed` indicates whether the component will be emitted, and if so what the filename wil be - 18/10/19 */
        private static Result<object> MoveComponentsToCombinedAST(Session session, Artifact artifact, Ancillary ancillary, RawAST combinedAST, Dictionary<string, (bool, string)> componentNamesProcessed, CancellationToken token)
        {
            var result = new Result<object>();

            var oldAST = ancillary.AST.Clone();

            var root = ASTHelpers.GetRoot(oldAST);

            System.Diagnostics.Debug.Assert(root?.Kind == SemanticKind.Domain);

            var newComponentNodes = new List<Node>();

            // var ancillaryComponentMainFuncBodyContent = new System.Text.StringBuilder();

            foreach (var cNode in ASTNodeFactory.Domain(oldAST, root).Components)
            {
                var oldComponent = ASTNodeFactory.Component(oldAST, (DataNode<string>)cNode);
                var oldComponentName = oldComponent.Name;
                
                var topLevelExpressionsFuncNameLexeme = $"{oldComponent.ID}_TOPLEVELEXP";

                // [dho] check if we have already processed this component - 17/10/19
                if(!componentNamesProcessed.ContainsKey(oldComponentName))
                {
                    // var r = ClientInlining.GetInlinerInfo(session, oldComponent.AST, cNode, LanguageSemantics.Swift, token);

                    // var inlinerInfo = result.AddMessages(r);

                    // if(HasErrors(r))
                    // {
                    //     continue;
                    // }

                    
                    
                    var oldComponentEdgeNodes = ASTHelpers.QueryEdgeNodes(oldComponent.AST, oldComponent.ID, x => x.Role != SemanticRole.Parent);
                    
                    if(oldComponentEdgeNodes.Length > 0)
                    {
                        var newComponent = NodeFactory.Component(combinedAST, new PhaseNodeOrigin(PhaseKind.Bundling), oldComponent.ID);
                        {
                            ASTHelpers.DeepRegister(ancillary.AST, combinedAST, oldComponentEdgeNodes);

                            ASTHelpers.Connect(combinedAST, newComponent.ID, oldComponentEdgeNodes, SemanticRole.None);
                        

                            var r = ClientInlining.GetInlinerInfo(session, oldComponent.AST, cNode, LanguageSemantics.Swift, token);

                            var inlinerInfo = result.AddMessages(r);

                            if(HasErrors(r))
                            {
                                continue;
                            }

                            var imports = new List<Node>();
                            result.AddMessages(
                                ProcessImports(session, artifact, combinedAST, newComponent, inlinerInfo.ImportDeclarations, token, ref imports)
                            );
                        }

                        newComponentNodes.Add(newComponent.Node);

                        componentNamesProcessed[oldComponentName] = (true, oldComponent.ID);
                    }
                    else
                    {
                        componentNamesProcessed[oldComponentName] = (false, null);
                    }
                



                    // var topLevelExpressionsFunc = NodeFactory.FunctionDeclaration(oldAST, new PhaseNodeOrigin(PhaseKind.Bundling));
                    // {
                    //     var publicFlag = NodeFactory.Meta(oldAST, new PhaseNodeOrigin(PhaseKind.Bundling), MetaFlag.WorldVisibility);
                    //     var name = NodeFactory.Identifier(oldAST, new PhaseNodeOrigin(PhaseKind.Bundling), topLevelExpressionsFuncNameLexeme);
                    //     var body = NodeFactory.Block(oldAST, new PhaseNodeOrigin(PhaseKind.Bundling));
                    //     {
                    //         ASTHelpers.Connect(oldAST, body.ID, inlinerInfo.ExecOnLoads.ToArray(), SemanticRole.Content);
                    //     }

                    //     ASTHelpers.Connect(oldAST, topLevelExpressionsFunc.ID, new[] { publicFlag.Node }, SemanticRole.Meta);
                    //     ASTHelpers.Connect(oldAST, topLevelExpressionsFunc.ID, new [] { name.Node }, SemanticRole.Name);
                    //     ASTHelpers.Connect(oldAST, topLevelExpressionsFunc.ID, new [] { body.Node }, SemanticRole.Body);
                    // }
                    // ASTHelpers.Connect(oldAST, oldComponent.ID, new [] { topLevelExpressionsFunc.Node }, SemanticRole.None);


                    
                } 
                else
                {
                    int i = 0;
                }

                // ancillaryComponentMainFuncBodyContent.Append($"{topLevelExpressionsFuncNameLexeme}();");
                // ancillaryComponentMainFuncBodyContent.AppendLine();

                newComponentNodes.Add(oldComponent.Node);
            }
            
            // // [dho] invoke the 'main' method in the user's code - 17/10/19
            // // ancillaryComponentMainFuncBodyContent.Append($"{entrypointName}();");
            // // ancillaryComponentMainFuncBodyContent.AppendLine();


            // var ancillaryComponentNameLexeme = ancillary.Role.ToString();
            // var ancillaryComponent = NodeFactory.Component(combinedAST, new PhaseNodeOrigin(PhaseKind.Bundling), ancillaryComponentNameLexeme);
            // {
            //     var ancillaryMainFuncNameLexeme = $"{ancillaryComponent.ID}_MAIN";
            //     var ancillaryMainFunc = NodeFactory.FunctionDeclaration(ancillaryComponent.AST, new PhaseNodeOrigin(PhaseKind.Bundling));
            //     {
            //         var publicFlag = NodeFactory.Meta(ancillaryComponent.AST, new PhaseNodeOrigin(PhaseKind.Bundling), MetaFlag.WorldVisibility);
            //         var name = NodeFactory.Identifier(ancillaryComponent.AST, new PhaseNodeOrigin(PhaseKind.Bundling), ancillaryMainFuncNameLexeme);
            //         var body = NodeFactory.Block(ancillaryComponent.AST, new PhaseNodeOrigin(PhaseKind.Bundling));
            //         {
            //             ASTHelpers.Connect(ancillaryComponent.AST, body.ID, new [] {
            //                 NodeFactory.CodeConstant(ancillaryComponent.AST, new PhaseNodeOrigin(PhaseKind.Bundling), ancillaryComponentMainFuncBodyContent.ToString()).Node
            //             }, SemanticRole.Content);
            //         }
                    
            //         ASTHelpers.Connect(ancillaryComponent.AST, ancillaryMainFunc.ID, new[] { publicFlag.Node }, SemanticRole.Meta);
            //         ASTHelpers.Connect(ancillaryComponent.AST, ancillaryMainFunc.ID, new [] { name.Node }, SemanticRole.Name);
            //         ASTHelpers.Connect(ancillaryComponent.AST, ancillaryMainFunc.ID, new [] { body.Node }, SemanticRole.Body);
            //     }
            //     ASTHelpers.Connect(ancillaryComponent.AST, ancillaryComponent.ID, new [] { ancillaryMainFunc.Node }, SemanticRole.None);

            //     newComponentNodes.Add(ancillaryComponent.Node);
            // }

            // [dho] need to add the nodes to the new AST - 17/10/19
            // ASTHelpers.DeepRegister(ancillary.AST, combinedAST, newComponentNodes.ToArray());

            // [dho] connect the new components in the combined AST - 17/10/19
            ASTHelpers.Connect(
                combinedAST, 
                ASTHelpers.GetRoot(combinedAST).ID, 
                newComponentNodes.ToArray(), 
                SemanticRole.Component
            );

            return result;
        }

        // private static Result<object> XXXX(Session session, Artifact artifact, Ancillary ancillary, RawAST combinedAST, CancellationToken token)
        // {
        //     var result = new Result<object>();

        //     var ancillaryComponentNameLexeme = ancillary.Role.ToString();
        //     var ancillaryComponent = NodeFactory.Component(combinedAST, new PhaseNodeOrigin(PhaseKind.Bundling), ancillaryComponentNameLexeme);
        //     {
        //         var ancillaryMainFuncNameLexeme = $"{ancillaryComponent.ID}_MAIN";
        //         var ancillaryMainFunc = NodeFactory.FunctionDeclaration(ancillaryComponent.AST, new PhaseNodeOrigin(PhaseKind.Bundling));
        //         {
        //             var publicFlag = NodeFactory.Meta(ancillaryComponent.AST, new PhaseNodeOrigin(PhaseKind.Bundling), MetaFlag.WorldVisibility);
        //             var name = NodeFactory.Identifier(ancillaryComponent.AST, new PhaseNodeOrigin(PhaseKind.Bundling), ancillaryMainFuncNameLexeme);
        //             var body = NodeFactory.Block(ancillaryComponent.AST, new PhaseNodeOrigin(PhaseKind.Bundling));
        //             {
        //                 ASTHelpers.Connect(ancillaryComponent.AST, body.ID, new [] {
        //                     NodeFactory.CodeConstant(ancillaryComponent.AST, new PhaseNodeOrigin(PhaseKind.Bundling), ancillaryComponentMainFuncBodyContent.ToString()).Node
        //                 }, SemanticRole.Content);
        //             }
                    
        //             ASTHelpers.Connect(ancillaryComponent.AST, ancillaryMainFunc.ID, new[] { publicFlag.Node }, SemanticRole.Meta);
        //             ASTHelpers.Connect(ancillaryComponent.AST, ancillaryMainFunc.ID, new [] { name.Node }, SemanticRole.Name);
        //             ASTHelpers.Connect(ancillaryComponent.AST, ancillaryMainFunc.ID, new [] { body.Node }, SemanticRole.Body);
        //         }
        //         ASTHelpers.Connect(ancillaryComponent.AST, ancillaryComponent.ID, new [] { ancillaryMainFunc.Node }, SemanticRole.None);
        //     }


        //     ASTHelpers.Connect(
        //         combinedAST, 
        //         ASTHelpers.GetRoot(combinedAST).ID, 
        //         new [] { ancillaryComponent.Node }, 
        //         SemanticRole.Component
        //     );

        //     return result;
        // }


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
            var entrypointInlined = default(ObjectTypeDeclaration);

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
                            entrypointInlined = r.Value;
                            // entrypointView = r.Value;
                        }
                        // [dho] any code that was outside an artifact root is just emitted without a class wrapper, so we have a way
                        // in the input sources of declaring global symbols, or things like protocols which cannot be nested inside other
                        // declarations in Swift - 18/07/19
                        else if (!WillInlineAsObjectTypeDeclaration(session, component))
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



                // [dho] we will expose the launch args to the user entrypoint function for the app if they have specified
                // a parameter declaration for it, eg `export default main(args : { [key : UIApplication.LaunchOptionsKey] : Any })` - 29/09/19
                // [dho] TODO investigate whether to provide a universal API for launch args across platforms.. is that counter to the Sempiler tenets? - 29/09/19
                var requiresLaunchArgsAccess = false;
                {
                    foreach(var member in entrypointInlined.Members)
                    {
                        if(member.Kind == SemanticKind.MethodDeclaration)
                        {
                            var methodDecl = ASTNodeFactory.MethodDeclaration(ast, member);

                            if(ASTNodeHelpers.IsIdentifierWithName(ast, methodDecl.Name, EntrypointSymbolName))
                            {
                                // [dho] for now just assuming that any parameter would be a reference to the 
                                // launch args - 29/09/19
                                requiresLaunchArgsAccess = methodDecl.Parameters.Length > 0;
                                break;
                            }
                        }
                    }
                }



                var appDelegateClass = NodeFactory.ObjectTypeDeclaration(ast, new PhaseNodeOrigin(PhaseKind.Bundling));
                {
                    ASTHelpers.Connect(ast, appDelegateClass.ID, new[] {
                        NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Bundling), AppDelegateClassSymbolName).Node
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

                    if(requiresLaunchArgsAccess)
                    {
                        ASTHelpers.Connect(ast, appDelegateClass.ID, new[] { 
                            NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Bundling),
                                $"static var {entrypointComponent.ID}LaunchOptions: [UIApplication.LaunchOptionsKey: Any]?").Node
                         }, SemanticRole.Member);
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

                        if(requiresLaunchArgsAccess)
                        {
                            topLevelExpressions.Add(
                                //launchOptions
                                NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Bundling), $"{AppDelegateClassSymbolName}.{entrypointComponent.ID}LaunchOptions = launchOptions").Node
                            );
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
                        NodeFactory.Identifier(ast, new PhaseNodeOrigin(PhaseKind.Bundling), SceneDelegateClassSymbolName).Node
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


                        var entrypointInvocationArgString = requiresLaunchArgsAccess ? $"{AppDelegateClassSymbolName}.{entrypointComponent.ID}LaunchOptions ?? [:]" : string.Empty;

                        var body = NodeFactory.Block(ast, new PhaseNodeOrigin(PhaseKind.Bundling));
                        {
                            ASTHelpers.Connect(ast, body.ID, new[] {
                                NodeFactory.CodeConstant(ast, new PhaseNodeOrigin(PhaseKind.Bundling),
    $@"if let windowScene = scene as? UIWindowScene {{
            let window = UIWindow(windowScene: windowScene)
            window.rootViewController = UIHostingController(rootView: {ToInlinedObjectTypeClassIdentifier(ast, entrypointComponent.Node)}.{EntrypointSymbolName}({entrypointInvocationArgString}))
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

            // [dho] disabling this because:
            // - TypeScript semantics do not automatically qualify instance references, so the author would have to do this usually (assuming source language is TypeScript)
            // - If the class extends another class then the inherited members would not be qualified using the current implementation
            //
            // 04/10/19
            //
            // {
            //     var task = new Sempiler.Transformation.SwiftInstanceSymbolTransformer().Transform(session, artifact, ast, token);

            //     task.Wait();

            //     var newAST = result.AddMessages(task.Result);

            //     if (HasErrors(result) || token.IsCancellationRequested) return result;

            //     if (newAST != ast)
            //     {
            //         result.AddMessages(new Message(MessageKind.Error, "Swift Instance Symbol Transformer unexpectedly returned a different AST that was discarded"));
            //     }
            // }

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

        private static bool WillInlineAsObjectTypeDeclaration(Session session, Component component)
        {
            return !BundlerHelpers.IsOutsideArtifactInferredSourceDir(session, component);
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
                var (userCodeParams, userCodeBody) = result.AddMessages(
                    GetEntrypointParamsAndBody(session, artifact, ast, component, inlinerInfo.Entrypoint, inlinerInfo.EntrypointUserCode, token)
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


                    ASTHelpers.Connect(ast, entrypointMethod.ID, userCodeParams, SemanticRole.Parameter);

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

                if(WillInlineAsObjectTypeDeclaration(session, component))
                {
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
                }
                else
                {
                    for (int i = 0; i < fnDecls.Length; ++i)
                    {
                        fnDecls[i] = inlinerInfo.FunctionDeclarations[i].Node;
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

        private static Result<(Node[], Node)> GetEntrypointParamsAndBody(Session session, Artifact artifact, RawAST ast, Component component, Node entrypoint, Node entrypointUserCode, CancellationToken token)
        {
            var result = new Result<(Node[], Node)>();

            if (entrypoint != null)
            {
                if (BundlerHelpers.IsInferredArtifactEntrypointComponent(session, artifact, component))
                {
                    // [dho] TODO CLEANUP HACK to get function parameters!! - 29/09/19
                    var userCodeParams = ASTHelpers.QueryEdgeNodes(ast, entrypointUserCode.ID, SemanticRole.Parameter);
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

                    result.Value = (userCodeParams, userCodeBody);
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
            else
            {
                result.Value = (default(Node[]), default(Node));
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
                var shouldMakeTopLevelDeclsStatic = WillInlineAsObjectTypeDeclaration(session, component);

                // var topLevelStatements = new List<Node>();

                foreach (var execOnLoad in execOnLoads)
                {
                    if (LanguageSemantics.Swift.IsDeclarationStatement(ast, execOnLoad))
                    {
                        // [dho] need to make the declarations static at file level because we
                        // are putting them inside a class - 14/07/19
                        if ((MetaHelpers.ReduceFlags(ast, execOnLoad) & MetaFlag.Static) == 0 && shouldMakeTopLevelDeclsStatic)
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