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

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarAnalyzer.Helpers;
using StyleCop.Analyzers.Lightup;

namespace SonarAnalyzer.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class TrackNotImplementedException : SonarDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "S3717";
        private const string MessageFormat = "Implement this method or throw 'NotSupportedException' instead.";

        private static readonly DiagnosticDescriptor rule =
            DescriptorFactory.Create(DiagnosticId, MessageFormat);
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(rule);

        protected override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterSyntaxNodeActionInNonGenerated(
                c =>
                {
                    var throwStatement = (ThrowStatementSyntax)c.Node;
                    if (throwStatement.Expression == null)
                    {
                        return;
                    }

                    ReportDiagnostic(c, throwStatement.Expression, throwStatement);
                },
                SyntaxKind.ThrowStatement);

            context.RegisterSyntaxNodeActionInNonGenerated(
                 c =>
                 {
                     var throwExpression = (ThrowExpressionSyntaxWrapper)c.Node;
                     ReportDiagnostic(c, throwExpression.Expression, throwExpression);
                 },
                 SyntaxKindEx.ThrowExpression);
        }

        private static void ReportDiagnostic(SyntaxNodeAnalysisContext c, ExpressionSyntax newExceptionExpression, SyntaxNode throwExpression)
        {
            if (c.SemanticModel.GetTypeInfo(newExceptionExpression).Type.Is(KnownType.System_NotImplementedException))
            {
                c.ReportIssue(Diagnostic.Create(rule, throwExpression.GetLocation()));
            }
        }
    }
}
