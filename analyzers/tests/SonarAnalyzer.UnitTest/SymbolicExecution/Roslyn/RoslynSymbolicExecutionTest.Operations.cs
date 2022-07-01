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

using Microsoft.CodeAnalysis.Operations;
using SonarAnalyzer.SymbolicExecution.Constraints;
using SonarAnalyzer.SymbolicExecution.Roslyn;
using SonarAnalyzer.UnitTest.TestFramework.SymbolicExecution;
using StyleCop.Analyzers.Lightup;

namespace SonarAnalyzer.UnitTest.SymbolicExecution.Roslyn
{
    public partial class RoslynSymbolicExecutionTest
    {
        [TestMethod]
        public void SimpleAssignment_ToLocalVariable_FromLiteral()
        {
            var validator = SETestContext.CreateCS(@"var a = true; var tag = ""End"";", new LiteralDummyTestCheck()).Validator;
            validator.Validate("Literal: true", x => x.State[x.Operation].HasConstraint(DummyConstraint.Dummy).Should().BeTrue("it's scaffolded"));
            validator.Validate("SimpleAssignment: a = true (Implicit)", x => x.State[x.Operation].HasConstraint(DummyConstraint.Dummy).Should().BeTrue());
            validator.Validate("SimpleAssignment: a = true (Implicit)", x => x.State[((ISimpleAssignmentOperation)x.Operation.Instance).Target].HasConstraint(DummyConstraint.Dummy).Should().BeTrue());
            validator.ValidateTag("End", "a", x => x.HasConstraint(DummyConstraint.Dummy).Should().BeTrue());
        }

        [TestMethod]
        public void SimpleAssignment_ToLocalVariable_FromTrackedSymbol()
        {
            var validator = SETestContext.CreateCS(@"bool a = true, b; b = a; var tag = ""End"";", new LiteralDummyTestCheck()).Validator;
            validator.Validate("Literal: true", x => x.State[x.Operation].HasConstraint(DummyConstraint.Dummy).Should().BeTrue("it's scaffolded"));
            validator.Validate("SimpleAssignment: b = a", x => x.State[x.Operation].HasConstraint(DummyConstraint.Dummy).Should().BeTrue());
            validator.Validate("SimpleAssignment: b = a", x => x.State[((ISimpleAssignmentOperation)x.Operation.Instance).Target].HasConstraint(DummyConstraint.Dummy).Should().BeTrue());
            validator.ValidateTag("End", "b", x => x.HasConstraint(DummyConstraint.Dummy).Should().BeTrue());
        }

        [TestMethod]
        public void SimpleAssignment_ToLocalVariable_FromTrackedSymbol_Chained()
        {
            var validator = SETestContext.CreateCS(@"bool a = true, b, c; c = b = a; var tag = ""End"";", new LiteralDummyTestCheck()).Validator;
            validator.Validate("Literal: true", x => x.State[x.Operation].HasConstraint(DummyConstraint.Dummy).Should().BeTrue("it's scaffolded"));
            validator.Validate("SimpleAssignment: c = b = a", x => x.State[x.Operation].HasConstraint(DummyConstraint.Dummy).Should().BeTrue());
            validator.Validate("SimpleAssignment: c = b = a", x => x.State[((ISimpleAssignmentOperation)x.Operation.Instance).Target].HasConstraint(DummyConstraint.Dummy).Should().BeTrue());
            validator.ValidateTag("End", "c", x => x.HasConstraint(DummyConstraint.Dummy).Should().BeTrue());
        }

        [TestMethod]
        public void SimpleAssignment_ToParameter_FromLiteral()
        {
            var validator = SETestContext.CreateCS(@"boolParameter = true; var tag = ""End"";", new LiteralDummyTestCheck()).Validator;
            validator.Validate("Literal: true", x => x.State[x.Operation].HasConstraint(DummyConstraint.Dummy).Should().BeTrue("it's scaffolded"));
            validator.Validate("SimpleAssignment: boolParameter = true", x => x.State[x.Operation].HasConstraint(DummyConstraint.Dummy).Should().BeTrue());
            validator.Validate("SimpleAssignment: boolParameter = true", x => x.State[((ISimpleAssignmentOperation)x.Operation.Instance).Target].HasConstraint(DummyConstraint.Dummy).Should().BeTrue());
            validator.ValidateTag("End", "boolParameter", x => x.HasConstraint(DummyConstraint.Dummy).Should().BeTrue());
        }

