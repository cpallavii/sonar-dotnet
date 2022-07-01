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
using SonarAnalyzer.CFG.Roslyn;
using SonarAnalyzer.Extensions;
using SonarAnalyzer.SymbolicExecution.Roslyn;
using SonarAnalyzer.UnitTest.Helpers;
using StyleCop.Analyzers.Lightup;

namespace SonarAnalyzer.UnitTest.TestFramework.SymbolicExecution
{
    /// <summary>
    /// This checks looks for specific tags in the source and collects them:
    /// tag = "TagName" - registers TagName, doesn't change the flow
    /// Tag("TagName") - can change flow, because invocations can throw exceptions in the engine
    /// Tag("TagName", variable) - can change flow, enables asserting on variable state
    /// </summary>
    internal class ValidatorTestCheck : SymbolicCheck
    {
        private readonly ControlFlowGraph cfg;
        private readonly List<SymbolicContext> postProcessed = new();
        private readonly List<(string Name, SymbolicContext Context)> tags = new();
        private readonly Dictionary<string, HashSet<ISymbol>> symbols = new();
        private int executionCompletedCount;

        public List<ProgramState> ExitStates { get; } = new();

        public ValidatorTestCheck(ControlFlowGraph cfg) =>
            this.cfg = cfg;

        public override void ExitReached(SymbolicContext context) =>
            ExitStates.Add(context.State);

        public override void ExecutionCompleted() =>
            executionCompletedCount++;

        public void ValidateOrder(params string[] expected) =>
            postProcessed.Select(x => TestHelper.Serialize(x.Operation)).Should().OnlyContainInOrder(expected);

        public void ValidateTagOrder(params string[] expected) =>
            tags.Select(x => x.Name).Should().OnlyContainInOrder(expected);

        public void Validate(string operation, Action<SymbolicContext> action) =>
            action(postProcessed.Single(x => TestHelper.Serialize(x.Operation) == operation));

        public void ValidateTag(string tag, string symbolName, Action<SymbolicValue> action) =>
            action(TagValues(tag, symbolName).Single());

        public ProgramState[] TagStates(string tag) =>
            tags.Where(x => x.Name == tag).Select(x => x.Context.State).ToArray();

        public SymbolicValue[] TagValues(string tag, string symbolName) =>
            tags.Where(x => x.Name == tag).Select(x => TagValue(x.Context, symbolName)).ToArray();

        public void ValidateExitReachCount(int expected) =>
            ExitStates.Should().HaveCount(expected);

        public void ValidateExecutionCompleted() =>
            executionCompletedCount.Should().Be(1);

        public void ValidateExecutionNotCompleted() =>
            executionCompletedCount.Should().Be(0);

        public void ValidatePostProcessCount(int expected) =>
            postProcessed.Should().HaveCount(expected);

        public void ValidateOperationValuesAreNull() =>
            postProcessed.Should().OnlyContain(x => x.State[x.Operation] == null);

        public void ValidateContainsOperation(OperationKind operationKind) =>
            cfg.Blocks.Any(x => x.OperationsAndBranchValue.ToExecutionOrder().Any(op => op.Instance.Kind == operationKind));

        protected override ProgramState PostProcessSimple(SymbolicContext context)
        {
            postProcessed.Add(context);
            if (context.Operation.Instance is IAssignmentOperation assignment && assignment.Target.TrackedSymbol() is { Name: "tag" or "Tag" })
            {
                AddTagName(assignment.Value.ConstantValue, context);
            }
            else if (context.Operation.Instance is IInvocationOperation invocation && invocation.TargetMethod.Name == "Tag")
            {
                AddTagName(invocation.Arguments.First().Value.ConstantValue, context);
            }
            else if (context.Operation.Instance.TrackedSymbol() is { } symbol)
            {
                symbols.GetOrAdd(symbol.Name, x => new(SymbolEqualityComparer.Default)).Add(symbol);
            }
            return context.State;
        }

        private void AddTagName(Optional<object> tagName, SymbolicContext context)
        {
            tagName.HasValue.Should().BeTrue("tag should have literal name");
            tags.Add(((string)tagName.Value, context));
        }

        private SymbolicValue TagValue(SymbolicContext context, string symbolName)
        {
            symbols.TryGetValue(symbolName, out var capturedSymbols).Should().BeTrue($"Requested symbol '{symbolName}' should exist in the code snippet");
            capturedSymbols.Should().HaveCount(1, $"there should be exactly one symbol with name '{symbolName}' in the code snippet");
            return context.State[capturedSymbols.Single()];
        }
    }
}
