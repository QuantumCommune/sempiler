using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Sempiler.Diagnostics;
using Sempiler.AST;
using Sempiler.AST.Diagnostics;

namespace Sempiler.Emission
{
    using static Sempiler.AST.Diagnostics.DiagnosticsHelpers;

    public class JavaEmitter : BaseEmitter
    {
        public JavaEmitter() : base(new string[]{ ArtifactTargetLang.Java, PhaseKind.Emission.ToString("g").ToLower() })
        {
            FileExtension = ".java";
        }

        public override Result<object> EmitAccessorDeclaration(AccessorDeclaration node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitAccessorSignature(AccessorSignature node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitAddition(Addition node, Context context, CancellationToken token)
        {
            return EmitBinaryLike(node, "+", context, token);
        }

        public override Result<object> EmitAdditionAssignment(AdditionAssignment node, Context context, CancellationToken token)
        {
            return EmitAssignmentLike(node, "+=", context, token);
        }

        public override Result<object> EmitAddressOf(AddressOf node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitAnnotation(Annotation node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            context.Emission.Append(node, "@");

            result.AddMessages(
                EmitNode(node.Expression, context, token)
            );

            return result;
        }

        public override Result<object> EmitArithmeticNegation(ArithmeticNegation node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            context.Emission.Append(node, "-");

            result.AddMessages(
                EmitNode(node.Operand, childContext, token)
            );

            return result;
        }

        public override Result<object> EmitArrayConstruction(ArrayConstruction node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            if(node.Type == null)
            {
                result.AddMessages(
                    CreateUnsupportedFeatureResult(node)
                );
            }

            context.Emission.Append(node, "new ");

            result.AddMessages(
                EmitNode(node.Type, childContext, token)
            );

            if(node.Size != null)
            {
                context.Emission.Append(node, "[");

                result.AddMessages(
                    EmitNode(node.Size, childContext, token)
                );

                context.Emission.Append(node, "]");
            }
            else
            {
                context.Emission.Append(node, "[]");
            }

            if(node.Members.Length > 0)
            {
                context.Emission.Append(node, "{");

                result.AddMessages(
                    EmitCSV(node.Members, childContext, token)
                );

                context.Emission.Append(node, "}");
            }

            return result;
        }

        public override Result<object> EmitArrayTypeReference(ArrayTypeReference node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            result.AddMessages(
                EmitNode(node.Type, childContext, token)
            );
            
            context.Emission.Append(node, "[]");

            return result;
        }

        public override Result<object> EmitAssignment(Assignment node, Context context, CancellationToken token)
        {
            return EmitAssignmentLike(node, "=", context, token);
        }

        public override Result<object> EmitAssociation(Association node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            context.Emission.Append(node, "(");

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            result.AddMessages(
                EmitNode(node.Subject, childContext, token)  
            );

            context.Emission.Append(node, ")");

            return result;
        }

        public override Result<object> EmitBitwiseAnd(BitwiseAnd node, Context context, CancellationToken token)
        {
            return EmitBinaryLike(node, "&", context, token);
        }

        public override Result<object> EmitBitwiseAndAssignment(BitwiseAndAssignment node, Context context, CancellationToken token)
        {
            return EmitAssignmentLike(node, "&=", context, token);
        }

        public override Result<object> EmitBitwiseExclusiveOr(BitwiseExclusiveOr node, Context context, CancellationToken token)
        {
            return EmitBinaryLike(node, "^", context, token);
        }

        public override Result<object> EmitBitwiseExclusiveOrAssignment(BitwiseExclusiveOrAssignment node, Context context, CancellationToken token)
        {
            return EmitAssignmentLike(node, "^=", context, token);
        }

        public override Result<object> EmitBitwiseLeftShift(BitwiseLeftShift node, Context context, CancellationToken token)
        {
            return EmitBitwiseShift(node, "<<", context, token);
        }

        public override Result<object> EmitBitwiseLeftShiftAssignment(BitwiseLeftShiftAssignment node, Context context, CancellationToken token)
        {
            return EmitAssignmentLike(node, "<<=", context, token);
        }

        public override Result<object> EmitBitwiseNegation(BitwiseNegation node, Context context, CancellationToken token)
        {
            return EmitPrefixUnaryLike(node, "~", context, token);
        }

        public override Result<object> EmitBitwiseOr(BitwiseOr node, Context context, CancellationToken token)
        {
            return EmitBinaryLike(node, "|", context, token);
        }

        public override Result<object> EmitBitwiseOrAssignment(BitwiseOrAssignment node, Context context, CancellationToken token)
        {
            return EmitAssignmentLike(node, "|=", context, token);
        }

        public override Result<object> EmitBitwiseRightShift(BitwiseRightShift node, Context context, CancellationToken token)
        {
            return EmitBitwiseShift(node, ">>", context, token);
        }

        public override Result<object> EmitBitwiseRightShiftAssignment(BitwiseRightShiftAssignment node, Context context, CancellationToken token)
        {
            return EmitAssignmentLike(node, ">>=", context, token);
        }

        public override Result<object> EmitBitwiseUnsignedRightShift(BitwiseUnsignedRightShift node, Context context, CancellationToken token)
        {
            return EmitBitwiseShift(node, ">>>", context, token);
        }

        public override Result<object> EmitBitwiseUnsignedRightShiftAssignment(BitwiseUnsignedRightShiftAssignment node, Context context, CancellationToken token)
        {
            return EmitAssignmentLike(node, ">>>=", context, token);
        }

        public override Result<object> EmitBlock(Block node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            context.Emission.Append(node, "{");

            if (node.Content.Length > 0)
            {
                context.Emission.Indent();

                foreach (var (member, hasNext) in ASTNodeHelpers.IterateMembers(node.Content))
                {
                    context.Emission.AppendBelow(node, "");

                    var prevEmissionLength = context.Emission.Length;

                    result.AddMessages(
                        EmitNode(member, context, token)
                    );

                    var didAppend = context.Emission.Length > prevEmissionLength;

                    if(didAppend && RequiresSemicolonSentinel(member))
                    {
                        context.Emission.Append(member, ";");
                    }
                }

                context.Emission.Outdent();
            }

            context.Emission.AppendBelow(node, "}");

            return result;
        }

        public override Result<object> EmitBooleanConstant(BooleanConstant node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            context.Emission.Append(node, node.Value ? "true" : "false");

            return result;
        }

        public override Result<object> EmitBreakpoint(Breakpoint node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitClauseBreak(ClauseBreak node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            context.Emission.Append(node, "break");
            
            if(node.Expression != null)
            {
                context.Emission.Append(node, " ");

                result.AddMessages(EmitNode(node.Expression, childContext, token));
            }

            return result;
        }

        public override Result<object> EmitCodeConstant(CodeConstant node, Context context, CancellationToken token)
        {
            return base.EmitCodeConstant(node, context, token);
        }

        public override Result<object> EmitCollectionDestructuring(CollectionDestructuring node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitCompilerHint(CompilerHint node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitConcatenation(Concatenation node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            var operands = node.Operands;

            result.AddMessages(EmitDelimited(operands, "+", childContext, token));

            return result;
        }

        public override Result<object> EmitConcatenationAssignment(ConcatenationAssignment node, Context context, CancellationToken token)
        {
            return EmitAssignmentLike(node, "+=", context, token);
        }

        public override Result<object> EmitConditionalTypeReference(ConditionalTypeReference node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitLiteralTypeReference(LiteralTypeReference node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        const MetaFlag ConstructorDeclarationFlags = TypeDeclarationMemberFlags;

        public override Result<object> EmitConstructorDeclaration(ConstructorDeclaration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            var metaFlags = MetaHelpers.ReduceFlags(node);

            context.Emission.AppendBelow(node, "");

            result.AddMessages(EmitAnnotationsAndModifiers(node, context, token));

            result.AddMessages(EmitTypeDeclarationMemberFlags(node, metaFlags, context, token));

            result.AddMessages(
                ReportUnsupportedMetaFlags(node, metaFlags & ~ConstructorDeclarationFlags, context, token)
            );

            // [dho] TODO super call - 29/09/18

            if(node.Name != null)
            {
                result.AddMessages(
                    EmitNode(node.Name, childContext, token)
                );
            }
            else
            {
                result.AddMessages(
                    new NodeMessage(MessageKind.Error, $"Constructor must have a name", node)
                    {
                        Hint = GetHint(node.Origin),
                        Tags = DiagnosticTags
                    }
                );
            }

            result.AddMessages(
                EmitFunctionTemplate(node, childContext, token)
            );

            result.AddMessages(
                EmitFunctionParameters(node, childContext, token)
            );

            if(node.Type != null)
            {
                result.AddMessages(
                    CreateUnsupportedFeatureResult(node.Type, "constructor return types")
                );
            }

            result.AddMessages(
                EmitBlockLike(node.Body, node.Node, childContext, token)
            );

            return result;
        }

        public override Result<object> EmitConstructorSignature(ConstructorSignature node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitConstructorTypeReference(ConstructorTypeReference node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        const MetaFlag DataValueDeclarationFlags = MetaFlag.Constant | MetaFlag.BlockScope;

        public override Result<object> EmitDataValueDeclaration(DataValueDeclaration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;
            
            var metaFlags = MetaHelpers.ReduceFlags(node);

            context.Emission.AppendBelow(node, "");

            result.AddMessages(EmitAnnotationsAndModifiers(node, context, token));

            if((metaFlags & MetaFlag.Constant) == MetaFlag.Constant)
            {
                context.Emission.Append(node, "final ");
            }

            // [dho] variables in Java are block scoped anyway - 29/04/19
            metaFlags &= ~MetaFlag.BlockScope;


            result.AddMessages(
                ReportUnsupportedMetaFlags(node, metaFlags & ~DataValueDeclarationFlags, context, token)
            );

            if (node.Type != null)
            {
                result.AddMessages(
                    EmitNode(node.Type, childContext, token)
                );
            }
            else
            {
                result.AddMessages(
                    CreateUnsupportedFeatureResult(node, "data value declaration without a type annotation")
                );
            }

            context.Emission.Append(node, " ");

            result.AddMessages(
                EmitNode(node.Name, childContext, token)
            );

            if (node.Initializer != null)
            {
                context.Emission.Append(node, "=");

                result.AddMessages(
                    EmitNode(node.Initializer, childContext, token)
                );
            }

            return result;
        }

        public override Result<object> EmitDefaultExportReference(DefaultExportReference node, Context context, CancellationToken token)
        {
            return base.EmitDefaultExportReference(node, context, token);
        }

        public override Result<object> EmitDestruction(Destruction node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitDestructorDeclaration(DestructorDeclaration node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitDestructorSignature(DestructorSignature node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitDestructuredMember(DestructuredMember node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitDictionaryConstruction(DictionaryConstruction node, Context context, CancellationToken token)
        {
            // [dho] should we use HashMap here? But the interface for it is
            // different, so that would have needed to be abstracted away? - 26/04/19
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitDictionaryTypeReference(DictionaryTypeReference node, Context context, CancellationToken token)
        {
            // [dho] should we use HashMap here? But the interface for it is
            // different, so that would have needed to be abstracted away? - 26/04/19
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitDirective(Directive node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitDivision(Division node, Context context, CancellationToken token)
        {
            return EmitBinaryLike(node, "/", context, token);
        }

        public override Result<object> EmitDivisionAssignment(DivisionAssignment node, Context context, CancellationToken token)
        {
            return EmitAssignmentLike(node, "/=", context, token);
        }

        public override Result<object> EmitDoOrDieErrorTrap(DoOrDieErrorTrap node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitDoOrRecoverErrorTrap(DoOrRecoverErrorTrap node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitDoWhilePredicateLoop(DoWhilePredicateLoop node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            context.Emission.AppendBelow(node, "do");

            result.AddMessages(EmitBlockLike(node.Body, node.Node, childContext, token));

            context.Emission.AppendBelow(node, "while(");

            result.AddMessages(EmitNode(node.Condition, childContext, token));

            context.Emission.Append(node, ")");

            return result;
        }

        public override Result<object> EmitDomain(Domain node, Context context, CancellationToken token)
        {
            return base.EmitDomain(node, context, token);
        }

        public override Result<object> EmitDynamicTypeConstruction(DynamicTypeConstruction node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitDynamicTypeReference(DynamicTypeReference node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitEntityDestructuring(EntityDestructuring node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitEnumerationMemberDeclaration(EnumerationMemberDeclaration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            result.AddMessages(
                EmitNode(node.Name, childContext, token)
            );

            if(node.Initializer != null)
            {
                context.Emission.Append(node.Initializer, "=");

                result.AddMessages(
                    EmitNode(node.Initializer, childContext, token)
                );
            }

            return result;
        }

        public override Result<object> EmitEnumerationTypeDeclaration(EnumerationTypeDeclaration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            var metaFlags = MetaHelpers.ReduceFlags(node);

            context.Emission.AppendBelow(node, "");

            result.AddMessages(EmitAnnotationsAndModifiers(node, context, token));
            
            result.AddMessages(EmitTypeDeclarationVisibilityFlags(node, metaFlags, context, token));
                
            result.AddMessages(
                ReportUnsupportedMetaFlags(node, metaFlags & ~TypeDeclarationVisibilityFlags, context, token)
            );

            context.Emission.Append(node, "enum ");

            var name = node.Name;
            var template = node.Template;
            var supers = node.Supers;
            var interfaces = node.Interfaces;
            var members = node.Members;

            if (name != null)
            {
                if (name.Kind == SemanticKind.Identifier)
                {
                    result.AddMessages(
                        EmitIdentifier(ASTNodeFactory.Identifier(context.AST, (DataNode<string>)name), childContext, token)
                    );
                }
                else
                {
                    result.AddMessages(
                        new NodeMessage(MessageKind.Error, $"Enumeration type declaration has unsupported name type : '{name.Kind}'", name)
                        {
                            Hint = GetHint(name.Origin),
                            Tags = DiagnosticTags
                        }
                    );
                }
            }
            else
            {
                result.AddMessages(
                    CreateUnsupportedFeatureResult(node)
                );
            }

            // generics
            if(template.Length > 0)
            {
                result.AddMessages(
                    CreateUnsupportedFeatureResult(node)
                );
            }

            if(supers.Length > 0 || interfaces.Length > 0)
            {
                result.AddMessages(
                    CreateUnsupportedFeatureResult(node)
                );
            }

            result.AddMessages(
                EmitBlockLike(members, node.Node, childContext, token)
            );

            return result;
        }

        public override Result<object> EmitErrorFinallyClause(ErrorFinallyClause node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            context.Emission.AppendBelow(node, "finally ");

            if(node.Expression.Length > 0)
            {
                result.AddMessages(
                    CreateUnsupportedFeatureResult(node.Expression, "finally clause expression")
                );
            }

            result.AddMessages(
                EmitBlockLike(node.Body, node.Node, childContext, token)
            );

            return result;
        }

        public override Result<object> EmitErrorHandlerClause(ErrorHandlerClause node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            context.Emission.AppendBelow(node, "catch ");

            var expression = node.Expression;
            var body = node.Body;

            if(expression.Length == 1)
            {
                context.Emission.Append(node, "(");

                result.AddMessages(
                    EmitNode(expression[0], childContext, token)
                );

                context.Emission.Append(node, ")");
            }
            else
            {
                result.AddMessages(new NodeMessage(MessageKind.Error, $"Expected single expression but found {expression.Length}", node)
                {
                    Hint = GetHint(node.Origin),
                    Tags = DiagnosticTags
                });
            }
            

            result.AddMessages(
                EmitBlockLike(body, node.Node, childContext, token)
            );

            return result;
        }

        public override Result<object> EmitErrorTrapJunction(ErrorTrapJunction node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            context.Emission.AppendBelow(node, "try ");

            result.AddMessages(
                EmitBlockLike(node.Body, node.Node, childContext, token)
            );
            
            foreach(var (member, hasNext) in ASTNodeHelpers.IterateMembers(node.Clauses))
            {
                result.AddMessages(EmitNode(member, childContext, token));
            }

            return result;
        }

        public override Result<object> EmitEvalToVoid(EvalToVoid node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitExponentiation(Exponentiation node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitExponentiationAssignment(ExponentiationAssignment node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitExportDeclaration(ExportDeclaration node, Context context, CancellationToken token)
        {
            return base.EmitExportDeclaration(node, context, token);
        }

        const MetaFlag FieldDeclarationFlags = TypeDeclarationMemberFlags | MetaFlag.Volatile | MetaFlag.Transient;

        public override Result<object> EmitFieldDeclaration(FieldDeclaration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            var metaFlags = MetaHelpers.ReduceFlags(node);

            context.Emission.AppendBelow(node, "");

            result.AddMessages(EmitAnnotationsAndModifiers(node, context, token));

            result.AddMessages(EmitTypeDeclarationMemberFlags(node, metaFlags, context, token));

            result.AddMessages(EmitAnnotationsAndModifiers(node, context, token));

            if((metaFlags & MetaFlag.Volatile) == MetaFlag.Volatile)
            {
                context.Emission.Append(node, "volatile ");
            }

            if((metaFlags & MetaFlag.Transient) == MetaFlag.Transient)
            {
                context.Emission.Append(node, "transient ");
            }

            result.AddMessages(
                ReportUnsupportedMetaFlags(node, metaFlags & ~FieldDeclarationFlags, context, token)
            );


            if (node.Type != null)
            {
                result.AddMessages(
                    EmitNode(node.Type, childContext, token)
                );

                context.Emission.Append(node, " ");
            }
            else
            {
                result.AddMessages(
                    new NodeMessage(MessageKind.Error, $"Field must have a type", node)
                    {
                        Hint = GetHint(node.Origin),
                        Tags = DiagnosticTags
                    }
                );
            }

            result.AddMessages(
                EmitNode(node.Name, childContext, token)
            );


            if (node.Initializer != null)
            {
                context.Emission.Append(node.Initializer, "=");

                result.AddMessages(
                    EmitNode(node.Initializer, childContext, token)
                );
            }

            return result;
        }

        public override Result<object> EmitFieldSignature(FieldSignature node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            if (node.Type != null)
            {
                context.Emission.AppendBelow(node, " ");

                result.AddMessages(
                    EmitNode(node.Type, childContext, token)
                );

                context.Emission.Append(node, " ");
            }
            else
            {
                result.AddMessages(
                        new NodeMessage(MessageKind.Error, $"Field must have a type", node)
                    {
                        Hint = GetHint(node.Origin),
                        Tags = DiagnosticTags
                    }
                    );
            }
        
            result.AddMessages(
                EmitNode(node.Name, childContext, token)
            );


            if (node.Initializer != null)
            {
                result.AddMessages(
                    CreateUnsupportedFeatureResult(node.Initializer, "field signature initializer")
                );
            }

            return result;
        }

        public override Result<object> EmitForKeysLoop(ForKeysLoop node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitForMembersLoop(ForMembersLoop node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            context.Emission.AppendBelow(node, "for(");

            result.AddMessages(
                EmitNode(node.Handle, childContext, token)
            );

            context.Emission.Append(node, " : ");

            result.AddMessages(
                EmitNode(node.Subject, childContext, token)
            );

            context.Emission.Append(node, ")");

            result.AddMessages(
                EmitBlockLike(node.Body, node.Node, childContext, token)
            );

            return result;
        }

        public override Result<object> EmitForPredicateLoop(ForPredicateLoop node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            context.Emission.AppendBelow(node, "for(");

            if(node.Initializer != null)
            {
                result.AddMessages(
                    EmitNode(node.Initializer, childContext, token)
                );
            }

            context.Emission.Append(node, ";");

            if(node.Condition != null)
            {
                result.AddMessages(
                    EmitNode(node.Condition, childContext, token)
                );
            }

            context.Emission.Append(node, ";");

            if(node.Iterator != null)
            {
                result.AddMessages(
                    EmitNode(node.Iterator, childContext, token)
                );
            }

            context.Emission.Append(node, ")");

            result.AddMessages(
                EmitBlockLike(node.Body, node.Node, childContext, token)
            );

            return result;
        }

        public override Result<object> EmitForcedCast(ForcedCast node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            context.Emission.Append(node.TargetType, "(");
            
            result.AddMessages(
                EmitNode(node.TargetType, childContext, token)
            );

            context.Emission.Append(node.TargetType, ")");

            result.AddMessages(
                EmitNode(node.Subject, childContext, token)
            );

            return result;
        }

        public override Result<object> EmitFunctionDeclaration(FunctionDeclaration node, Context context, CancellationToken token)
        {
            // [dho] because Java only has methods right?? - 26/04/19
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitFunctionTermination(FunctionTermination node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            if(node.Value != null)
            {
                context.Emission.Append(node, "return ");

                var childContext = ContextHelpers.Clone(context);
                // childContext.Parent = node;

                result.AddMessages(
                    EmitNode(node.Value, childContext, token)
                );

                // [dho] end of return statment - 21/09/18
                // context.Emission.Append(node, ";");
            }
            else
            {
                context.Emission.Append(node, "return");
            }

            return result;
        }

        public override Result<object> EmitFunctionTypeReference(FunctionTypeReference node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitGeneratorSuspension(GeneratorSuspension node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitGlobalDeclaration(GlobalDeclaration node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitIdentity(Identity node, Context context, CancellationToken token)
        {
            return EmitPrefixUnaryLike(node, "+", context, token);
        }

        // public override Result<object> EmitIfDirective(IfDirective node, Context context, CancellationToken token)
        // {
        //     return CreateUnsupportedFeatureResult(node);
        // }

        public override Result<object> EmitImportDeclaration(ImportDeclaration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            // [dho] TODO EmitOrnamentation - 01/03/19
            // if(!node.Required)
            // {
            //     result.AddMessages(
            //         CreateUnsupportedFeatureResult(node)
            //     );
            // }
            var specifier = node.Specifier;
            var clauses = node.Clauses;

            context.Emission.Append(node, "import ");

            result.AddMessages(
                EmitNode(specifier, childContext, token)
            );


            if(clauses.Length > 0)
            {
                if(clauses.Length == 1 && clauses[0].Kind == SemanticKind.WildcardExportReference)
                {
                    context.Emission.Append(clauses[0], ".*");
                }
                else
                {
                    result.AddMessages(
                        CreateUnsupportedFeatureResult(clauses, "import declaration clause other than wildcard")
                    );
                }
            }


            context.Emission.Append(node, ";");
            
            return result;
        }

        public override Result<object> EmitIncidentContextReference(IncidentContextReference node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            context.Emission.Append(node, "this");

            return result;
        }

        public override Result<object> EmitIncidentTypeConstraint(IncidentTypeConstraint node, Context context, CancellationToken token)
        {
            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            context.Emission.Append(node, "implements ");

            return EmitNode(node.Type, context, token);
        }

        public override Result<object> EmitIndexTypeQuery(IndexTypeQuery node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitInferredTypeQuery(InferredTypeQuery node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitIndexedAccess(IndexedAccess node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            result.AddMessages(
                EmitNode(node.Incident, childContext, token)
            );

            context.Emission.Append(node, "[");

            result.AddMessages(
                EmitNode(node.Member, childContext, token)
            );

            context.Emission.Append(node, "]");

            return result;
        }

        public override Result<object> EmitIndexerSignature(IndexerSignature node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitInterfaceDeclaration(InterfaceDeclaration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            var metaFlags = MetaHelpers.ReduceFlags(node);

            context.Emission.AppendBelow(node, "");

            result.AddMessages(EmitAnnotationsAndModifiers(node, childContext, token));

            result.AddMessages(EmitTypeDeclarationVisibilityFlags(node, metaFlags, context, token));
                
            result.AddMessages(
                ReportUnsupportedMetaFlags(node, metaFlags & ~TypeDeclarationVisibilityFlags, context, token)
            );

            if(node.Name != null)
            {
                var name = node.Name;

                if(name.Kind == SemanticKind.Identifier)
                {
                    context.Emission.Append(node, "interface ");
                    
                    result.AddMessages(
                        EmitIdentifier(ASTNodeFactory.Identifier(context.AST, (DataNode<string>)name), childContext, token)
                    );
                }
                else
                {
                    result.AddMessages(
                        new NodeMessage(MessageKind.Error, $"Interface type declaration has unsupported name type : '{name.Kind}'", name)
                        {
                            Hint = GetHint(name.Origin),
                            Tags = DiagnosticTags
                        }
                    );
                }
            }
            else
            {
                result.AddMessages(
                    CreateUnsupportedFeatureResult(node, "anonymous interface declarations")
                );
            }

            // generics
            result.AddMessages(
                EmitTypeDeclarationTemplate(node, childContext, token)
            );
        

            result.AddMessages(
                EmitTypeDeclarationHeritage(node, context, token)
            );

            result.AddMessages(
                EmitBlockLike(node.Members, node.Node, childContext, token)
            );

            return result;
        }

        public override Result<object> EmitInterimSuspension(InterimSuspension node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitInterpolatedString(InterpolatedString node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitInterpolatedStringConstant(InterpolatedStringConstant node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitIntersectionTypeReference(IntersectionTypeReference node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitIntrinsicTypeReference(IntrinsicTypeReference node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            switch(node.Role)
            {
                case TypeRole.Boolean:
                    context.Emission.Append(node, "boolean");
                break;

                case TypeRole.Char:
                    context.Emission.Append(node, "char");
                break;

                case TypeRole.Double64:
                    context.Emission.Append(node, "double");
                break;

                case TypeRole.Float32:
                    context.Emission.Append(node, "float");
                break;

                case TypeRole.Integer32:
                    context.Emission.Append(node, "int");
                break;

                case TypeRole.RootObject:
                    context.Emission.Append(node, "Object");
                break;

                case TypeRole.String:
                    context.Emission.Append(node, "String");
                break;

                case TypeRole.Void:
                    context.Emission.Append(node, "void");
                break;

                default:
                    result.AddMessages(
                        CreateUnsupportedFeatureResult(node, $"Intrisinsic {node.Role.ToString()} types")
                    );
                break;
            }

            return result;
        }

        public override Result<object> EmitInvocation(Invocation node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            var subject = node.Subject;
            var template = node.Template;
            var arguments = node.Arguments;

            result.AddMessages(
                EmitNode(subject, childContext, token)
            );

            if (template.Length > 0)
            {
                context.Emission.Append(node, "<");

                result.AddMessages(
                    EmitCSV(template, childContext, token)
                );

                context.Emission.Append(node, ">");
            }

            context.Emission.Append(node, "(");

            if (arguments.Length > 0)
            {
                result.AddMessages(
                    EmitCSV(arguments, childContext, token)
                );
            }

            context.Emission.Append(node, ")");

            return result;
        }

        public override Result<object> EmitInvocationArgument(InvocationArgument node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            if(node.Label != null)
            {
                result.AddMessages(
                    CreateUnsupportedFeatureResult(node.Label, "argument labels")
                );
            }

            result.AddMessages(
                EmitNode(node.Value, childContext, token)
            );

            return result;
        }

        public override Result<object> EmitJumpToNextIteration(JumpToNextIteration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            context.Emission.Append(node, "continue");
            
            if(node.Label != null)
            {
                context.Emission.Append(node, " ");

                result.AddMessages(EmitNode(node.Label, childContext, token));
            }

            return result;
        }

        public override Result<object> EmitKeyValuePair(KeyValuePair node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitLabel(Label node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            result.AddMessages(EmitNode(node.Name, childContext, token));

            context.Emission.Append(node, ":");

            return result;
        }

        public override Result<object> EmitLambdaDeclaration(LambdaDeclaration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            var metaFlags = MetaHelpers.ReduceFlags(node);

            result.AddMessages(
                ReportUnsupportedMetaFlags(node, metaFlags, context, token)
            );

            var name = node.Name;

            if (name != null)
            {
                result.AddMessages(
                    new NodeMessage(MessageKind.Error, $"Lambda must be anonymous", name)
                    {
                        Hint = GetHint(name.Origin),
                        Tags = DiagnosticTags
                    }
                );
            }

            var template = node.Template;

            if (template.Length > 0)
            {
                var templateMarker = template[0];

                result.AddMessages(
                    new NodeMessage(MessageKind.Error, $"Lambda must not be templated", templateMarker)
                    {
                        Hint = GetHint(templateMarker.Origin),
                        Tags = DiagnosticTags
                    }
                );
            }

            result.AddMessages(
                EmitFunctionParameters(node, childContext, token)
            );

            if(node.Type != null)
            {
                result.AddMessages(
                    new NodeMessage(MessageKind.Error, $"Lambda must not have type", node.Type)
                    {
                        Hint = GetHint(node.Type.Origin),
                        Tags = DiagnosticTags
                    }
                );
            }

            context.Emission.Append(node, "->");

            result.AddMessages(
                EmitBlockLike(node.Body, node.Node, childContext, token)
            );
          
            return result;
        }

        public override Result<object> EmitLogicalAnd(LogicalAnd node, Context context, CancellationToken token)
        {
            return EmitBinaryLike(node, "&&", context, token);
        }

        public override Result<object> EmitLogicalNegation(LogicalNegation node, Context context, CancellationToken token)
        {
            return EmitPrefixUnaryLike(node, "!", context, token);
        }

        public override Result<object> EmitLogicalOr(LogicalOr node, Context context, CancellationToken token)
        {
            return EmitBinaryLike(node, "||", context, token);
        }

        public override Result<object> EmitLoopBreak(LoopBreak node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            context.Emission.Append(node, "break");
            
            if(node.Expression != null)
            {
                context.Emission.Append(node, " ");

                result.AddMessages(EmitNode(node.Expression, childContext, token));
            }

            return result;
        }

        public override Result<object> EmitLooseEquivalent(LooseEquivalent node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitLooseNonEquivalent(LooseNonEquivalent node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitLowerBoundedTypeConstraint(LowerBoundedTypeConstraint node, Context context, CancellationToken token)
        {
            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            context.Emission.Append(node, "super ");

            return EmitNode(node.Type, context, token);
        }

        public override Result<object> EmitMappedDestructuring(MappedDestructuring node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitMappedTypeReference(MappedTypeReference node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitMatchClause(MatchClause node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            var expression = node.Expression;

            if(expression.Length == 1)
            {
                context.Emission.Append(node, "case ");

                result.AddMessages(
                    EmitNode(expression[0], context, token)
                );

                context.Emission.Append(node, " : ");
            }
            else if(expression.Length == 0)
            {
                context.Emission.Append(node, "default:");
            }
            else
            {
                result.AddMessages(new NodeMessage(MessageKind.Error, $"Expected single expression but found {expression.Length}", node)
                {
                    Hint = GetHint(node.Origin),
                    Tags = DiagnosticTags
                });
            }

            result.AddMessages(
                EmitBlockLike(node.Body, node.Node, childContext, token)
            );

            return result;
        }

        public override Result<object> EmitMatchJunction(MatchJunction node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            context.Emission.Append(node, "switch(");

            result.AddMessages(
                EmitNode(node.Subject, childContext, token)
            );

            context.Emission.Append(node, ")");

            result.AddMessages(
                EmitBlockLike(node.Clauses, node.Node, childContext, token)
            );
            
            return result;
        }

        public override Result<object> EmitMaybeNull(MaybeNull node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitMemberNameReflection(MemberNameReflection node, Context context, CancellationToken token)
        {
            return base.EmitMemberNameReflection(node, context, token);
        }

        public override Result<object> EmitMemberTypeConstraint(MemberTypeConstraint node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitMembershipTest(MembershipTest node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitMeta(Meta node, Context context, CancellationToken token)
        {
            return base.EmitMeta(node, context, token);
        }

        public override Result<object> EmitMetaProperty(MetaProperty node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        const MetaFlag MethodDeclarationFlags = TypeDeclarationMemberFlags | MetaFlag.Abstract;

        public override Result<object> EmitMethodDeclaration(MethodDeclaration node, Context context, CancellationToken token)
        {
            return EmitFunctionLikeDeclaration(node, context, token);
        }

        protected Result<object> EmitFunctionLikeDeclaration(FunctionLikeDeclaration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            var metaFlags = MetaHelpers.ReduceFlags(node);

            context.Emission.AppendBelow(node, "");

            result.AddMessages(EmitAnnotationsAndModifiers(node, context, token));

            result.AddMessages(EmitTypeDeclarationMemberFlags(node, metaFlags, context, token));

            if((metaFlags & MetaFlag.Abstract) == MetaFlag.Abstract)
            {
                context.Emission.Append(node, "abstract ");
            }

            result.AddMessages(
                ReportUnsupportedMetaFlags(node, metaFlags & ~MethodDeclarationFlags, context, token)
            );

            var name = node.Name;

            if(name != null)
            {
                var type = node.Type;


                // [dho] TODO reuse function signature emission - 29/09/18

                // [dho] TODO modifiers! - 21/09/18
                
                if(name.Kind == SemanticKind.Identifier)
                {
                    // return type
                    if(type != null)
                    {
                        result.AddMessages(
                            EmitNode(type, childContext, token)
                        );

                        // [dho] the space after the return type - 21/09/18
                        context.Emission.Append(node, " ");
                    }
                    else
                    {
                        result.AddMessages(
                            new NodeMessage(MessageKind.Error, $"Expected method to have a specified return type", node)
                            {
                                Hint = GetHint(node.Origin),
                                Tags = DiagnosticTags
                            }
                        );
                    }

                    result.AddMessages(
                        EmitIdentifier(ASTNodeFactory.Identifier(node.AST, (DataNode<string>)name), childContext, token)
                    );

                    // generics
                    result.AddMessages(
                        EmitFunctionTemplate(node, childContext, token)
                    );

                    // [dho] Now we can emit the parameter list after the diamond - 29/09/18
                    result.AddMessages(
                        EmitFunctionParameters(node, childContext, token)
                    );

                    // body
                    result.AddMessages(
                        EmitBlockLike(node.Body, node.Node, childContext, token)
                    );
                }
                else
                {
                    result.AddMessages(
                        new NodeMessage(MessageKind.Error, $"Method has unsupported Name type : '{name.Kind}'", name)
                    {
                        Hint = GetHint(node.Name.Origin),
                        Tags = DiagnosticTags
                    }
                    );
                }
            }
            else
            {
                result.AddMessages(
                        new NodeMessage(MessageKind.Error, $"Method must have a name", node)
                    {
                        Hint = GetHint(node.Origin),
                        Tags = DiagnosticTags
                    }
                    );
            }


            return result;
        }

        public override Result<object> EmitMethodSignature(MethodSignature node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            // [dho] TODO EmitOrnamentation - 01/03/19
            // if(node.Override)
            // {
            //     context.Emission.Append(node, "@Override ");
            // }

            // if(node.Static)
            // {
            //     context.Emission.Append(node, "static ");
            // }
            var name = node.Name;
            var type = node.Type;

            if(name != null)
            {

                // [dho] TODO reuse function signature emission - 29/09/18

                // [dho] TODO modifiers! - 21/09/18
                
                if(name.Kind == SemanticKind.Identifier)
                {
                    // return type
                    if(type != null)
                    {
                        result.AddMessages(
                            EmitNode(type, childContext, token)
                        );

                        // [dho] the space after the return type - 21/09/18
                        context.Emission.Append(node, " ");
                    }
                    else
                    {
                        result.AddMessages(
                            new NodeMessage(MessageKind.Error, $"Expected function to have a specified return type", node)
                            {
                                Hint = GetHint(node.Origin),
                                Tags = DiagnosticTags
                            }
                        );
                    }

                    result.AddMessages(
                        EmitIdentifier(ASTNodeFactory.Identifier(node.AST, (DataNode<string>)name), childContext, token)
                    );

                    // generics
                    result.AddMessages(
                        EmitFunctionTemplate(node, childContext, token)
                    );

                    // [dho] Now we can emit the parameter list after the diamond - 29/09/18
                    result.AddMessages(
                        EmitFunctionParameters(node, childContext, token)
                    );
                }
                else
                {
                    result.AddMessages(
                        new NodeMessage(MessageKind.Error, $"Function has unsupported Name type : '{name.Kind}'", name)
                    {
                        Hint = GetHint(name.Origin),
                        Tags = DiagnosticTags
                    }
                    );
                }
            }
            else
            {
                result.AddMessages(
                        new NodeMessage(MessageKind.Error, $"Function must have a name", node)
                    {
                        Hint = GetHint(node.Origin),
                        Tags = DiagnosticTags
                    }
                    );
            }


            return result;
        }

        public override Result<object> EmitModifier(Modifier node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            context.Emission.Append(node, node.Lexeme);

            return result;
        }

        public override Result<object> EmitMultiplication(Multiplication node, Context context, CancellationToken token)
        {
            return EmitBinaryLike(node, "*", context, token);
        }

        public override Result<object> EmitMultiplicationAssignment(MultiplicationAssignment node, Context context, CancellationToken token)
        {
            return EmitAssignmentLike(node, "*=", context, token);
        }

        public override Result<object> EmitMutatorDeclaration(MutatorDeclaration node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitMutatorSignature(MutatorSignature node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitMutex(AST.Mutex node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitNamedTypeConstruction(NamedTypeConstruction node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            context.Emission.Append(node, "new ");

            result.AddMessages(
                EmitNode(node.Name, childContext, token)
            );

            var template = node.Template;

            if(template.Length > 0)
            {
                context.Emission.Append(node, "<");

                result.AddMessages(
                    EmitCSV(template, context, token)
                );

                context.Emission.Append(node, ">");
            }

            context.Emission.Append(node, "(");

            var arguments = node.Arguments;

            if(arguments.Length > 0)
            {
                result.AddMessages(
                    EmitCSV(arguments, childContext, token)
                );
            }

            context.Emission.Append(node, ")");

            return result;
        }

        public override Result<object> EmitNamedTypeQuery(NamedTypeQuery node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitNamedTypeReference(NamedTypeReference node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            result.AddMessages(
                EmitNode(node.Name, childContext, token)
            );

            var template = node.Template;

            if(template.Length > 0)
            {
                context.Emission.Append(node, "<");

                result.AddMessages(
                    EmitCSV(template, childContext, token)
                );

                context.Emission.Append(node, ">");
            }

            return result;
        }

        public override Result<object> EmitNamespaceDeclaration(NamespaceDeclaration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            context.Emission.Append(node, "package ");
            var name = node.Name;

            if (name != null)
            {
                if(name.Kind == SemanticKind.QualifiedAccess)
                {
                    result.AddMessages(
                        EmitQualifiedAccess(ASTNodeFactory.QualifiedAccess(context.AST, name), childContext, token)
                    );
                }
                else if (name.Kind == SemanticKind.Identifier)
                {
                    result.AddMessages(
                        EmitIdentifier(ASTNodeFactory.Identifier(context.AST, (DataNode<string>)name), childContext, token)
                    );
                }
                else
                {
                    result.AddMessages(
                        new NodeMessage(MessageKind.Error, $"Namespace declaration has unsupported name type : '{name.Kind}'", name)
                        {
                            Hint = GetHint(name.Origin),
                            Tags = DiagnosticTags
                        }
                    );
                }
            }
            else
            {
                result.AddMessages(
                    CreateUnsupportedFeatureResult(node)
                );
            }

            foreach (var (member, hasNext) in ASTNodeHelpers.IterateMembers(node.Members))
            {
                context.Emission.AppendBelow(member, "");

                result.AddMessages(
                    EmitNode(member, context, token)
                );
            }

            return result;
        }

        public override Result<object> EmitNamespaceReference(NamespaceReference node, Context context, CancellationToken token)
        {
            return base.EmitNamespaceReference(node, context, token);
        }

        public override Result<object> EmitNonMembershipTest(NonMembershipTest node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitNop(Nop node, Context context, CancellationToken token)
        {
            return base.EmitNop(node, context, token);
        }

        public override Result<object> EmitNotNull(NotNull node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitNotNumber(NotNumber node, Context context, CancellationToken token)
        {
            return base.EmitNotNumber(node, context, token);
        }

        public override Result<object> EmitNull(Null node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            context.Emission.Append(node, "null");

            return result;
        }

        public override Result<object> EmitNullCoalescence(NullCoalescence node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitNumericConstant(NumericConstant node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            context.Emission.Append(node, node.Value.ToString());

            return result;
        }

        public override Result<object> EmitObjectTypeDeclaration(ObjectTypeDeclaration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            var metaFlags = MetaHelpers.ReduceFlags(node);

            context.Emission.AppendBelow(node, "");

            result.AddMessages(EmitAnnotationsAndModifiers(node, context, token));
            
            result.AddMessages(EmitObjectTypeDeclarationFlags(node, metaFlags, context, token));
            
            result.AddMessages(
                ReportUnsupportedMetaFlags(node, metaFlags & ~ObjectTypeDeclarationFlags, context, token)
            );

            var name = node.Name;
            if(name != null)
            {

                if(name.Kind == SemanticKind.Identifier)
                {
                    context.Emission.Append(node, "class ");
                    
                    result.AddMessages(
                        EmitIdentifier(ASTNodeFactory.Identifier(context.AST, (DataNode<string>)name), childContext, token)
                    );
                }
                else
                {
                    result.AddMessages(
                        new NodeMessage(MessageKind.Error, $"Object type declaration has unsupported name type : '{name.Kind}'", name)
                        {
                            Hint = GetHint(name.Origin),
                            Tags = DiagnosticTags
                        }
                    );
                }
            }
            else
            {
                result.AddMessages(
                    CreateUnsupportedFeatureResult(node)
                );
            }

            // generics
            result.AddMessages(
                EmitTypeDeclarationTemplate(node, childContext, token)
            );
        

            result.AddMessages(
                EmitTypeDeclarationHeritage(node, context, token)
            );

            result.AddMessages(
                EmitBlockLike(node.Members, node.Node, childContext, token)
            );

            return result;
        }

        // public override Result<object> EmitOrderedGroup(OrderedGroup node, Context context, CancellationToken token)
        // {
        //     var result = new Result<object>();

        //     var memberContext = ContextHelpers.Clone(context);
        //     // memberContext.Parent = node;

        //     foreach(var (member, hasNext) in NodeInterrogation.IterateMembers(node))
        //     {
        //         result.AddMessages(
        //             EmitNode(member, memberContext, token).Messages
        //         );

        //         if(RequiresSemicolonSentinel(member))
        //         {
        //             context.Emission.Append(member, ";");
        //         }

        //         if(token.IsCancellationRequested)
        //         {
        //             break;
        //         }
        //     }

        //     return result;
        // }

        public override Result<object> EmitParameterDeclaration(ParameterDeclaration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            if(node.Type != null)
            {
                result.AddMessages(
                    EmitNode(node.Type, childContext, token)
                );
                
                context.Emission.Append(node, " ");
            }
            else if(node.Parent.Kind != SemanticKind.LambdaDeclaration)
            {
                result.AddMessages(
                    new NodeMessage(MessageKind.Error, $"Expected parameter to have a specified type", node)
                    {
                        Hint = GetHint(node.Origin),
                        Tags = DiagnosticTags
                    }
                );
            }

            if(node.Label != null)
            {
                result.AddMessages(
                    CreateUnsupportedFeatureResult(node.Label, "parameter label")
                );
            }

            result.AddMessages(
                EmitNode(node.Name, childContext, token)
            );


            if(node.Default != null)
            {
                result.AddMessages(
                    CreateUnsupportedFeatureResult(node.Default, "parameter default value")
                );
            }

            return result;
        }

        public override Result<object> EmitParenthesizedTypeReference(ParenthesizedTypeReference node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitPointerDereference(PointerDereference node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitPostDecrement(PostDecrement node, Context context, CancellationToken token)
        {
            return EmitPostUnaryArithmetic(node, "--", context, token);
        }

        public override Result<object> EmitPostIncrement(PostIncrement node, Context context, CancellationToken token)
        {
            return EmitPostUnaryArithmetic(node, "++", context, token);
        }

        public override Result<object> EmitPreDecrement(PreDecrement node, Context context, CancellationToken token)
        {
            return EmitPreUnaryArithmetic(node, "--", context, token);
        }

        public override Result<object> EmitPreIncrement(PreIncrement node, Context context, CancellationToken token)
        {
            return EmitPreUnaryArithmetic(node, "++", context, token);
        }

        public override Result<object> EmitPredicateFlat(PredicateFlat node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            result.AddMessages(
                EmitNode(node.Predicate, childContext, token)
            );

            context.Emission.Append(node, "?");

            result.AddMessages(
                EmitNode(node.TrueValue, childContext, token)
            );

            context.Emission.Append(node, ":");

            result.AddMessages(
                EmitNode(node.FalseValue, childContext, token)
            );

            return result;
        }

        public override Result<object> EmitPredicateJunction(PredicateJunction node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            context.Emission.Append(node, "if(");

            result.AddMessages(
                EmitNode(node.Predicate, childContext, token)
            );

            context.Emission.Append(node, ")");

            result.AddMessages(
                EmitBlockLike(node.TrueBranch, node.Node, childContext, token)
            );

            if(node.FalseBranch != null)
            {
                context.Emission.AppendBelow(node, "else ");

                if(node.FalseBranch.Kind == SemanticKind.PredicateJunction)
                {
                    result.AddMessages(
                        EmitPredicateJunction(ASTNodeFactory.PredicateJunction(node.AST, node.FalseBranch), childContext, token)
                    );
                }
                else
                {
                    result.AddMessages(
                        EmitBlockLike(node.FalseBranch, node.Node, childContext, token)
                    );
                }
            }
            
            return result;
        }

        public override Result<object> EmitPrioritySymbolResolutionContext(PrioritySymbolResolutionContext node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitPropertyDeclaration(PropertyDeclaration node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitQualifiedAccess(QualifiedAccess node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            result.AddMessages(
                EmitNode(node.Incident, childContext, token)
            );

            context.Emission.Append(node, ".");

            result.AddMessages(
                EmitNode(node.Member, childContext, token)
            );

            return result;
        }

        public override Result<object> EmitRaiseError(RaiseError node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            context.Emission.Append(node, "throw");
            
            if(node.Operand != null)
            {
                context.Emission.Append(node, " ");

                result.AddMessages(EmitNode(node.Operand, childContext, token));
            }

            return result;
        }

        public override Result<object> EmitReferenceAliasDeclaration(ReferenceAliasDeclaration node, Context context, CancellationToken token)
        {
            return base.EmitReferenceAliasDeclaration(node, context, token);
        }

        public override Result<object> EmitRegularExpressionConstant(RegularExpressionConstant node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitRemainder(Remainder node, Context context, CancellationToken token)
        {
            return EmitBinaryLike(node, "%", context, token);
        }

        public override Result<object> EmitRemainderAssignment(RemainderAssignment node, Context context, CancellationToken token)
        {
            return EmitAssignmentLike(node, "*=", context, token);
        }

        public override Result<object> EmitSafeCast(SafeCast node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitSmartCast(SmartCast node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitSpreadDestructuring(SpreadDestructuring node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitStrictEquivalent(StrictEquivalent node, Context context, CancellationToken token)
        {
            return EmitBinaryLike(node, "==", context, token);
        }

        public override Result<object> EmitStrictGreaterThan(StrictGreaterThan node, Context context, CancellationToken token)
        {
            return EmitBinaryLike(node, ">", context, token);
        }

        public override Result<object> EmitStrictGreaterThanOrEquivalent(StrictGreaterThanOrEquivalent node, Context context, CancellationToken token)
        {
            return EmitBinaryLike(node, ">=", context, token);
        }

        public override Result<object> EmitStrictLessThan(StrictLessThan node, Context context, CancellationToken token)
        {
            return EmitBinaryLike(node, "<", context, token);
        }

        public override Result<object> EmitStrictLessThanOrEquivalent(StrictLessThanOrEquivalent node, Context context, CancellationToken token)
        {
            return EmitBinaryLike(node, "<=", context, token);
        }

        public override Result<object> EmitStrictNonEquivalent(StrictNonEquivalent node, Context context, CancellationToken token)
        {
            return EmitBinaryLike(node, "!=", context, token);
        }

        public override Result<object> EmitStringConstant(StringConstant node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            if(ASTNodeHelpers.IsMultilineString(node))
            {
                result.AddMessages(
                    CreateUnsupportedFeatureResult(node)
                );
            }
            else
            {
                context.Emission.Append(node, $"\"{node.Value}\"");
            }

            return result;
        }

        public override Result<object> EmitSubtraction(Subtraction node, Context context, CancellationToken token)
        {
            return EmitBinaryLike(node, "-", context, token);
        }

        public override Result<object> EmitSubtractionAssignment(SubtractionAssignment node, Context context, CancellationToken token)
        {
            return EmitAssignmentLike(node, "-=", context, token);
        }

        public override Result<object> EmitSuperContextReference(SuperContextReference node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            context.Emission.Append(node, "super");

            return result;
        }

        public override Result<object> EmitTupleConstruction(TupleConstruction node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitTupleTypeReference(TupleTypeReference node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitTypeAliasDeclaration(TypeAliasDeclaration node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitTypeInterrogation(TypeInterrogation node, Context context, CancellationToken token)
        {
            return base.EmitTypeInterrogation(node, context, token);
        }

        public override Result<object> EmitTypeParameterDeclaration(TypeParameterDeclaration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            result.AddMessages(
                EmitNode(node.Name, childContext, token)
            );

            if(node.Constraints.Length > 0)
            {
                result.AddMessages(
                    CreateUnsupportedFeatureResult(node.Constraints, "type constraints")
                );
            }

            // if (node.SuperConstraints != null)
            // {
            //     context.Emission.Append(node.SuperConstraints, " extends ");

            //     result.AddMessages(
            //         EmitDelimited(node.SuperConstraints, "&", childContext, token)
            //     );

            //     if(NodeInterrogation.MemberCount(node.SubConstraints) > 0)
            //     {
            //         context.Emission.Append(node, ",");
            //     }
            // }

            // if (node.SubConstraints != null)
            // {
            //     context.Emission.Append(node, " super ");

            //     result.AddMessages(
            //         EmitDelimited(node.SubConstraints, "&", childContext, token)
            //     );
            // }

            if (node.Default != null)
            {
                result.AddMessages(
                    CreateUnsupportedFeatureResult(node.Default, "default type constraints")
                );
            }

            return result;
        }

        public override Result<object> EmitTypeQuery(TypeQuery node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitTypeTest(TypeTest node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            result.AddMessages(
                EmitNode(node.Subject, childContext, token)
            );

            context.Emission.Append(node, " instanceof ");
            
            result.AddMessages(
                EmitNode(node.Criteria, childContext, token)
            );

            return result;
        }

        public override Result<object> EmitUpperBoundedTypeConstraint(UpperBoundedTypeConstraint node, Context context, CancellationToken token)
        {
            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            context.Emission.Append(node, "extends ");

            return EmitNode(node.Type, context, token);
        }

        public override Result<object> EmitUnionTypeReference(UnionTypeReference node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitViewConstruction(ViewConstruction node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitViewDeclaration(ViewDeclaration node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public override Result<object> EmitWhilePredicateLoop(WhilePredicateLoop node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            context.Emission.AppendBelow(node, "while(");

            result.AddMessages(EmitNode(node.Condition, childContext, token));

            context.Emission.Append(node, ")");

            result.AddMessages(EmitBlockLike(node.Body, node.Node, childContext, token));

            return result;
        }

        public override Result<object> EmitWildcardExportReference(WildcardExportReference node, Context context, CancellationToken token)
        {
            return base.EmitWildcardExportReference(node, context, token);
        }

        private Result<object> EmitBinaryLike(BinaryLike node, string operatorToken, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            var operands = node.Operands;

            result.AddMessages(EmitDelimited(operands, operatorToken, childContext, token));

            return result;
        }

        private Result<object> EmitPrefixUnaryLike(UnaryLike node, string operatorToken, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            // operator
            context.Emission.Append(node, operatorToken);
            
            // operand
            result.AddMessages(
                EmitNode(node.Operand, childContext, token)
            );

            return result;
        }

        private Result<object> EmitBitwiseShift(BitwiseShift node, string operatorToken, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            // left operand
            result.AddMessages(
                EmitNode(node.Subject, childContext, token)
            );

            // operator
            context.Emission.Append(node, operatorToken);

            // right operand
            result.AddMessages(
                EmitNode(node.Offset, childContext, token)
            );

            return result;
        }

        private Result<object> EmitAssignmentLike(AssignmentLike node, string operatorToken, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            result.AddMessages(
                EmitNode(node.Storage, childContext, token)
            );

            // operator
            context.Emission.Append(node, $" {operatorToken} ");

            result.AddMessages(
                EmitNode(node.Value, childContext, token)
            );

            return result;
        }

        #region type parts

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Result<object> EmitTypeDeclarationHeritage(TypeDeclaration node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            var supers = node.Supers;

            if(supers.Length > 0)
            {
                context.Emission.Append(node, " extends ");

                result.AddMessages(
                    EmitCSV(supers, childContext, token)
                );
            }
            
            var interfaces = node.Interfaces;

            if(interfaces.Length > 0)
            {
                context.Emission.Append(node, " implements ");

                result.AddMessages(
                    EmitCSV(interfaces, childContext, token)
                );
            }

            return result;
        }

        // [dho] COPYPASTA from SwiftEmitter - 04/11/18
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Result<object> EmitTypeDeclarationTemplate(TypeDeclaration typeDecl, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var template = typeDecl.Template;

            if (template.Length > 0)
            {
                context.Emission.Append(typeDecl, "<");

                result.AddMessages(
                    EmitCSV(template, context, token)
                );

                context.Emission.Append(typeDecl, ">");
            }

            return result;
        }

        // // [dho] COPYPASTA from SwiftEmitter - 09/11/18
        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // protected Result<object> EmitTypeDeclarationMembers(TypeDeclaration node, Context context, CancellationToken token)
        // {
        //     var result = new Result<object>();

        //     if (node.Members != null)
        //     {
        //         context.Emission.Indent();

        //         foreach (var (member, hasNext) in NodeInterrogation.IterateMembers(node.Members, true /* filter nulls */))
        //         {
        //             context.Emission.AppendBelow(node.Members, "");

        //             result.AddMessages(
        //                 EmitNode(member, context, token)
        //             );

        //             if(Requi)
        //         }

        //         context.Emission.Outdent();
        //     }

        //     return result;
        // }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Result<object> EmitAnnotationsAndModifiers<T>(T node, Context context, CancellationToken token) where T : ASTNode, IAnnotated, IModified
        {
            var result = new Result<object>();

            {
                if(node.Annotations.Length > 0)
                {
                    result.AddMessages(EmitDelimited(node.Annotations, " ", context, token));

                    context.Emission.Append(node, " ");
                }

                if(node.Modifiers.Length > 0)
                {
                    result.AddMessages(EmitDelimited(node.Modifiers, " ", context, token));

                    context.Emission.Append(node, " ");
                }
            }

            return result;
        }

        #endregion

        #region function parts

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Result<object> EmitFunctionName(FunctionLikeDeclaration fn, Context context, CancellationToken token)
        {
            var name = fn.Name;

            if (name.Kind == SemanticKind.Identifier)
            {
                return EmitIdentifier(ASTNodeFactory.Identifier(fn.AST, (DataNode<string>)name), context, token);
            }
            else
            {
                var result = new Result<object>();

                result.AddMessages(
                    new NodeMessage(MessageKind.Error, $"Unsupported name type : '{name.Kind}'", name)
                    {
                        Hint = GetHint(name.Origin),
                        Tags = DiagnosticTags
                    }
                );

                return result;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Result<object> EmitFunctionTemplate<T>(T fn, Context context, CancellationToken token) where T : ASTNode, ITemplated
        {
            var result = new Result<object>();

            var template = fn.Template;

            if (template.Length > 0)
            {
                context.Emission.Append(fn, "<");

                result.AddMessages(
                    EmitCSV(template, context, token)
                );

                context.Emission.Append(fn, ">");
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Result<object> EmitFunctionParameters<T>(T fn, Context context, CancellationToken token) where T : ASTNode, IParametered
        {
            var result = new Result<object>();

            var parameters = fn.Parameters;

            context.Emission.Append(fn, "(");

            if (parameters.Length > 0)
            {
                result.AddMessages(
                    EmitCSV(parameters, context, token)
                );
            }

            context.Emission.Append(fn, ")");

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Result<object> EmitBlockLike(Node body, Node parent, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            if (body != null)
            {
                if (body.Kind == SemanticKind.Block)
                {
                    result.AddMessages(
                        EmitBlock(ASTNodeFactory.Block(context.AST, body), context, token)
                    );
                }
                else
                {
                    context.Emission.Append(body, "{");
                    context.Emission.Indent();

                    context.Emission.AppendBelow(body, "");

                    result.AddMessages(
                        EmitNode(body, context, token)
                    );

                    context.Emission.Outdent();
                    context.Emission.AppendBelow(body, "}");
                }
            }
            else
            {
                context.Emission.Append(parent, "{}");
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Result<object> EmitBlockLike(Node[] members, Node parent, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            if (members?.Length > 0)
            {
                context.Emission.Append(parent, "{");
                context.Emission.Indent();

                for(int i = 0; i < members.Length; ++i)
                {
                    var member = members[i];

                    context.Emission.AppendBelow(member, "");

                    var prevEmissionLength = context.Emission.Length;

                    result.AddMessages(
                        EmitNode(member, context, token)
                    );

                    var didAppend = context.Emission.Length > prevEmissionLength;

                    if(didAppend && RequiresSemicolonSentinel(member))
                    {
                        context.Emission.Append(member, ";");
                    }
                }

                context.Emission.Outdent();
                context.Emission.AppendBelow(parent, "}");
            
            }
            else
            {
                context.Emission.Append(parent, "{}");
            }

            return result;
        }


        #endregion

        #region meta

        const MetaFlag ObjectTypeDeclarationFlags = MetaFlag.Static | TypeDeclarationVisibilityFlags;

        private Result<object> EmitObjectTypeDeclarationFlags(ASTNode node, MetaFlag flags, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            result.AddMessages(EmitTypeDeclarationVisibilityFlags(node, flags, context, token));

            if((flags & MetaFlag.Static) == MetaFlag.Static)
            {
                context.Emission.Append(node, "static ");
            }

            return result;
        }

        const MetaFlag TypeDeclarationVisibilityFlags = MetaFlag.FileVisibility | MetaFlag.SubtypeVisibility | MetaFlag.TypeVisibility | MetaFlag.WorldVisibility;

        private Result<object> EmitTypeDeclarationVisibilityFlags(ASTNode node, MetaFlag flags, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            if((flags & MetaFlag.SubtypeVisibility) == MetaFlag.SubtypeVisibility)
            {
                context.Emission.Append(node, "protected ");
            }

            if((flags & MetaFlag.TypeVisibility) == MetaFlag.TypeVisibility)
            {
                context.Emission.Append(node, "private ");
            }

            if((flags & MetaFlag.WorldVisibility) == MetaFlag.WorldVisibility)
            {
                context.Emission.Append(node, "public ");
            }

            return result;
        }

        const MetaFlag TypeDeclarationMemberFlags = MetaFlag.Constant | MetaFlag.Static | MetaFlag.SubtypeVisibility | MetaFlag.TypeVisibility | MetaFlag.WorldVisibility;

        private Result<object> EmitTypeDeclarationMemberFlags(ASTNode node, MetaFlag flags, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            if((flags & MetaFlag.Constant) == MetaFlag.Constant)
            {
                context.Emission.Append(node, "final ");
            }

            if((flags & MetaFlag.Static) == MetaFlag.Static)
            {
                context.Emission.Append(node, "static ");
            }

            if((flags & MetaFlag.SubtypeVisibility) == MetaFlag.SubtypeVisibility)
            {
                context.Emission.Append(node, "protected ");
            }

            if((flags & MetaFlag.TypeVisibility) == MetaFlag.TypeVisibility)
            {
                context.Emission.Append(node, "private ");
            }

            if((flags & MetaFlag.WorldVisibility) == MetaFlag.WorldVisibility)
            {
                context.Emission.Append(node, "public ");
            }

            return result;
        }

        #endregion

        protected override bool RequiresSemicolonSentinel(Node node)
        {
            switch(node.Kind)
            {
                case SemanticKind.Block:
                case SemanticKind.CodeConstant:
                case SemanticKind.Directive:
                case SemanticKind.DoWhilePredicateLoop:
                case SemanticKind.DoOrDieErrorTrap:
                case SemanticKind.DoOrRecoverErrorTrap:
                case SemanticKind.EnumerationTypeDeclaration:
                case SemanticKind.ErrorTrapJunction:
                case SemanticKind.ForKeysLoop:
                case SemanticKind.ForMembersLoop:
                case SemanticKind.ForPredicateLoop:
                case SemanticKind.FunctionDeclaration:
                case SemanticKind.InterfaceDeclaration:
                case SemanticKind.MatchJunction:
                case SemanticKind.MatchClause:
                case SemanticKind.MethodDeclaration:
                case SemanticKind.NamespaceDeclaration:
                case SemanticKind.ObjectTypeDeclaration:
                case SemanticKind.PredicateJunction:
                case SemanticKind.WhilePredicateLoop:
                    return false;

                default:
                    return true;
            }
        }

        private Result<object> EmitPostUnaryArithmetic(UnaryArithmetic node, string operatorToken, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            result.AddMessages(
                EmitNode(node.Operand, childContext, token)
            );

            context.Emission.Append(node, operatorToken);

            return result;
        }

        private Result<object> EmitPreUnaryArithmetic(UnaryArithmetic node, string operatorToken, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            context.Emission.Append(node, operatorToken);

            var childContext = ContextHelpers.Clone(context);
            // childContext.Parent = node;

            result.AddMessages(
                EmitNode(node.Operand, childContext, token)
            );

            return result;
        }

        private Result<object> EmitCSV(Node[] nodes, Context context, CancellationToken token)
        {
            return EmitDelimited(nodes, ",", context, token);
        }
    }
}