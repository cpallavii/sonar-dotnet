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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using SonarAnalyzer.Helpers;
using SonarAnalyzer.Metrics.CSharp;
using StyleCop.Analyzers.Lightup;

namespace SonarAnalyzer.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class CognitiveComplexity : CognitiveComplexityBase<SyntaxKind>
    {
        protected override ILanguageFacade<SyntaxKind> Language => CSharpFacade.Instance;

        protected override void Initialize(ParameterLoadingAnalysisContext context)
        {
            context.RegisterSyntaxNodeActionInNonGenerated(c =>
                {
                    if (c.ContainingSymbol.IsTopLevelMain())
                    {
                        CheckComplexity<CompilationUnitSyntax>(
                            c,
                            compilationUnit => compilationUnit,
                            _ => Location.Create(c.Node.SyntaxTree, TextSpan.FromBounds(0, 0)),
                            node => CSharpCognitiveComplexityMetric.GetComplexity(node, true),
                            "top-level file",
                            Threshold);
                    }
                },
                SyntaxKind.CompilationUnit);

            context.RegisterSyntaxNodeActionInNonGenerated(
                c => CheckComplexity<MethodDeclarationSyntax>(
                    c,
                    m => m,
                    m => m.Identifier.GetLocation(),
                    CSharpCognitiveComplexityMetric.GetComplexity,
                    "method",
                    Threshold),
                SyntaxKind.MethodDeclaration);

            // Here, we only care about arrowed properties, others will be handled by the accessor.
            context.RegisterSyntaxNodeActionInNonGenerated(
                c => CheckComplexity<PropertyDeclarationSyntax>(
                    c,
                    p => p.ExpressionBody,
                    p => p.Identifier.GetLocation(),
                    CSharpCognitiveComplexityMetric.GetComplexity,
                    "property",
                    PropertyThreshold),
                SyntaxKind.PropertyDeclaration);

            context.RegisterSyntaxNodeActionInNonGenerated(
                c => CheckComplexity<ConstructorDeclarationSyntax>(
                    c,
                    co => co,
                    co => co.Identifier.GetLocation(),
                    CSharpCognitiveComplexityMetric.GetComplexity,
                    "constructor",
                    Threshold),
                SyntaxKind.ConstructorDeclaration);

            context.RegisterSyntaxNodeActionInNonGenerated(
                c => CheckComplexity<DestructorDeclarationSyntax>(
                    c,
                    d => d,
                    d => d.Identifier.GetLocation(),
                    CSharpCognitiveComplexityMetric.GetComplexity,
                    "destructor",
                    Threshold),
                SyntaxKind.DestructorDeclaration);

            context.RegisterSyntaxNodeActionInNonGenerated(
                c => CheckComplexity<OperatorDeclarationSyntax>(
                    c,
                    o => o,
                    o => o.OperatorToken.GetLocation(),
                    CSharpCognitiveComplexityMetric.GetComplexity,
                    "operator",
                    Threshold),
                SyntaxKind.OperatorDeclaration);

            context.RegisterSyntaxNodeActionInNonGenerated(
                c => CheckComplexity<AccessorDeclarationSyntax>(
                    c,
                    a => a,
                    a => a.Keyword.GetLocation(),
                    CSharpCognitiveComplexityMetric.GetComplexity,
                    "accessor",
                    PropertyThreshold),
                SyntaxKind.GetAccessorDeclaration,
                SyntaxKind.SetAccessorDeclaration,
                SyntaxKindEx.InitAccessorDeclaration,
                SyntaxKind.AddAccessorDeclaration,
                SyntaxKind.RemoveAccessorDeclaration);

            context.RegisterSyntaxNodeActionInNonGenerated(
               c => CheckComplexity<FieldDeclarationSyntax>(
                   c,
                   f => f,
                   f => f.Declaration.Variables[0].Identifier.GetLocation(),
                   CSharpCognitiveComplexityMetric.GetComplexity,
                   "field",
                   Threshold),
               SyntaxKind.FieldDeclaration);

            context.RegisterSyntaxNodeActionInNonGenerated(c =>
            {
                if (((LocalFunctionStatementSyntaxWrapper)c.Node).Modifiers.Any(SyntaxKind.StaticKeyword))
                {
                   CheckComplexity<SyntaxNode>(
                       c,
                       m => m,
                       m => ((LocalFunctionStatementSyntaxWrapper)m).Identifier.GetLocation(),
                       CSharpCognitiveComplexityMetric.GetComplexity,
                       "static local function",
                       Threshold);
                }
            },
            SyntaxKindEx.LocalFunctionStatement);
        }
    }
}
