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
using Microsoft.CodeAnalysis.Diagnostics;
using SonarAnalyzer.Extensions;
using SonarAnalyzer.Helpers;

namespace SonarAnalyzer.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class SqlKeywordsDelimitedBySpace : SonarDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "S2857";
        private const string MessageFormat = "Add a space before '{0}'.";

        private static readonly DiagnosticDescriptor rule =
            DescriptorFactory.Create(DiagnosticId, MessageFormat);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(rule);

        private static readonly IList<NameSyntax> SqlNamespaces = new List<NameSyntax>()
        {
            CSharpSyntaxHelper.BuildQualifiedNameSyntax("System", "Data"),
            CSharpSyntaxHelper.BuildQualifiedNameSyntax("Microsoft", "EntityFrameworkCore"),
            CSharpSyntaxHelper.BuildQualifiedNameSyntax("ServiceStack", "OrmLite"),
            CSharpSyntaxHelper.BuildQualifiedNameSyntax("System", "Data", "SqlClient"),
            CSharpSyntaxHelper.BuildQualifiedNameSyntax("System", "Data", "SQLite"),
            CSharpSyntaxHelper.BuildQualifiedNameSyntax("System", "Data", "SqlServerCe"),
            CSharpSyntaxHelper.BuildQualifiedNameSyntax("System", "Data", "Entity"),
            CSharpSyntaxHelper.BuildQualifiedNameSyntax("System", "Data", "Odbc"),
            CSharpSyntaxHelper.BuildQualifiedNameSyntax("System", "Data", "OracleClient"),
            CSharpSyntaxHelper.BuildQualifiedNameSyntax("Microsoft", "Data", "Sqlite"),
            SyntaxFactory.IdentifierName("Dapper"),
            SyntaxFactory.IdentifierName("NHibernate"),
            SyntaxFactory.IdentifierName("PetaPoco")
        };

        // We are interested in SQL keywords that start a query (so without "FROM", for example)
        private static readonly IList<string> SqlStartQueryKeywords = new List<string>()
        {
            "ALTER",
            "BULK INSERT",
            "CREATE",
            "DELETE",
            "DROP",
            "EXEC",
            "EXECUTE",
            "GRANT",
            "INSERT",
            "MERGE",
            "READTEXT",
            "SELECT",
            "TRUNCATE",
            "UPDATE",
            "UPDATETEXT",
            "WRITETEXT"
        };

        private static readonly int SqlKeywordMinSize = SqlStartQueryKeywords
            .Select(s => s.Length)
            .OrderBy(i => i)
            .First();

        protected override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterSyntaxNodeActionInNonGenerated(
                c =>
                {
                    var namespaceDeclaration = (NamespaceDeclarationSyntax)c.Node;
                    var compilationUnit = namespaceDeclaration.Parent as CompilationUnitSyntax;
                    if (compilationUnit == null ||
                        (!HasSqlNamespace(compilationUnit.Usings) &&
                        !HasSqlNamespace(namespaceDeclaration.Usings)))
                    {
                        return;
                    }
                    var visitor = new StringConcatenationWalker(c);
                    foreach (var member in namespaceDeclaration.Members)
                    {
                        visitor.SafeVisit(member);
                    }
            },
            SyntaxKind.NamespaceDeclaration);
        }

        private bool HasSqlNamespace(SyntaxList<UsingDirectiveSyntax> usings) =>
            usings.Select(usingDirective => usingDirective.Name)
                .Any(name => SqlNamespaces.Any(sn => SyntaxFactory.AreEquivalent(name, sn)));

        private class StringConcatenationWalker : SafeCSharpSyntaxWalker
        {
            private readonly SyntaxNodeAnalysisContext context;

            public StringConcatenationWalker(SyntaxNodeAnalysisContext context)
            {
                this.context = context;
            }

            // The assumption is that in a chain of concatenations "a" + "b" + "c"
            // the AST form is
            //        +
            //       / \
            //      +  "c"
            //     / \
            //   "a" "b"
            // So we start from the lower-left node which should contain the SQL start keyword
            public override void VisitBinaryExpression(BinaryExpressionSyntax node)
            {
                if (node.IsKind(SyntaxKind.AddExpression) &&
                    // we do the analysis only if it's a SQL keyword on the left
                    TryGetStringWrapper(node.Left, out var leftSide) &&
                    TryGetStringWrapper(node.Right, out var rightSide) &&
                    StartsWithSqlKeyword(leftSide.Text.Trim()))
                {
                    var strings = new List<StringWrapper>();
                    strings.Add(leftSide);
                    strings.Add(rightSide);
                    var onlyStringsInConcatenation = AddStringsToList(node, strings);
                    if (!onlyStringsInConcatenation)
                    {
                        return;
                    }

                    CheckSpaceBetweenStrings(strings);
                }
                else
                {
                    base.Visit(node.Left);
                    base.Visit(node.Right);
                }
            }

            private void CheckSpaceBetweenStrings(List<StringWrapper> stringWrappers)
            {
                for (var i = 0; i < stringWrappers.Count -1; i++)
                {
                    var firstStringText = stringWrappers[i].Text;
                    var secondString = stringWrappers[i + 1];
                    var secondStringText = secondString.Text;
                    if (firstStringText.Length > 0 &&
                        IsAlphaNumericOrAt(firstStringText.ToCharArray().Last()) &&
                        secondStringText.Length > 0 &&
                        IsAlphaNumericOrAt(secondStringText[0]))
                    {
                        var word = secondStringText.Split(' ').FirstOrDefault();
                        this.context.ReportIssue(Diagnostic.Create(rule, secondString.Node.GetLocation(), word));
                    }
                }
            }

            private static bool TryGetStringWrapper(ExpressionSyntax expression, out StringWrapper stringWrapper)
            {
                if (expression is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression))
                {
                    stringWrapper = new StringWrapper(literal, literal.Token.ValueText);
                    return true;
                }

                if (expression is InterpolatedStringExpressionSyntax interpolatedString)
                {
                    stringWrapper = new StringWrapper(interpolatedString, interpolatedString.GetContentsText());
                    return true;
                }

                stringWrapper = null;
                return false;
            }

            /**
             * Returns
             * - true if all the found elements are string literals.
             * - false if, inside the chain of binary expressions, some elements are not string literals or
             * some binary expressions are not additions.
             */
            private static bool AddStringsToList(BinaryExpressionSyntax node, List<StringWrapper> strings)
            {
                // this is the left-most node of a concatenation chain
                // collect all string literals
                var parent = node.Parent;
                while (parent is BinaryExpressionSyntax concatenation)
                {
                    if (concatenation.IsKind(SyntaxKind.AddExpression) &&
                        TryGetStringWrapper(concatenation.Right, out var stringWrapper))
                    {
                        strings.Add(stringWrapper);
                    }
                    else
                    {
                        // we are in a binary expression, but it's not only of strings or not only concatenations
                        return false;
                    }
                    parent = parent.Parent;
                }
                return true;
            }

            private static bool StartsWithSqlKeyword(string firstString) =>
                firstString.Length >= SqlKeywordMinSize &&
                SqlStartQueryKeywords.Any(s => firstString.StartsWith(s, StringComparison.OrdinalIgnoreCase));

            /**
             * The '@' symbol is used for named parameters. The '{' and '}' symbols are used in string interpolations.
             * We ignore other non-alphanumeric characters (e.g. '>','=') to avoid false positives.
             */
            private static bool IsAlphaNumericOrAt(char c) => char.IsLetterOrDigit(c) || c == '@' || c == '{' || c == '}';
        }

        private class StringWrapper
        {
            public SyntaxNode Node { get; }
            public string Text { get; }

            internal StringWrapper(SyntaxNode node, string text)
            {
                Node = node;
                Text = text;
            }
        }
    }
}
