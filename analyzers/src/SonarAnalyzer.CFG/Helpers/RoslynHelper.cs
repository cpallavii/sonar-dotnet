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

namespace SonarAnalyzer.CFG.Helpers
{
    internal static class RoslynHelper
    {
        private const int MinimalSupportedMajorVersion = 3;

        public static bool IsRoslynCfgSupported(int minimalVersion = MinimalSupportedMajorVersion) =>
            typeof(SemanticModel).Assembly.GetName().Version.Major >= minimalVersion;

        public static Type FlowAnalysisType(string typeName) =>
            typeof(SemanticModel).Assembly.GetType("Microsoft.CodeAnalysis.FlowAnalysis." + typeName);
    }
}