        [TestMethod]
        public void SimpleAssignment_ToLocalVariable_FromTrackedSymbol_CS()
        {
            var setter = new PreProcessTestCheck(OperationKind.ParameterReference, x => x.SetOperationConstraint(DummyConstraint.Dummy));
            var validator = SETestContext.CreateCS(@"var b = boolParameter; var tag = ""End"";", setter).Validator;
            validator.ValidateTag("End", "b", x => x.HasConstraint(DummyConstraint.Dummy).Should().BeTrue());
        }

        [TestMethod]
        public void SimpleAssignment_ToLocalVariable_FromTrackedSymbol_VB()
        {
            var setter = new PreProcessTestCheck(OperationKind.ParameterReference, x => x.SetOperationConstraint(DummyConstraint.Dummy));
            var validator = SETestContext.CreateVB(@"Dim B As Boolean = BoolParameter : Dim Tag As String = ""End""", setter).Validator;
            validator.ValidateTag("End", "B", x => x.HasConstraint(DummyConstraint.Dummy).Should().BeTrue());
        }

        [DataTestMethod]
        [DataRow(@"Sample.StaticField = 42; var tag = ""End"";", "StaticField", "SimpleAssignment: Sample.StaticField = 42")]
        [DataRow(@"StaticField = 42; var tag = ""End"";", "StaticField", "SimpleAssignment: StaticField = 42")]
        [DataRow(@"field = 42; var tag = ""End"";", "field", "SimpleAssignment: field = 42")]
        [DataRow(@"this.field = 42; var tag = ""End"";", "field", "SimpleAssignment: this.field = 42")]
        [DataRow(@"field = 42; var a = field; var tag = ""End"";", "field", "SimpleAssignment: a = field (Implicit)")]
        public void SimpleAssignment_Fields(string snippet, string symbolName, string operation)
        {
            var validator = SETestContext.CreateCS(snippet, new LiteralDummyTestCheck()).Validator;
            validator.Validate("Literal: 42", x => x.State[x.Operation].HasConstraint(DummyConstraint.Dummy).Should().BeTrue("it's scaffolded"));
            validator.Validate(operation, x => x.State[x.Operation].HasConstraint(DummyConstraint.Dummy).Should().BeTrue());
            validator.ValidateTag("End", symbolName, x => x.HasConstraint(DummyConstraint.Dummy).Should().BeTrue());
        }

        [DataTestMethod]
        [DataRow(@"Sample.StaticProperty = 42; Tag(""Target"", Sample.StaticProperty);")]
        [DataRow(@"StaticProperty = 42; Tag(""Target"", StaticProperty);")]
        [DataRow(@"var arr = new byte[] { 13 }; arr[0] = 42; Tag(""Target"", arr[0]);")]
        [DataRow(@"var dict = new Dictionary<string, int>(); dict[""key""] = 42; Tag(""Target"", dict[""key""]);")]
        [DataRow(@"var other = new Sample(); other.Property = 42; Tag(""Target"", other.Property);")]
        [DataRow(@"this.Property = 42; Tag(""Target"", this.Property);")]
        [DataRow(@"Property = 42; Tag(""Target"", Property);")]
        [DataRow(@"var other = new Sample(); other.field = 42; Tag(""Target"", other.field);")]
        public void SimpleAssignment_ToUnsupported_FromLiteral(string snippet)
        {
            var validator = SETestContext.CreateCS(snippet, new LiteralDummyTestCheck()).Validator;
            validator.Validate("Literal: 42", x => x.State[x.Operation].HasConstraint(DummyConstraint.Dummy).Should().BeTrue("it's scaffolded"));
            validator.ExitStates.Should().ContainSingle().Which.Should().Be(ProgramState.Empty);
        }

        [TestMethod]
        public void Conversion_ToLocalVariable_FromTrackedSymbol_ExplicitCast()
        {
            var validator = SETestContext.CreateCS(@"int a = 42; byte b = (byte)a; var c = (byte)field; var tag = ""End"";", new LiteralDummyTestCheck()).Validator;
            validator.ValidateOrder(
                "LocalReference: a = 42 (Implicit)",
                "Literal: 42",
                "SimpleAssignment: a = 42 (Implicit)",
                "LocalReference: b = (byte)a (Implicit)",
                "LocalReference: a",
                "Conversion: (byte)a",
                "SimpleAssignment: b = (byte)a (Implicit)",
                "LocalReference: c = (byte)field (Implicit)",
                "InstanceReference: field (Implicit)",
                "FieldReference: field",
                "Conversion: (byte)field",
                "SimpleAssignment: c = (byte)field (Implicit)",
                @"LocalReference: tag = ""End"" (Implicit)",
                @"Literal: ""End""",
                @"SimpleAssignment: tag = ""End"" (Implicit)");
            validator.ValidateTag("End", "b", x => x.HasConstraint(DummyConstraint.Dummy).Should().BeTrue());
            validator.ValidateTag("End", "c", x => x.Should().BeNull());
        }

