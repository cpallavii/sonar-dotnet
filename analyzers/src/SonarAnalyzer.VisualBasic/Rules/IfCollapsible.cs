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
using SonarAnalyzer.Helpers;

namespace SonarAnalyzer.Rules.VisualBasic
{
    [DiagnosticAnalyzer(LanguageNames.VisualBasic)]
    public sealed class IfCollapsible : IfCollapsibleBase
    {
        private static readonly DiagnosticDescriptor rule =
            DescriptorFactory.Create(DiagnosticId, MessageFormat);
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(rule);

        protected override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterSyntaxNodeActionInNonGenerated(
                c =>
                {
                    var multilineIfBlock = (MultiLineIfBlockSyntax)c.Node;

                    if (multilineIfBlock.ElseIfBlocks.Count > 0 ||
                        multilineIfBlock.ElseBlock != null)
                    {
                        return;
                    }

                    var parentMultilineIfBlock = multilineIfBlock.Parent as MultiLineIfBlockSyntax;

                    if (parentMultilineIfBlock == null ||
                        parentMultilineIfBlock.ElseIfBlocks.Count != 0 ||
                        parentMultilineIfBlock.ElseBlock != null ||
                        parentMultilineIfBlock.Statements.Count != 1)
                    {
                        return;
                    }

                    c.ReportIssue(Diagnostic.Create(
                        rule,
                        multilineIfBlock.IfStatement.IfKeyword.GetLocation(),
                        additionalLocations: new[] { parentMultilineIfBlock.IfStatement.IfKeyword.GetLocation() }));
                },
                SyntaxKind.MultiLineIfBlock);
        }
    }
}

