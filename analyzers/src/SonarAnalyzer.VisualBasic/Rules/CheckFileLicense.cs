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
using Microsoft.CodeAnalysis.Diagnostics;
using SonarAnalyzer.Common;
using SonarAnalyzer.Helpers;

namespace SonarAnalyzer.Rules.VisualBasic
{
    [DiagnosticAnalyzer(LanguageNames.VisualBasic)]
    public sealed class CheckFileLicense : CheckFileLicenseBase
    {
        internal const string HeaderFormatDefaultValue =
@"' <Your-Product-Name>
' Copyright (c) <Year-From>-<Year-To> <Your-Company-Name>
'
' Please configure this header in your SonarCloud/SonarQube quality profile.
' You can also set it in SonarLint.xml additional file for SonarLint or standalone NuGet analyzer.
";

        protected override ILanguageFacade Language => VisualBasicFacade.Instance;

        [RuleParameter(HeaderFormatRuleParameterKey, PropertyType.Text, "Expected copyright and license header.", HeaderFormatDefaultValue)]
        public override string HeaderFormat { get; set; } = HeaderFormatDefaultValue;
    }
}
