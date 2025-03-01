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

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;

namespace SonarAnalyzer.UnitTest.TestFramework.Tests
{
    [TestClass]
    public class OldVerifierTest
    {
        [TestMethod]
        public void VerifyCodeFix_WithDuplicateIssues()
        {
            const string filename = @"TestCases\VerifyCodeFix.Empty.cs";
            Action a = () => { OldVerifier.VerifyCodeFix<TestDuplicateLocationRuleCodeFix>(filename, filename, new TestDuplicateLocationRule()); };
            a.Should().NotThrow();
        }

        [Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer(LanguageNames.CSharp)]
        private class TestDuplicateLocationRule : SonarDiagnosticAnalyzer
        {
            public const string DiagnosticId = "Test42";
            private readonly DiagnosticDescriptor rule = TestHelper.CreateDescriptor(DiagnosticId, DiagnosticDescriptorFactory.MainSourceScopeTag);
            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(rule);

            protected override void Initialize(SonarAnalysisContext context)
            {
                context.RegisterSyntaxNodeAction(c =>
                {
                    // Duplicate issues from different analyzer versions, see https://github.com/SonarSource/sonar-dotnet/issues/1109
                    c.ReportDiagnostic(Diagnostic.Create(this.rule, c.Node.GetLocation()));
                    c.ReportDiagnostic(Diagnostic.Create(this.rule, c.Node.GetLocation()));
                }, SyntaxKind.NamespaceDeclaration);
            }
        }

        [ExportCodeFixProvider(LanguageNames.CSharp)]
        private class TestDuplicateLocationRuleCodeFix : SonarCodeFix
        {
            public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(TestDuplicateLocationRule.DiagnosticId);

            protected override Task RegisterCodeFixesAsync(SyntaxNode root, CodeFixContext context)
            {
                context.RegisterCodeFix(CodeAction.Create("TestTitle", c => Task.FromResult(context.Document)), context.Diagnostics);
                return Task.CompletedTask;
            }
        }
    }
}
