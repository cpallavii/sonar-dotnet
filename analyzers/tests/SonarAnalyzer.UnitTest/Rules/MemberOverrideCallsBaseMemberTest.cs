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
    public class MemberOverrideCallsBaseMemberTest
    {
        private readonly VerifierBuilder verifier = new VerifierBuilder<MemberOverrideCallsBaseMember>();

        [TestMethod]
        public void MemberOverrideCallsBaseMember() =>
            verifier.AddPaths("MemberOverrideCallsBaseMember.cs").Verify();

#if NET
        [TestMethod]
        public void MemberOverrideCallsBaseMember_CSharp10() =>
            verifier.AddPaths("MemberOverrideCallsBaseMember.CSharp10.cs").WithOptions(ParseOptionsHelper.FromCSharp10).Verify();

        [TestMethod]
        public void MemberOverrideCallsBaseMember_CSharp10_CodeFix() => verifier
            .AddPaths("MemberOverrideCallsBaseMember.CSharp10.cs")
            .WithOptions(ParseOptionsHelper.FromCSharp10)
            .WithCodeFix<MemberOverrideCallsBaseMemberCodeFix>()
            .WithCodeFixedPaths("MemberOverrideCallsBaseMember.CSharp10.Fixed.cs")
            .VerifyCodeFix();
#endif

        [TestMethod]
        public void MemberOverrideCallsBaseMember_CodeFix() => verifier
            .AddPaths("MemberOverrideCallsBaseMember.cs")
            .WithCodeFix<MemberOverrideCallsBaseMemberCodeFix>()
            .WithCodeFixedPaths("MemberOverrideCallsBaseMember.Fixed.cs")
            .VerifyCodeFix();

        [TestMethod]
        public void MemberOverrideCallsBaseMember_ToString()
        {
            var toString = "public override string ToString() => base.ToString();";
            toString +=
#if NET
                "// Noncompliant {{Remove this method 'ToString' to simply inherit its behavior.}}";
#elif NETFRAMEWORK
                "// FN. ToString has a [__DynamicallyInvokable] attribute in .Net framework";
#endif
            verifier.AddSnippet($@"
                class Test
                {{
                    {toString}
                }}
                ").Verify();
        }
    }
}
