﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2022 SonarSource SA
 * mailto: contact AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SonarAnalyzer.Extensions;
using StyleCop.Analyzers.Lightup;

namespace SonarAnalyzer.Helpers
{
    internal static class CSharpSyntaxHelper
    {
        public static readonly ExpressionSyntax NullLiteralExpression =
            SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);

        public static readonly ExpressionSyntax FalseLiteralExpression =
            SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression);

        public static readonly ExpressionSyntax TrueLiteralExpression =
            SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression);

        public static readonly string NameOfKeywordText =
            SyntaxFacts.GetText(SyntaxKind.NameOfKeyword);

        private static readonly SyntaxKind[] LiteralSyntaxKinds =
            new[]
            {
                SyntaxKind.CharacterLiteralExpression,
                SyntaxKind.FalseLiteralExpression,
                SyntaxKind.NullLiteralExpression,
                SyntaxKind.NumericLiteralExpression,
                SyntaxKind.StringLiteralExpression,
                SyntaxKind.TrueLiteralExpression,
            };

        public static bool AnyOfKind(this IEnumerable<SyntaxNode> nodes, SyntaxKind kind) =>
            nodes.Any(n => n.RawKind == (int)kind);

        public static bool AnyOfKind(this IEnumerable<SyntaxToken> tokens, SyntaxKind kind) =>
            tokens.Any(n => n.RawKind == (int)kind);

        public static SyntaxNode GetTopMostContainingMethod(this SyntaxNode node) =>
            node.AncestorsAndSelf().LastOrDefault(ancestor => ancestor is BaseMethodDeclarationSyntax || ancestor is PropertyDeclarationSyntax);

        public static SyntaxNode GetSelfOrTopParenthesizedExpression(this SyntaxNode node)
        {
            var current = node;
            while (current?.Parent?.IsKind(SyntaxKind.ParenthesizedExpression) ?? false)
            {
                current = current.Parent;
            }
            return current;
        }

        public static ExpressionSyntax GetSelfOrTopParenthesizedExpression(this ExpressionSyntax node) =>
             (ExpressionSyntax)GetSelfOrTopParenthesizedExpression((SyntaxNode)node);

        public static SyntaxNode GetFirstNonParenthesizedParent(this SyntaxNode node) =>
            node.GetSelfOrTopParenthesizedExpression().Parent;

        public static IEnumerable<AttributeSyntax> GetAttributes(this SyntaxList<AttributeListSyntax> attributeLists, KnownType attributeKnownType, SemanticModel semanticModel) =>
            attributeLists.SelectMany(x => x.Attributes).Where(x => x.IsKnownType(attributeKnownType, semanticModel));

        public static IEnumerable<AttributeSyntax> GetAttributes(this SyntaxList<AttributeListSyntax> attributeLists,
            ImmutableArray<KnownType> attributeKnownTypes, SemanticModel semanticModel) =>
            attributeLists.SelectMany(list => list.Attributes)
                .Where(a => semanticModel.GetTypeInfo(a).Type.IsAny(attributeKnownTypes));

        public static bool IsOnThis(this ExpressionSyntax expression) =>
            IsOn(expression, SyntaxKind.ThisExpression);

        public static bool IsOnBase(this ExpressionSyntax expression) =>
            IsOn(expression, SyntaxKind.BaseExpression);

        private static bool IsOn(this ExpressionSyntax expression, SyntaxKind onKind)
        {
            switch (expression.Kind())
            {
                case SyntaxKind.InvocationExpression:
                    return IsOn(((InvocationExpressionSyntax)expression).Expression, onKind);

                case SyntaxKind.AliasQualifiedName:
                case SyntaxKind.GenericName:
                case SyntaxKind.IdentifierName:
                case SyntaxKind.QualifiedName:
                    // This is a simplification as we don't check where the method is defined (so this could be this or base)
                    return true;

                case SyntaxKind.PointerMemberAccessExpression:
                case SyntaxKind.SimpleMemberAccessExpression:
                    return ((MemberAccessExpressionSyntax)expression).Expression.RemoveParentheses().IsKind(onKind);

                case SyntaxKind.ConditionalAccessExpression:
                    return ((ConditionalAccessExpressionSyntax)expression).Expression.RemoveParentheses().IsKind(onKind);

                default:
                    return false;
            }
        }

        public static bool IsInNameOfArgument(this ExpressionSyntax expression, SemanticModel semanticModel)
        {
            var parentInvocation = expression.FirstAncestorOrSelf<InvocationExpressionSyntax>();
            return parentInvocation != null && parentInvocation.IsNameof(semanticModel);
        }

        public static bool IsNameof(this InvocationExpressionSyntax expression, SemanticModel semanticModel) =>
            expression != null &&
            expression.Expression is IdentifierNameSyntax identifierNameSyntax &&
            identifierNameSyntax.Identifier.ValueText == NameOfKeywordText &&
            semanticModel.GetSymbolInfo(expression).Symbol?.Kind != SymbolKind.Method;

        public static bool IsStringEmpty(this ExpressionSyntax expression, SemanticModel semanticModel)
        {
            if (!expression.IsKind(SyntaxKind.SimpleMemberAccessExpression) &&
                !expression.IsKind(SyntaxKind.PointerMemberAccessExpression))
            {
                return false;
            }

            var nameSymbolInfo = semanticModel.GetSymbolInfo(((MemberAccessExpressionSyntax)expression).Name);

            return nameSymbolInfo.Symbol != null &&
                   nameSymbolInfo.Symbol.IsInType(KnownType.System_String) &&
                   nameSymbolInfo.Symbol.Name == nameof(string.Empty);
        }

        public static bool IsNullLiteral(this SyntaxNode syntaxNode) =>
            syntaxNode != null && syntaxNode.IsKind(SyntaxKind.NullLiteralExpression);

        public static bool IsAnyKind(this SyntaxNode syntaxNode, params SyntaxKind[] syntaxKinds) =>
            syntaxNode != null && syntaxKinds.Contains((SyntaxKind)syntaxNode.RawKind);

        public static bool IsAnyKind(this SyntaxNode syntaxNode, ISet<SyntaxKind> syntaxKinds) =>
            syntaxNode != null && syntaxKinds.Contains((SyntaxKind)syntaxNode.RawKind);

        public static bool IsAnyKind(this SyntaxToken syntaxToken, params SyntaxKind[] syntaxKinds) =>
            syntaxKinds.Contains((SyntaxKind)syntaxToken.RawKind);

        public static bool IsAnyKind(this SyntaxToken syntaxToken, ISet<SyntaxKind> syntaxKinds) =>
            syntaxKinds.Contains((SyntaxKind)syntaxToken.RawKind);

        public static bool IsAnyKind(this SyntaxTrivia syntaxTravia, params SyntaxKind[] syntaxKinds) =>
            syntaxKinds.Contains((SyntaxKind)syntaxTravia.RawKind);

        public static bool ContainsMethodInvocation(this BaseMethodDeclarationSyntax methodDeclarationBase,
            SemanticModel semanticModel,
            Func<InvocationExpressionSyntax, bool> syntaxPredicate, Func<IMethodSymbol, bool> symbolPredicate)
        {
            var childNodes = methodDeclarationBase?.Body?.DescendantNodes()
                ?? methodDeclarationBase?.ExpressionBody()?.DescendantNodes()
                ?? Enumerable.Empty<SyntaxNode>();

            // See issue: https://github.com/SonarSource/sonar-dotnet/issues/416
            // Where clause excludes nodes that are not defined on the same SyntaxTree as the SemanticModel
            // (because of partial definition).
            // More details: https://github.com/dotnet/roslyn/issues/18730
            return childNodes
                .OfType<InvocationExpressionSyntax>()
                .Where(syntaxPredicate)
                .Select(e => e.Expression.SyntaxTree.GetSemanticModelOrDefault(semanticModel)?.GetSymbolInfo(e.Expression).Symbol)
                .OfType<IMethodSymbol>()
                .Any(symbolPredicate);
        }

        public static SyntaxToken? GetIdentifierOrDefault(this BaseMethodDeclarationSyntax methodDeclaration)
        {
            switch (methodDeclaration?.Kind())
            {
                case SyntaxKind.ConstructorDeclaration:
                    return ((ConstructorDeclarationSyntax)methodDeclaration).Identifier;

                case SyntaxKind.DestructorDeclaration:
                    return ((DestructorDeclarationSyntax)methodDeclaration).Identifier;

                case SyntaxKind.MethodDeclaration:
                    return ((MethodDeclarationSyntax)methodDeclaration).Identifier;

                default:
                    return null;
            }
        }

        public static SyntaxToken? GetMethodCallIdentifier(this InvocationExpressionSyntax invocation)
        {
            if (invocation == null)
            {
                return null;
            }
            var expression = invocation.Expression;
            switch (expression.Kind())
            {
                case SyntaxKind.IdentifierName:
                    // method()
                    return ((IdentifierNameSyntax)expression).Identifier;

                case SyntaxKind.SimpleMemberAccessExpression:
                    // foo.method()
                    return ((MemberAccessExpressionSyntax)expression).Name.Identifier;

                case SyntaxKind.MemberBindingExpression:
                    // foo?.method()
                    return ((MemberBindingExpressionSyntax)expression).Name.Identifier;

                default:
                    return null;
            }
        }

        public static bool IsMethodInvocation(this InvocationExpressionSyntax invocation, KnownType type, string methodName, SemanticModel semanticModel) =>
            invocation.Expression.NameIs(methodName) &&
            semanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol methodSymbol &&
            methodSymbol.IsInType(type);

        public static bool IsMethodInvocation(this InvocationExpressionSyntax invocation, ImmutableArray<KnownType> types, string methodName, SemanticModel semanticModel) =>
            invocation.Expression.NameIs(methodName) &&
            semanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol methodSymbol &&
            methodSymbol.IsInType(types);

        public static bool IsPropertyInvocation(this MemberAccessExpressionSyntax expression, ImmutableArray<KnownType> types, string propertyName, SemanticModel semanticModel) =>
            expression.NameIs(propertyName) &&
            semanticModel.GetSymbolInfo(expression).Symbol is IPropertySymbol propertySymbol &&
            propertySymbol.IsInType(types);

        public static Location FindIdentifierLocation(this BaseMethodDeclarationSyntax methodDeclaration) =>
            GetIdentifierOrDefault(methodDeclaration)?.GetLocation();

        /// <summary>
        /// Determines whether the node is being used as part of an expression tree
        /// i.e. whether it is part of lambda being assigned to System.Linq.Expressions.Expression[TDelegate].
        /// This could be a local declaration, an assignment, a field, or a property
        /// </summary>
        public static bool IsInExpressionTree(this SyntaxNode node, SemanticModel semanticModel)
        {
            // Possible ancestors:
            // * VariableDeclarationSyntax (for local variable or field),
            // * PropertyDeclarationSyntax,
            // * SimpleAssigmentExpression
            foreach (var n in node.Ancestors())
            {
                switch (n.Kind())
                {
                    case SyntaxKind.FromClause:
                    case SyntaxKind.LetClause:
                    case SyntaxKind.JoinClause:
                    case SyntaxKind.JoinIntoClause:
                    case SyntaxKind.WhereClause:
                    case SyntaxKind.OrderByClause:
                    case SyntaxKind.SelectClause:
                    case SyntaxKind.GroupClause:
                        // For those clauses, we don't know how to differentiate an expression tree from a delegate,
                        // so we assume we are in the (more restricted) expression tree
                        return true;
                    case SyntaxKind.SimpleLambdaExpression:
                    case SyntaxKind.ParenthesizedLambdaExpression:
                        return semanticModel.GetTypeInfo(n).ConvertedType?.OriginalDefinition.Is(KnownType.System_Linq_Expressions_Expression_T)
                            ?? false;
                    default:
                        continue;
                }
            }
            return false;
        }

        public static bool HasDefaultLabel(this SwitchStatementSyntax node) =>
            GetDefaultLabelSectionIndex(node) >= 0;

        public static int GetDefaultLabelSectionIndex(this SwitchStatementSyntax node) =>
            node.Sections.IndexOf(section => section.Labels.AnyOfKind(SyntaxKind.DefaultSwitchLabel));

        public static bool HasBodyOrExpressionBody(this AccessorDeclarationSyntax node) =>
            node.Body != null || node.ExpressionBody() != null;

        public static bool HasBodyOrExpressionBody(this BaseMethodDeclarationSyntax node) =>
            node?.Body != null || node?.ExpressionBody() != null;

        public static SimpleNameSyntax GetIdentifier(this ExpressionSyntax expression) =>
            expression switch
            {
                MemberBindingExpressionSyntax memberBinding => memberBinding.Name,
                MemberAccessExpressionSyntax memberAccess => memberAccess.Name,
                QualifiedNameSyntax qualified => qualified.Right,
                IdentifierNameSyntax identifier => identifier,
                _ => null
            };

        public static string GetName(this ExpressionSyntax expression) =>
            expression switch
            {
                MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText,
                QualifiedNameSyntax qualifiedName => qualifiedName.Right.Identifier.ValueText,
                SimpleNameSyntax simpleNameSyntax => simpleNameSyntax.Identifier.ValueText,
                NameSyntax nameSyntax => nameSyntax.GetText().ToString(),
                _ => string.Empty
            };

        public static bool NameIs(this ExpressionSyntax expression, string name) =>
            expression.GetName().Equals(name, StringComparison.InvariantCulture);

        public static bool HasConstantValue(this ExpressionSyntax expression, SemanticModel semanticModel) =>
            expression.RemoveParentheses().IsAnyKind(LiteralSyntaxKinds) || expression.FindConstantValue(semanticModel) != null;

        public static string GetStringValue(this SyntaxNode node) =>
            node != null &&
            node.IsKind(SyntaxKind.StringLiteralExpression) &&
            node is LiteralExpressionSyntax literal
                ? literal.Token.ValueText
                : null;

        public static bool IsLeftSideOfAssignment(this ExpressionSyntax expression)
        {
            var topParenthesizedExpression = expression.GetSelfOrTopParenthesizedExpression();
            return topParenthesizedExpression.Parent.IsKind(SyntaxKind.SimpleAssignmentExpression)
                   && topParenthesizedExpression.Parent is AssignmentExpressionSyntax assignment
                   && assignment.Left == topParenthesizedExpression;
        }

        public static bool IsComment(this SyntaxTrivia trivia)
        {
            switch (trivia.Kind())
            {
                case SyntaxKind.SingleLineCommentTrivia:
                case SyntaxKind.MultiLineCommentTrivia:
                case SyntaxKind.SingleLineDocumentationCommentTrivia:
                case SyntaxKind.MultiLineDocumentationCommentTrivia:
                    return true;

                default:
                    return false;
            }
        }

        // creates a QualifiedNameSyntax "a.b"
        public static QualifiedNameSyntax BuildQualifiedNameSyntax(string a, string b) =>
            SyntaxFactory.QualifiedName(
                SyntaxFactory.IdentifierName(a),
                SyntaxFactory.IdentifierName(b));

        // creates a QualifiedNameSyntax "a.b.c"
        public static QualifiedNameSyntax BuildQualifiedNameSyntax(string a, string b, string c) =>
            SyntaxFactory.QualifiedName(
                SyntaxFactory.QualifiedName(
                    SyntaxFactory.IdentifierName(a),
                    SyntaxFactory.IdentifierName(b)),
                SyntaxFactory.IdentifierName(c));

        /// <summary>
        /// Returns argument expressions for given parameter.
        ///
        /// There can be zero, one or more results based on parameter type (Optional or ParamArray/params).
        /// </summary>
        public static ImmutableArray<SyntaxNode> ArgumentValuesForParameter(SemanticModel semanticModel, ArgumentListSyntax argumentList, string parameterName)
        {
            var methodParameterLookup = new CSharpMethodParameterLookup(argumentList, semanticModel);
            return methodParameterLookup.TryGetSyntax(parameterName, out var expressions)
                ? expressions
                : ImmutableArray<SyntaxNode>.Empty;
        }
    }
}
