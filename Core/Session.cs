﻿using Sempiler.AST;
using Sempiler.Core;
using Sempiler.Emission;
using Sempiler.Diagnostics;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Sempiler
{
    ///<summary>
    /// Not serialized between runs of the compiler, so can contain machine specific
    /// information such as the base path
    ///</summary>
    public struct Session
    {
        // IOptions Options { get; set; }

        // [dho] DO NOT store the AST on the session, because we might
        // have multiple ASTs throughout a session - 23/08/18
        //RawAST AST { get; }
        public DateTime Start;

        public DateTime End;

        public DuplexSocketServer Server;

        public IDirectoryLocation BaseDirectory;

        public Dictionary<string, Component> ComponentCache;

        public IEnumerable<string> InputPaths;

        public Dictionary<string, Artifact> Artifacts;

        public Dictionary<string, List<Shard>> Shards;

        // public Dictionary<string, string> GUIDs { get; set; }

        // public Dictionary<string, RawAST> ASTs { get; set; }

        // public Dictionary<string, List<ISource>> Resources { get; set; }

        // public Dictionary<string, List<Capability>> Capabilities { get; set; }
        // public Dictionary<string, List<Dependency>> Dependencies { get; set; }

        // public Dictionary<string, List<Entitlement>> Entitlements { get; set; }

        // public Dictionary<string, List<Permission>> Permissions { get; set; }

        // ///<summary>Key is *source* artifact name (ie. where the bridge intent was found), *NOT* the *target* artifact it wants to talk to</summary>
        // public Dictionary<string, List<Directive>> BridgeIntents { get; set; }

        public Dictionary<string, Dictionary<string, OutFile>> FilesWritten;

        public CTExec.CTExecInfo CTExecInfo;

    }

    public struct Capability
    {
        public string Name;
        public object Value;
    }


    public struct ManifestEntry
    {
        public string[] Path;
        public object Value;
    }

    public struct Dependency 
    {
        public string Name;
        public string Version;
        public string PackageManager;
        public string URL;
    }

    public static class DependencyHelpers
    {
        public static Result<bool> AddIfNotPresent(ref List<Dependency> dependencies, Dependency newDependency)
        {
            var result = new Result<bool> { Value = false };

            foreach(var existingDependency in dependencies)
            {
                if(existingDependency.Name == newDependency.Name)
                {
                    return result;
                }
            }

            dependencies.Add(newDependency);

            result.Value = true;

            return result;
        }
    }

    

    public static class PackageManager
    {
        // public const string Compiler = "compiler";
        
        public const string NPM = "npm";
        public const string CocoaPods = "pod";
        public const string SwiftPackageManager = "spm";

        // public static readonly Dictionary<string, string> CacheDirNames = new Dictionary<string, string>{
        //     { PackageManager.NPM, "node_modules" },
        //     { PackageManager.CocoaPods, "Pods" }
        // };
    }
    

    public enum ConfigurationPrimitive
    {
        Boolean,
        Integer,
        Float,
        String,
        StringArray
    }

    public struct Entitlement
    {
        public string Name;
        public object Value;
    }

    public struct Resource
    {
        public ISource Source;
        /** [dho] optional rewriting of the file name in the target output - 11/02/20 */
        public string TargetFileName;
    }

    public struct Permission
    {
        public string Name;
        public string Description;
    }

    public enum Orientation 
    {
        Unspecified = 0x0,
        Portrait = 0x1,
        PortraitUpsideDown = 0x2,
        LandscapeLeft = 0x4,
        LandscapeRight = 0x8
    }

    public enum AssetRole 
    {
        None,
        AppIcon,
        Image,
        Font,
        Splash
    }

    public abstract class Asset 
    {
        public AssetRole Role; 
    }

    public class RawAsset : Asset 
    {
        public string SourcePath;
        public List<ISourceFile> Files;
    }

    public class ImageAssetSet : Asset
    {
        public string Name;
        public List<ImageAssetMember> Images;
    }

    public struct ImageAssetMember 
    {
        public string Size;
        public string Scale;
        public ISourceFile Source;
    }

    public class FontAsset : Asset 
    {
        public string Name;

        public ISourceFile Source;
    }

    public class SplashAsset : Asset 
    {
        public int[] BackgroundRGB;
        public int? Width;
        public int? Height;

        public ImageAssetMember Image;
        public ImageAssetMember ImageX2;
        public ImageAssetMember ImageX3;
    }

    public static class SessionHelpers
    {
        // [dho] Session is a struct (value type) so it will have been
        // implicitly copied when passed to this function. If we ever change
        // Session to a reference type, here we will have to do more work to
        // actually copy the properties on the object to a new instance explicitly - 21/04/19
        public static Session Clone(Session session)
        {
            return session;
        }

        // public static void AddSource(Session session, ISource source)
        // {
        //     // [dho] no de duping?
        //     session.Sources.Add(source);
        // }

        public static void CacheComponents(Session session, IEnumerable<Component> components, CancellationToken token)
        {
            lock(session.ComponentCache)
            {
                foreach(var component in components)
                {
                    // [dho] originally we just directly cached the component, but that was a pretty serious bug
                    // because if any nodes were disabled in that subtree, we would copy that disabling to every
                    // AST that copied this component from the cache.. so instead we provision a fresh AST and copy
                    // the component to it, which is:
                    // - more expensive
                    // - not suitable for all the code that expects each AST to have a Domain as root
                    //
                    // but is much more robust for now.. - 07/12/19
                    var ast = new RawAST();

                    ASTHelpers.DeepRegister(component.AST, ast, new [] { component.Node }, token);

                    session.ComponentCache[component.Name] = ASTNodeFactory.Component(ast, (DataNode<string>)component.Node);
                }
            }
        }
    }

}
