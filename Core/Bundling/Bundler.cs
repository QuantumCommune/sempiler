﻿using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Sempiler.AST;
using Sempiler.Emission;
using static Sempiler.AST.Diagnostics.DiagnosticsHelpers;
using Sempiler.Diagnostics;
using static Sempiler.Diagnostics.DiagnosticsHelpers;

namespace Sempiler.Bundler
{
    public interface IBundler
    { 
        // [dho] creates a bundle of files that can be deployed and executed in the target platform, eg. inferring and injecting
        // a manifest file where none has been explicitly provided - 21/05/19
        Task<Sempiler.Diagnostics.Result<OutFileCollection>> Bundle(Session session, Artifact artifact, List<Ancillary> ancillaries, CancellationToken token);

        IList<string> GetPreservedDebugEmissionRelPaths();
    }

    public static class BundlerHelpers
    {
          // [dho] TODO HACK make dynamic! - 01/06/19
        public const string EmittedPackageName = "com.sempiler";

        public static bool IsInferredSessionEntrypointComponent(Session session, Component component)
        {
            var relParentDirPath = component.Name.Replace(session.BaseDirectory.ToPathString(), "");

            foreach(var inputPath in session.InputPaths)
            {
                if(inputPath.Replace(session.BaseDirectory.ToPathString(), "") == relParentDirPath)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsInferredArtifactEntrypointComponent(Session session, Artifact artifact, Component component)
        {
            var relParentDirPath = component.Name.Replace(session.BaseDirectory.ToPathString(), "");

            // [dho] is this component the entrypoint for the whole artifact - 21/06/19
            return (relParentDirPath.ToLower().IndexOf(GetNameOfExpectedArtifactEntrypointComponent(session, artifact)) == 0);
        }

        public static bool IsOutsideArtifactInferredSourceDir(Session session, Component component)
        {
            var relComponentPath = component.Name.Replace(session.BaseDirectory.ToPathString(), "");

            // [dho] check component is not inside the inferred source directory for any artifact in the session - 18/07/19
            foreach (var kv in session.Artifacts)
            {
                var artifactName = kv.Key;
                var inferredArtifactSourceDirPath = $"/{Sempiler.Core.Main.InferredConfig.SourceDirName}/{artifactName}/";

                if (relComponentPath.IndexOf(inferredArtifactSourceDirPath) > -1)
                {
                    return false;
                }
            }
            return true;
        }

        public static string GetNameOfExpectedArtifactEntrypointComponent(Session session, Artifact artifact)
        {
            return $"/{Sempiler.Core.Main.InferredConfig.SourceDirName}/{artifact.Name}/{Sempiler.Core.Main.InferredConfig.EntrypointFileName}".ToLower();
        }

        public static bool AddRawFileIfMissing(OutFileCollection ofc, string relPath, string content)
        {
            var location = FileSystem.ParseFileLocation(relPath);

            if (!ofc.Contains(location))
            {
                ofc[location] = new Sempiler.Emission.RawOutFileContent(System.Text.Encoding.UTF8.GetBytes(content));

                return true;
            }

            return false;
        }

        public static bool AddCopyOfFileIfMissing(OutFileCollection ofc, string relPath, string sourcePath)
        {
            var location = FileSystem.ParseFileLocation(relPath);

            if (!ofc.Contains(location))
            {
                ofc[location] = new Sempiler.Emission.RawOutFileContent(System.IO.File.ReadAllBytes(sourcePath));

                return true;
            }

            return false;
        }

        public static Result<MethodDeclaration> ConvertToStaticMethodDeclaration(Session session, RawAST ast, FunctionLikeDeclaration node, CancellationToken token)
        {
            var result = new Result<MethodDeclaration>();

            var decl = NodeFactory.MethodDeclaration(ast, node.Origin);

            var name = node.Name;

            if (name == null)
            {
                result.AddMessages(
                    new AST.Diagnostics.NodeMessage(MessageKind.Error, $"Function must have a name", node)
                    {
                        Hint = GetHint(node.Origin)
                    }
                );

                return result;
            }

            ASTHelpers.Connect(ast, decl.ID, new[] { name }, SemanticRole.Name);

            ASTHelpers.Connect(ast, decl.ID, node.Template, SemanticRole.Template);

            ASTHelpers.Connect(ast, decl.ID, node.Parameters, SemanticRole.Parameter);

            var type = node.Type;

            if(type != null)
            {
                ASTHelpers.Connect(ast, decl.ID, new[] { type }, SemanticRole.Type);
            }


            ASTHelpers.Connect(ast, decl.ID, new[] { node.Body }, SemanticRole.Body);

            ASTHelpers.Connect(ast, decl.ID, node.Annotations, SemanticRole.Annotation);

            ASTHelpers.Connect(ast, decl.ID, node.Modifiers, SemanticRole.Modifier);


            AddMetaFlag(session, ast, decl, MetaFlag.Static, token);

            result.Value = decl;

            return result;
        }

        public static void AddMetaFlag(Session session, RawAST ast, ASTNode node, MetaFlag newFlag, CancellationToken token)
        {
            var metaFlags = MetaHelpers.ReduceFlags(node);

            var meta = node.Meta;

            // [dho] make the method static if it is not already - 16/04/19
            if ((metaFlags & newFlag) == 0)
            {
                var f = NodeFactory.Meta(
                    ast,
                    new PhaseNodeOrigin(PhaseKind.Bundling),
                    newFlag
                );

                var m = new Node[meta.Length + 1];

                if (meta.Length > 0)
                {
                    System.Array.Copy(meta, m, meta.Length);
                }

                m[m.Length - 1] = f.Node;

                ASTHelpers.Connect(ast, node.ID, m, SemanticRole.Meta);
            }
            else
            {
                ASTHelpers.Connect(ast, node.ID, meta, SemanticRole.Meta);
            }

        }

        // public static string GenerateCTID()
        // {
        //     return "_" + System.Guid.NewGuid().ToString().Replace('-', '_');
        // }
    }
}