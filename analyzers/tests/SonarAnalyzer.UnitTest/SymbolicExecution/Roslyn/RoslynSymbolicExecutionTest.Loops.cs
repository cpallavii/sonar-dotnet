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
        [DataTestMethod]
        [DataRow("for (var i = 0; i < items.Length; i++)")]
        [DataRow("while (value > 0)")]
        [DataRow("while (Condition)")]
        public void Loops_InstructionVisitedMaxTwice(string loop)
        {
            var code = $@"
var value = 42;
{loop}
{{
    value.ToString(); // Add another constraint to 'value'
    value--;
}}
var tag = ""End"";";
            var validator = SETestContext.CreateCS(code, ", int[] items", new AddConstraintOnInvocationCheck(), new PreserveTestCheck("value")).Validator;
            validator.ValidateExitReachCount(2);
            validator.TagValues("End", "value").Should().HaveCount(2)
                .And.ContainSingle(x => x == null)
                .And.ContainSingle(x => x != null && x.HasConstraint(TestConstraint.First) && !x.HasConstraint(BoolConstraint.True));
        }

        [TestMethod]
        public void Loops_InstructionVisitedMaxTwice_ForEach()
        {
            const string code = @"
var value = 42;
foreach (var i in items)
{{
    value.ToString(); // Add another constraint to 'value'
    value--;
}}
var tag = ""End"";";
            var validator = SETestContext.CreateCS(code, ", int[] items", new AddConstraintOnInvocationCheck(), new PreserveTestCheck("value")).Validator;
            // In the case of foreach, there are implicit method calls that in the current implementation can throw:
            // - IEnumerable<>.GetEnumerator()
            // - System.Collections.IEnumerator.MoveNext()
            // - System.IDisposable.Dispose()
            validator.ValidateExitReachCount(6);                // foreach produces implicit TryFinally region where it can throw and changes the flow
            validator.TagValues("End", "value").Should().HaveCount(2)    // These Exception flows do not reach the Tag("End") line
                .And.ContainSingle(x => x == null)
                .And.ContainSingle(x => x != null && x.HasConstraint(TestConstraint.First) && !x.HasConstraint(BoolConstraint.True));
        }

        [TestMethod]
        public void Loops_InstructionVisitedMaxTwice_EvenWithMultipleStates()
        {
            const string code = @"
var value = 42;
bool condition;
if (Condition)      // This generates two different ProgramStates, each tracks its own visits
    condition = true;
else
    condition = false;
do
{
    value.ToString(); // Add another constraint to 'value'
} while (Condition);
Tag(""End"", value);";
            var validator = SETestContext.CreateCS(code, ", int[] items", new AddConstraintOnInvocationCheck(), new PreserveTestCheck("condition"), new PreserveTestCheck("value")).Validator;
            validator.ValidateExitReachCount(4);
            var states = validator.TagStates("End");
            var condition = states.SelectMany(x => x.SymbolsWith(BoolConstraint.False)).First();    // "False" is never set for "value"
            var value = states.SelectMany(x => x.SymbolsWith(TestConstraint.First)).First();        // "First" is never set for "condition"
            states.Should().HaveCount(4)
                .And.ContainSingle(x => x[condition].HasConstraint(BoolConstraint.True) && x[value].HasConstraint(TestConstraint.First) && !x[value].HasConstraint(BoolConstraint.True))
                .And.ContainSingle(x => x[condition].HasConstraint(BoolConstraint.True) && x[value].HasConstraint(TestConstraint.First) && x[value].HasConstraint(BoolConstraint.True))
                .And.ContainSingle(x => x[condition].HasConstraint(BoolConstraint.False) && x[value].HasConstraint(TestConstraint.First) && !x[value].HasConstraint(BoolConstraint.True))
                .And.ContainSingle(x => x[condition].HasConstraint(BoolConstraint.False) && x[value].HasConstraint(TestConstraint.First) && x[value].HasConstraint(BoolConstraint.True));
        }

        [TestMethod]
        public void DoLoop_InstructionVisitedMaxTwice()
        {
            var code = $@"
var value = 42;
do
{{
    value.ToString(); // Add another constraint to 'value'
    value--;
}} while (value > 0);
var tag = ""End"";";
            var validator = SETestContext.CreateCS(code, new AddConstraintOnInvocationCheck()).Validator;
            validator.ValidateExitReachCount(1);
            validator.TagValues("End", "value").Should().HaveCount(2)
                .And.ContainSingle(x => x.HasConstraint(TestConstraint.First) && !x.HasConstraint(BoolConstraint.True))
                .And.ContainSingle(x => x.HasConstraint(TestConstraint.First) && x.HasConstraint(BoolConstraint.True) && !x.HasConstraint(DummyConstraint.Dummy));
        }

        [DataTestMethod]
        [DataRow("for( ; ; )")]
        [DataRow("while (true)")]
        public void InfiniteLoopWithNoExitBranch_InstructionVisitedMaxTwice(string loop)
        {
            var code = @$"
var value = 42;
{loop}
{{
    value.ToString(); // Add another constraint to 'value'
    var tag = ""Inside"";
}}";
            var validator = SETestContext.CreateCS(code, new AddConstraintOnInvocationCheck()).Validator;
            validator.ValidateExitReachCount(0);                    // There's no branch to 'Exit' block, or the exist is never reached
            validator.TagValues("Inside", "value").Should().HaveCount(2)
                .And.ContainSingle(x => x.HasConstraint(TestConstraint.First) && !x.HasConstraint(BoolConstraint.True))
                .And.ContainSingle(x => x.HasConstraint(TestConstraint.First) && x.HasConstraint(BoolConstraint.True) && !x.HasConstraint(DummyConstraint.Dummy));
        }

        [TestMethod]
        public void GoTo_InfiniteWithNoExitBranch_InstructionVisitedMaxTwice()
        {
            const string code = @"
var value = 42;
Start:
value.ToString(); // Add another constraint to 'value'
var tag = ""Inside"";
goto Start;";
            var validator = SETestContext.CreateCS(code, new AddConstraintOnInvocationCheck()).Validator;
            validator.ValidateExitReachCount(0);                    // There's no branch to 'Exit' block
            validator.TagValues("Inside", "value").Should().HaveCount(2)
                .And.ContainSingle(x => x.HasConstraint(TestConstraint.First) && !x.HasConstraint(BoolConstraint.True))
                .And.ContainSingle(x => x.HasConstraint(TestConstraint.First) && x.HasConstraint(BoolConstraint.True) && !x.HasConstraint(DummyConstraint.Dummy));
        }

        [TestMethod]
        public void GoTo_InstructionVisitedMaxTwice()
        {
            const string code = @"
var value = 42;
Start:
value.ToString(); // Add another constraint to 'value'
value--;
if (value > 0)
{
    goto Start;
}
var tag = ""End"";";
            var validator = SETestContext.CreateCS(code, new AddConstraintOnInvocationCheck()).Validator;
            validator.ValidateExitReachCount(1);
            validator.TagValues("End", "value").Should().HaveCount(2)
                .And.ContainSingle(x => x.HasConstraint(TestConstraint.First) && !x.HasConstraint(BoolConstraint.True))
                .And.ContainSingle(x => x.HasConstraint(TestConstraint.First) && x.HasConstraint(BoolConstraint.True) && !x.HasConstraint(DummyConstraint.Dummy));
        }

        [DataTestMethod]
        [DataRow("for (var i = 0; condition; i++)")]
        [DataRow("while (condition)")]
        public void Loops_FalseConditionNotExecuted(string loop)
        {
            var code = $@"
var condition = false;
{loop}
{{
    Tag(""Loop"");
}}
Tag(""End"");";
            SETestContext.CreateCS(code).Validator.ValidateTagOrder("End");
        }
    }
}