        [TestMethod]
        public void Conversion_ToLocalVariable_FromLiteral_ImplicitCast()
        {
            var validator = SETestContext.CreateCS(@"byte b = 42; var tag = ""End"";", new LiteralDummyTestCheck()).Validator;
            validator.ValidateOrder(
                "LocalReference: b = 42 (Implicit)",
                "Literal: 42",
                "Conversion: 42 (Implicit)",
                "SimpleAssignment: b = 42 (Implicit)",
                @"LocalReference: tag = ""End"" (Implicit)",
                @"Literal: ""End""",
                @"SimpleAssignment: tag = ""End"" (Implicit)");
                validator.ValidateTag("End", "b", x => x.HasConstraint(DummyConstraint.Dummy).Should().BeTrue());
        }

        [TestMethod]
        public void Argument_Ref_ResetsConstraints_CS() =>
            SETestContext.CreateCS(@"var b = true; Main(boolParameter, ref b); var tag = ""End"";", ", ref bool outParam").Validator
                .ValidateTag("End", "b", x => x.Should().BeNull());

        [TestMethod]
        public void Argument_Out_ResetsConstraints_CS() =>
            SETestContext.CreateCS(@"var b = true; Main(boolParameter, out b); var tag = ""End""; outParam = false;", ", out bool outParam").Validator
                .ValidateTag("End", "b", x => x.Should().BeNull());

        [TestMethod]
        public void Argument_ByRef_ResetConstraints_VB() =>
            SETestContext.CreateVB(@"Dim B As Boolean = True : Main(BoolParameter, B) : Dim Tag As String = ""End""", ", ByRef ByRefParam As Boolean").Validator
                .ValidateTag("End", "B", x => x.Should().BeNull());

        [TestMethod]
        public void Binary_BoolOperands_Equals_CS()
        {
            const string code = @"
var isTrue = true;
var isFalse = false;

if (isTrue == true)
    Tag(""True"");
else
    Tag(""True Unreachable"");

if (isFalse == false)
    Tag(""False"");
else
    Tag(""False Unreachable"");

if (isTrue == isFalse)
    Tag(""Variables Unreachable"");
else
    Tag(""Variables"");";
            SETestContext.CreateCS(code).Validator.ValidateTagOrder("True", "False", "Variables");
        }

        [TestMethod]
        public void Binary_BoolOperands_Equals_VB()
        {
            const string code = @"
Dim IsTrue = True
Dim IsFalse = False

If IsTrue = True Then
    Tag(""True"")
Else
    Tag(""True Unreachable"")
End If

If IsFalse = False Then
    Tag(""False"")
Else
    Tag(""False Unreachable"")
End If

If IsTrue = IsFalse Then
    Tag(""Variables Unreachable"")
Else
    Tag(""Variables"")
End If";
            SETestContext.CreateVB(code).Validator.ValidateTagOrder("True", "False", "Variables");
        }

        [TestMethod]
        public void Binary_BoolOperands_NotEquals_CS()
        {
            const string code = @"
var isTrue = true;
var isFalse = false;

if (isTrue != true)
    Tag(""True Unreachable"");
else
    Tag(""True"");

if (isFalse != false)
    Tag(""False Unreachable"");
else
    Tag(""False"");

if (isTrue != isFalse)
    Tag(""Variables"");
else
    Tag(""Variables Unreachable"");";
            SETestContext.CreateCS(code).Validator.ValidateTagOrder("True", "False", "Variables");
        }

        [TestMethod]
        public void Binary_BoolOperands_NotEquals_VB()
        {
            const string code = @"
Dim IsTrue = True
Dim IsFalse = False

If IsTrue <> True Then
    Tag(""True Unreachable"")
Else
    Tag(""True"")
End If

If IsFalse <> False Then
    Tag(""False Unreachable"")
Else
    Tag(""False"")
End If

If IsTrue <> IsFalse Then
    Tag(""Variables"")
Else
    Tag(""Variables Unreachable"")
End If";
            SETestContext.CreateVB(code).Validator.ValidateTagOrder(
                "True",
                "False",
                "Variables");
        }

