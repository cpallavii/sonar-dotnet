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

using SonarAnalyzer.Rules.CSharp;

namespace SonarAnalyzer.UnitTest.Rules
{
    [TestClass]
    public class DoNotShiftByZeroOrIntSizeTest
    {
        [TestMethod]
        public void DoNotShiftByZeroOrIntSize() =>
            OldVerifier.VerifyAnalyzer(@"TestCases\DoNotShiftByZeroOrIntSize.cs", new DoNotShiftByZeroOrIntSize());

#if NET
        [TestMethod]
        public void DoNotShiftByZeroOrIntSize_CSharp9() =>
            OldVerifier.VerifyAnalyzerFromCSharp9Console(@"TestCases\DoNotShiftByZeroOrIntSize.CSharp9.cs", new DoNotShiftByZeroOrIntSize());

        [TestMethod]
        public void DoNotShiftByZeroOrIntSize_CSharp10() =>
            OldVerifier.VerifyAnalyzerFromCSharp10Library(@"TestCases\DoNotShiftByZeroOrIntSize.CSharp10.cs", new DoNotShiftByZeroOrIntSize());
#endif
    }
}
