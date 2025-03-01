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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarAnalyzer.Extensions;
using SonarAnalyzer.Helpers;
using SonarAnalyzer.Wrappers;
using StyleCop.Analyzers.Lightup;

namespace SonarAnalyzer.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class MethodsShouldNotHaveIdenticalImplementations : MethodsShouldNotHaveIdenticalImplementationsBase<SyntaxKind, IMethodDeclaration>
    {
        protected override ILanguageFacade<SyntaxKind> Language => CSharpFacade.Instance;

        protected override SyntaxKind[] SyntaxKinds => new[]
        {
            SyntaxKind.ClassDeclaration,
            SyntaxKindEx.RecordClassDeclaration,
            SyntaxKind.StructDeclaration,
            SyntaxKindEx.RecordStructDeclaration,
            SyntaxKind.InterfaceDeclaration,
            SyntaxKind.CompilationUnit
        };

        protected override IEnumerable<IMethodDeclaration> GetMethodDeclarations(SyntaxNode node) =>
            node.IsKind(SyntaxKind.CompilationUnit)
            ? ((CompilationUnitSyntax)node).GetMethodDeclarations()
            : ((TypeDeclarationSyntax)node).GetMethodDeclarations();

        protected override bool AreDuplicates(IMethodDeclaration firstMethod, IMethodDeclaration secondMethod) =>
            firstMethod is { Body: { Statements: { Count: > 1 } } }
            && firstMethod.Identifier.ValueText != secondMethod.Identifier.ValueText
            && HaveSameParameters<ParameterSyntax>(firstMethod.ParameterList.Parameters, secondMethod.ParameterList.Parameters)
            && firstMethod.Body.IsEquivalentTo(secondMethod.Body, false);

        protected override SyntaxToken GetMethodIdentifier(IMethodDeclaration method) =>
            method.Identifier;

        protected override bool IsExcludedFromBeingExamined(ISymbol nodeContainingSymbol) =>
            base.IsExcludedFromBeingExamined(nodeContainingSymbol) && !nodeContainingSymbol.IsTopLevelMain();
    }
}
