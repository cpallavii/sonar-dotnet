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
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using SonarAnalyzer.Common;
using SonarAnalyzer.Helpers;

namespace SonarAnalyzer.Rules.VisualBasic
{
    [DiagnosticAnalyzer(LanguageNames.VisualBasic)]
    public sealed class EventHandlerName : ParameterLoadingDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "S2347";
        private const string MessageFormat = "Rename event handler '{0}' to match the regular expression: '{1}'.";

        private const string DefaultPattern = "^(([a-z][a-z0-9]*)?" + NamingHelper.PascalCasingInternalPattern + "_)?" + NamingHelper.PascalCasingInternalPattern + "$";

        private static readonly DiagnosticDescriptor Rule = DescriptorFactory.Create(DiagnosticId, MessageFormat, isEnabledByDefault: false);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        [RuleParameter("format", PropertyType.String, "Regular expression used to check the even handler names against.", DefaultPattern)]
        public string Pattern { get; set; } = DefaultPattern;

        protected override void Initialize(ParameterLoadingAnalysisContext context) =>
            context.RegisterSyntaxNodeActionInNonGenerated(
                c =>
                {
                    var methodDeclaration = (MethodStatementSyntax)c.Node;
                    if (!NamingHelper.IsRegexMatch(methodDeclaration.Identifier.ValueText, Pattern)
                        && IsEventHandler(methodDeclaration, c.SemanticModel))
                    {
                        c.ReportIssue(Diagnostic.Create(Rule, methodDeclaration.Identifier.GetLocation(), methodDeclaration.Identifier.ValueText, Pattern));
                    }
                },
                SyntaxKind.SubStatement);

        internal static bool IsEventHandler(MethodStatementSyntax declaration, SemanticModel semanticModel)
        {
            if (declaration.HandlesClause != null)
            {
                return true;
            }

            var symbol = semanticModel.GetDeclaredSymbol(declaration);
            return symbol != null && symbol.IsEventHandler();
        }
    }
}