        [TestMethod]
        public void Binary_BoolOperands_And()
        {
            const string code = @"
var isTrue = true;
var isFalse = false;

if (isTrue & true)
    Tag(""True & True"");
else
    Tag(""True & True Unreachable"");

if (false & isTrue)
    Tag(""False & True Unreachable"");
else
    Tag(""False & True"");

if (false & isFalse)
    Tag(""False & False Unreachable"");
else
    Tag(""False & False"");

if (isTrue && true)
    Tag(""True && True"");
else
    Tag(""True && True Unreachable"");

if (isFalse && true)
    Tag(""False && True Unreachable"");
else
    Tag(""False && True"");";
            SETestContext.CreateCS(code).Validator.ValidateTagOrder("True & True", "False & True", "False & False", "True && True", "False && True");
        }

        [TestMethod]
        public void Binary_BoolOperands_Or()
        {
            const string code = @"
var isTrue = true;
var isFalse = false;

if (isTrue | true)
    Tag(""True | True"");
else
    Tag(""True | True Unreachable"");

if (false | isTrue)
    Tag(""False | True"");
else
    Tag(""False | True Unreachable"");

if (false | isFalse)
    Tag(""False | False Unreachable"");
else
    Tag(""False | False"");

if (isTrue || true)
    Tag(""True || True"");
else
    Tag(""True || True Unreachable"");

if (isFalse || true)
    Tag(""False || True"");
else
    Tag(""False || True Unreachable"");";
            SETestContext.CreateCS(code).Validator.ValidateTagOrder("True | True", "False | True", "False | False", "True || True", "False || True");
        }

        [TestMethod]
        public void Binary_BoolOperands_Xor()
        {
            const string code = @"
var isTrue = true;
var isFalse = false;

if (isTrue ^ true)
    Tag(""True ^ True Unreachable"");
else
    Tag(""True ^ True"");

if (false ^ isTrue)
    Tag(""False ^ True"");
else
    Tag(""False ^ True Unreachable"");

if (isTrue ^ false)
    Tag(""True ^ False"");
else
    Tag(""True ^ False Unreachable"");

if (false ^ isFalse)
    Tag(""False ^ False Unreachable"");
else
    Tag(""False ^ False"");";
            SETestContext.CreateCS(code).Validator.ValidateTagOrder("True ^ True", "False ^ True", "True ^ False", "False ^ False");
        }

        [DataTestMethod]
        [DataRow("boolParameter & isTrue")]
        [DataRow("isTrue & boolParameter")]
        public void Binary_NoConstraint_VisitsBothBranches(string condition)
        {
            var code = $@"
bool isTrue = true;
if ({condition})
{{
    Tag(""If"");
}}
else
{{
    Tag(""Else"");
}}
Tag(""End"");";
            SETestContext.CreateCS(code).Validator.ValidateTagOrder(
                "If",
                "Else",
                "End");
        }

        [DataTestMethod]
        [DataRow("boolParameter & isTrue")]
        [DataRow("isTrue & boolParameter")]
        public void Binary_OtherConstraint_VisitsBothBranches(string condition)
        {
            var code = $@"
bool isTrue = true;
if ({condition})
{{
    Tag(""If"");
}}
else
{{
    Tag(""Else"");
}}
Tag(""End"");";
            var check = new PostProcessTestCheck(OperationKind.ParameterReference, x => x.SetOperationConstraint(DummyConstraint.Dummy));
            SETestContext.CreateCS(code, check).Validator.ValidateTagOrder(
                "If",
                "Else",
                "End");
        }

        [TestMethod]
        public void Binary_UnexpectedOperator_VisitsBothBranches()
        {
            var code = $@"
if (a > b)      // Both, 'a' and 'b' have bool constraint (weird) and we do not produce bool constraint for '>' binary operator, because it doesn't make sense.
{{
    Tag(""If"");
}}
else
{{
    Tag(""Else"");
}}
Tag(""End"");";
            var check = new PostProcessTestCheck(OperationKind.ParameterReference, x => x.SetOperationConstraint(BoolConstraint.True));
            SETestContext.CreateCS(code, ", int a, int b", check).Validator.ValidateTagOrder(
                "If",
                "Else",
                "End");
        }

        [TestMethod]
        public void FlowCapture_SetsCapture()
        {
            var assertions = 0;
            var collector = new PostProcessTestCheck(x =>
            {
                if (x.Operation.Instance.Kind == OperationKind.FlowCaptureReference)
                {
                    var capture = IFlowCaptureReferenceOperationWrapper.FromOperation(x.Operation.Instance);
                    x.State.ResolveCapture(capture.WrappedOperation).Kind.Should().Be(OperationKind.LocalReference);
                    assertions++;
                }
                return x.State;
            });
            SETestContext.CreateCS("string a = null; a ??= arg;", ", string arg", collector);
            assertions.Should().Be(3);  // Block #3 transitive capture, Block #3 BranchValue, Block #4
        }
    }
}
