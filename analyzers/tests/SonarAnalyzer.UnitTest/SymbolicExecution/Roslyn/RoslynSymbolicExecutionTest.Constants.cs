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

using SonarAnalyzer.SymbolicExecution.Constraints;
using SonarAnalyzer.UnitTest.TestFramework.SymbolicExecution;

namespace SonarAnalyzer.UnitTest.SymbolicExecution.Roslyn
{
    public partial class RoslynSymbolicExecutionTest
    {
        [TestMethod]
        [DataRow("true")]
        [DataRow("false")]
        public void PreProcess_TrueLiteral_BooleanConstraint(string value)
        {
            var expectedConstraint = GetConstraint(value == "true");

            var validator = SETestContext.CreateCS($"var a = {value};").Validator;
            validator.ValidateOrder(
                $"LocalReference: a = {value} (Implicit)",
                $"Literal: {value}",
                $"SimpleAssignment: a = {value} (Implicit)");
            validator.Validate($"Literal: {value}", x => x.State[x.Operation].HasConstraint(expectedConstraint).Should().BeTrue());
            validator.Validate($"SimpleAssignment: a = {value} (Implicit)", x => x.State[x.Operation].HasConstraint(expectedConstraint).Should().BeTrue());
        }

        [TestMethod]
        [DataRow("1", true)]
        [DataRow("2", false)]
        public void PreProcess_EqualityCheck_BooleanConstraint(string constant, bool value)
        {
            var expectedConstraint = GetConstraint(value);
            var validator = SETestContext.CreateCS($"bool a = 1 == {constant};").Validator;
            validator.ValidateOrder(
                $"LocalReference: a = 1 == {constant} (Implicit)",
                "Literal: 1",
                $"Literal: {constant}",
                $"BinaryOperator: 1 == {constant}",
                $"SimpleAssignment: a = 1 == {constant} (Implicit)");
            validator.Validate($"BinaryOperator: 1 == {constant}", x => x.State[x.Operation].HasConstraint(expectedConstraint).Should().BeTrue());
            validator.Validate($"SimpleAssignment: a = 1 == {constant} (Implicit)", x => x.State[x.Operation].HasConstraint(expectedConstraint).Should().BeTrue());
        }

        [TestMethod]
        public void PreProcess_EqualityCheck_ConstantPropagation()
        {
            var validator = SETestContext.CreateCS("const int zero = 0; bool f = 1 == zero, t = 0 == zero;").Validator;
            validator.ValidateOrder(
                "LocalReference: zero = 0 (Implicit)",
                "Literal: 0",
                "SimpleAssignment: zero = 0 (Implicit)",
                "LocalReference: f = 1 == zero (Implicit)",
                "Literal: 1",
                "LocalReference: zero",
                "BinaryOperator: 1 == zero",
                "SimpleAssignment: f = 1 == zero (Implicit)",
                "LocalReference: t = 0 == zero (Implicit)",
                "Literal: 0",
                "LocalReference: zero",
                "BinaryOperator: 0 == zero",
                "SimpleAssignment: t = 0 == zero (Implicit)");
            validator.Validate("BinaryOperator: 1 == zero", x => x.State[x.Operation].HasConstraint(BoolConstraint.False).Should().BeTrue());
            validator.Validate("SimpleAssignment: f = 1 == zero (Implicit)", x => x.State[x.Operation].HasConstraint(BoolConstraint.False).Should().BeTrue());
            validator.Validate("BinaryOperator: 0 == zero", x => x.State[x.Operation].HasConstraint(BoolConstraint.True).Should().BeTrue());
            validator.Validate("SimpleAssignment: t = 0 == zero (Implicit)", x => x.State[x.Operation].HasConstraint(BoolConstraint.True).Should().BeTrue());
        }

        [TestMethod]
        public void PreProcess_OptionalArgument_DoesNotSetConstraint()
        {
            const string code = @"
public void Main(bool arg = true)
{
    var tag = ""End"";
}
private void Tag(string name, object arg) { }";
            SETestContext.CreateCSMethod(code).Validator.ValidateTag("End", "Arg", x => x.Should().BeNull());
        }

        private static BoolConstraint GetConstraint(bool value) =>
            value ? BoolConstraint.True : BoolConstraint.False;
    }
}
