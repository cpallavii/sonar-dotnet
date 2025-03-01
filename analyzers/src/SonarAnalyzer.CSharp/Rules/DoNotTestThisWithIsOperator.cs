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

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarAnalyzer.Common;
using SonarAnalyzer.Extensions;
using SonarAnalyzer.Helpers;
using StyleCop.Analyzers.Lightup;

namespace SonarAnalyzer.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DoNotTestThisWithIsOperator : SonarDiagnosticAnalyzer
    {
        private const string DiagnosticId = "S3060";
        private const string MessageFormat = "Offload the code that's conditional on this type test to the appropriate subclass and remove the condition.";

        private static readonly DiagnosticDescriptor Rule = DescriptorFactory.Create(DiagnosticId, MessageFormat);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        protected override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterSyntaxNodeActionInNonGenerated(AnalyzeIsExpression, SyntaxKind.IsExpression);
            context.RegisterSyntaxNodeActionInNonGenerated(AnalyzeIsPatternExpression, SyntaxKindEx.IsPatternExpression);
            context.RegisterSyntaxNodeActionInNonGenerated(AnalyzeSwitchExpression, SyntaxKindEx.SwitchExpression);
            context.RegisterSyntaxNodeActionInNonGenerated(AnalyzeSwitchStatement, SyntaxKind.SwitchStatement);
        }

        private static void AnalyzeIsExpression(SyntaxNodeAnalysisContext context)
        {
            if (IsThisExpressionSyntax(((BinaryExpressionSyntax)context.Node).Left))
            {
                ReportDiagnostic(context, context.Node);
            }
        }

        private static void AnalyzeIsPatternExpression(SyntaxNodeAnalysisContext context)
        {
            if (IsThisExpressionSyntax(((IsPatternExpressionSyntaxWrapper)context.Node).Expression) && ContainsTypeCheckInPattern(context.Node))
            {
                ReportDiagnostic(context, context.Node);
            }
        }

        private static void AnalyzeSwitchStatement(SyntaxNodeAnalysisContext context)
        {
            var switchStatement = (SwitchStatementSyntax)context.Node;
            if (IsThisExpressionSyntax(switchStatement.Expression))
            {
                ReportDiagnostic(context, switchStatement.Expression, CollectSecondaryLocations(switchStatement));
            }
        }

        private static void AnalyzeSwitchExpression(SyntaxNodeAnalysisContext context)
        {
            var switchExpression = (SwitchExpressionSyntaxWrapper)context.Node;
            if (IsThisExpressionSyntax(switchExpression.GoverningExpression))
            {
                 ReportDiagnostic(context, switchExpression.GoverningExpression, CollectSecondaryLocations(switchExpression));
            }
        }

        private static IList<SecondaryLocation> CollectSecondaryLocations(SwitchStatementSyntax switchStatement)
        {
            var secondaryLocations = new List<SecondaryLocation>();
            foreach (var section in switchStatement.Sections)
            {
                foreach (var label in section.Labels)
                {
                    if (ContainsTypeCheckInPattern(label)
                        || ContainsTypeCheckInCaseSwitchLabel(label))
                    {
                        secondaryLocations.Add(new SecondaryLocation(TypeMatchLocation(label), string.Empty));
                    }
                }
            }

            return secondaryLocations;
        }

        private static IList<SecondaryLocation> CollectSecondaryLocations(SwitchExpressionSyntaxWrapper switchExpression)
        {
            var secondaryLocations = new List<SecondaryLocation>();
            foreach (var arm in switchExpression.Arms)
            {
                if (ContainsTypeCheckInPattern(arm.Pattern.SyntaxNode))
                {
                    secondaryLocations.Add(new SecondaryLocation(arm.Pattern.SyntaxNode.GetLocation(), string.Empty));
                }
            }

            return secondaryLocations;
        }

        private static bool ContainsTypeCheckInCaseSwitchLabel(SwitchLabelSyntax switchLabel) =>
              switchLabel is CaseSwitchLabelSyntax caseSwitchLabel && caseSwitchLabel.Value.IsKind(SyntaxKind.IdentifierName);

        private static bool ContainsTypeCheckInPattern(SyntaxNode syntaxNode) =>
            syntaxNode.DescendantNodesAndSelf()
                      .Any(x => x.IsAnyKind(SyntaxKindEx.ConstantPattern, SyntaxKindEx.DeclarationPattern, SyntaxKindEx.RecursivePattern) && IsTypeCheckOnThis(x));

        private static bool IsTypeCheckOnThis(SyntaxNode pattern)
        {
            if (ConstantPatternSyntaxWrapper.IsInstance(pattern))
            {
                return ((ConstantPatternSyntaxWrapper)pattern).Expression.IsKind(SyntaxKind.IdentifierName) && IsNotInSubPattern(pattern);
            }
            else if (DeclarationPatternSyntaxWrapper.IsInstance(pattern))
            {
                return IsNotInSubPattern(pattern);
            }
            else if (RecursivePatternSyntaxWrapper.IsInstance(pattern))
            {
                return ((RecursivePatternSyntaxWrapper)pattern).Type != null && IsNotInSubPattern(pattern);
            }
            else
            {
                return false;
            }
        }

        private static bool IsNotInSubPattern(SyntaxNode node) =>
            !node.FirstAncestorOrSelf<SyntaxNode>(x => x.IsAnyKind(SyntaxKindEx.IsPatternExpression,
                                                                  SyntaxKindEx.SwitchExpression,
                                                                  SyntaxKind.SwitchStatement,
                                                                  SyntaxKindEx.Subpattern))
                                                 .IsKind(SyntaxKindEx.Subpattern);

        private static void ReportDiagnostic(SyntaxNodeAnalysisContext context, SyntaxNode node) =>
            context.ReportIssue(Diagnostic.Create(Rule, node.GetLocation()));

        private static void ReportDiagnostic(SyntaxNodeAnalysisContext context, SyntaxNode node, IList<SecondaryLocation> secondaryLocations)
        {
            if (secondaryLocations.Any())
            {
                context.ReportIssue(Diagnostic.Create(Rule, node.GetLocation(), secondaryLocations.ToAdditionalLocations(), secondaryLocations.ToProperties()));
            }
        }

        private static Location TypeMatchLocation(SwitchLabelSyntax label)
        {
            if (label is CaseSwitchLabelSyntax caseSwitchLabel)
            {
                return caseSwitchLabel.Value.GetLocation();
            }
            else if (CasePatternSwitchLabelSyntaxWrapper.IsInstance(label))
            {
                return ((CasePatternSwitchLabelSyntaxWrapper)label).Pattern.SyntaxNode.GetLocation();
            }
            else
            {
                return Location.None;
            }
        }

        private static bool IsThisExpressionSyntax(SyntaxNode syntaxNode) =>
            syntaxNode.RemoveParentheses() is ThisExpressionSyntax;
    }
}
