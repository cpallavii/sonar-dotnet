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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SonarAnalyzer.Helpers;

namespace SonarAnalyzer.Extensions
{
    internal static class AttributeSyntaxExtensions
    {
        private const int AttributeLength = 9;

        public static bool IsKnownType(this AttributeSyntax attribute, KnownType knownType, SemanticModel semanticModel) =>
            attribute.Name.GetName().Contains(GetShortNameWithoutAttributeSuffix(knownType))
            && SymbolHelper.IsKnownType(attribute, knownType, semanticModel);

        private static string GetShortNameWithoutAttributeSuffix(KnownType knownType) =>
            knownType.TypeName == nameof(Attribute) || !knownType.TypeName.EndsWith(nameof(Attribute))
                ? knownType.TypeName
                : knownType.TypeName.Remove(knownType.TypeName.Length - AttributeLength);
    }
}
