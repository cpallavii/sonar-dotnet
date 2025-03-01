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

using SonarAnalyzer.CFG;
using SonarAnalyzer.UnitTest.Helpers;

namespace SonarAnalyzer.UnitTest.CFG.Sonar
{
    [TestClass]
    public class RoslynCfgSerializerTest
    {
        [TestMethod]
        public void Serialize_MethodNameUsedInTitle()
        {
            var code = @"
class Sample
{
    void Method() { }
}";
            var dot = CfgSerializer.Serialize(TestHelper.CompileCfgCS(code), "GraphTitle");
            dot.Should().BeIgnoringLineEndings(
@"digraph ""GraphTitle"" {
cfg0_block0 [shape=record label=""{ENTRY #0}""]
cfg0_block1 [shape=record label=""{EXIT #1}""]
cfg0_block0 -> cfg0_block1
}
");
        }

        [TestMethod]
        public void Serialize_EmptyMethod()
        {
            var code = @"
class Sample
{
    void Method() { }
}";
            var dot = CfgSerializer.Serialize(TestHelper.CompileCfgCS(code));

            dot.Should().BeIgnoringLineEndings(
@"digraph ""RoslynCfg"" {
cfg0_block0 [shape=record label=""{ENTRY #0}""]
cfg0_block1 [shape=record label=""{EXIT #1}""]
cfg0_block0 -> cfg0_block1
}
");
        }

        [TestMethod]
        public void Serialize_OperationSequence()
        {
            var code = @"
class Sample
{
    void Method()
    {
        A();
        B();
        var c = C();
    }

    private void A() { }
    private void B() { }
    private int C() => 42;
}";
            var dot = CfgSerializer.Serialize(TestHelper.CompileCfgCS(code));
            dot.Should().BeIgnoringLineEndings(
@"digraph ""RoslynCfg"" {
subgraph ""cluster_1"" {
label = ""LocalLifetime region, Locals: c""
cfg0_block1 [shape=record label=""{BLOCK #1|0# ExpressionStatementOperation / ExpressionStatementSyntax: A();|1# InvocationOperation: A / InvocationExpressionSyntax: A()|2# InstanceReferenceOperation / IdentifierNameSyntax: A|##########|0# ExpressionStatementOperation / ExpressionStatementSyntax: B();|1# InvocationOperation: B / InvocationExpressionSyntax: B()|2# InstanceReferenceOperation / IdentifierNameSyntax: B|##########|0# SimpleAssignmentOperation / VariableDeclaratorSyntax: c = C()|1# LocalReferenceOperation / VariableDeclaratorSyntax: c = C()|1# InvocationOperation: C / InvocationExpressionSyntax: C()|2# InstanceReferenceOperation / IdentifierNameSyntax: C|##########}""]
}
cfg0_block0 [shape=record label=""{ENTRY #0}""]
cfg0_block2 [shape=record label=""{EXIT #2}""]
cfg0_block0 -> cfg0_block1
cfg0_block1 -> cfg0_block2
}
");
        }

        [TestMethod]
        public void Serialize_Switch()
        {
            var code = @"
class Sample
{
    void Foo(int a)
    {
        switch (a)
        {
            case 1:
                c1();
                break;

            case 2:
                c2();
                break;
        }
    }
    private void c1() { }
    private void c2() { }
}
";
            var dot = CfgSerializer.Serialize(TestHelper.CompileCfgCS(code));
            dot.Should().BeIgnoringLineEndings(
@"digraph ""RoslynCfg"" {
subgraph ""cluster_1"" {
label = ""LocalLifetime region, Captures: #0""
cfg0_block1 [shape=record label=""{BLOCK #1|0# FlowCaptureOperation: #0 / IdentifierNameSyntax: a|1# ParameterReferenceOperation / IdentifierNameSyntax: a|##########|## BranchValue ##|0# BinaryOperation / LiteralExpressionSyntax: 1|1# FlowCaptureReferenceOperation: #0 / IdentifierNameSyntax: a|1# LiteralOperation / LiteralExpressionSyntax: 1|##########}""]
cfg0_block2 [shape=record label=""{BLOCK #2|0# ExpressionStatementOperation / ExpressionStatementSyntax: c1();|1# InvocationOperation: c1 / InvocationExpressionSyntax: c1()|2# InstanceReferenceOperation / IdentifierNameSyntax: c1|##########}""]
cfg0_block3 [shape=record label=""{BLOCK #3|## BranchValue ##|0# BinaryOperation / LiteralExpressionSyntax: 2|1# FlowCaptureReferenceOperation: #0 / IdentifierNameSyntax: a|1# LiteralOperation / LiteralExpressionSyntax: 2|##########}""]
cfg0_block4 [shape=record label=""{BLOCK #4|0# ExpressionStatementOperation / ExpressionStatementSyntax: c2();|1# InvocationOperation: c2 / InvocationExpressionSyntax: c2()|2# InstanceReferenceOperation / IdentifierNameSyntax: c2|##########}""]
}
cfg0_block0 [shape=record label=""{ENTRY #0}""]
cfg0_block5 [shape=record label=""{EXIT #5}""]
cfg0_block0 -> cfg0_block1
cfg0_block1 -> cfg0_block2 [label=""Else""]
cfg0_block1 -> cfg0_block3 [label=""WhenFalse""]
cfg0_block3 -> cfg0_block4 [label=""Else""]
cfg0_block2 -> cfg0_block5
cfg0_block3 -> cfg0_block5 [label=""WhenFalse""]
cfg0_block4 -> cfg0_block5
}
");
        }

        [TestMethod]
        public void Serialize_If()
        {
            var code = @"
class Sample
{
    void Method()
    {
        if (true)
        {
            Bar();
        }
    }
    void Bar() { }
}
";
            var dot = CfgSerializer.Serialize(TestHelper.CompileCfgCS(code));

            dot.Should().BeIgnoringLineEndings(
@"digraph ""RoslynCfg"" {
cfg0_block0 [shape=record label=""{ENTRY #0}""]
cfg0_block1 [shape=record label=""{BLOCK #1|## BranchValue ##|0# LiteralOperation / LiteralExpressionSyntax: true|##########}""]
cfg0_block2 [shape=record label=""{BLOCK #2|0# ExpressionStatementOperation / ExpressionStatementSyntax: Bar();|1# InvocationOperation: Bar / InvocationExpressionSyntax: Bar()|2# InstanceReferenceOperation / IdentifierNameSyntax: Bar|##########}""]
cfg0_block3 [shape=record label=""{EXIT #3}""]
cfg0_block0 -> cfg0_block1
cfg0_block1 -> cfg0_block2 [label=""Else""]
cfg0_block1 -> cfg0_block3 [label=""WhenFalse""]
cfg0_block2 -> cfg0_block3
}
");
        }

        [TestMethod]
        public void Serialize_Foreach_Simple()
        {
            var code = @"
class Sample
{
    void Method(int[] items)
    {
        foreach (var i in items)
        {
            Bar(i);
        }
    }
    private void Bar(int i) { }
}";
            var dot = CfgSerializer.Serialize(TestHelper.CompileCfgCS(code));
            dot.Should().BeIgnoringLineEndings(
@"digraph ""RoslynCfg"" {
subgraph ""cluster_1"" {
label = ""LocalLifetime region, Captures: #0""
subgraph ""cluster_2"" {
label = ""TryAndFinally region""
subgraph ""cluster_3"" {
label = ""Try region""
subgraph ""cluster_4"" {
label = ""LocalLifetime region, Locals: i""
cfg0_block3 [shape=record label=""{BLOCK #3|0# SimpleAssignmentOperation / IdentifierNameSyntax: var|1# LocalReferenceOperation / IdentifierNameSyntax: var|1# ConversionOperation / IdentifierNameSyntax: var|2# PropertyReferenceOperation / IdentifierNameSyntax: var|3# FlowCaptureReferenceOperation: #0 / IdentifierNameSyntax: items|##########|0# ExpressionStatementOperation / ExpressionStatementSyntax: Bar(i);|1# InvocationOperation: Bar / InvocationExpressionSyntax: Bar(i)|2# InstanceReferenceOperation / IdentifierNameSyntax: Bar|2# ArgumentOperation / ArgumentSyntax: i|3# LocalReferenceOperation / IdentifierNameSyntax: i|##########}""]
}
cfg0_block2 [shape=record label=""{BLOCK #2|## BranchValue ##|0# InvocationOperation: MoveNext / IdentifierNameSyntax: items|1# FlowCaptureReferenceOperation: #0 / IdentifierNameSyntax: items|##########}""]
}
subgraph ""cluster_5"" {
label = ""Finally region, Captures: #1""
cfg0_block4 [shape=record label=""{BLOCK #4|0# FlowCaptureOperation: #1 / IdentifierNameSyntax: items|1# ConversionOperation / IdentifierNameSyntax: items|2# FlowCaptureReferenceOperation: #0 / IdentifierNameSyntax: items|##########|## BranchValue ##|0# IsNullOperation / IdentifierNameSyntax: items|1# FlowCaptureReferenceOperation: #1 / IdentifierNameSyntax: items|##########}""]
cfg0_block5 [shape=record label=""{BLOCK #5|0# InvocationOperation: Dispose / IdentifierNameSyntax: items|1# FlowCaptureReferenceOperation: #1 / IdentifierNameSyntax: items|##########}""]
cfg0_block6 [shape=record label=""{BLOCK #6}""]
}
}
cfg0_block1 [shape=record label=""{BLOCK #1|0# FlowCaptureOperation: #0 / IdentifierNameSyntax: items|1# InvocationOperation: GetEnumerator / IdentifierNameSyntax: items|2# ConversionOperation / IdentifierNameSyntax: items|3# ParameterReferenceOperation / IdentifierNameSyntax: items|##########}""]
}
cfg0_block0 [shape=record label=""{ENTRY #0}""]
cfg0_block7 [shape=record label=""{EXIT #7}""]
cfg0_block2 -> cfg0_block3 [label=""Else""]
cfg0_block1 -> cfg0_block2
cfg0_block3 -> cfg0_block2
cfg0_block4 -> cfg0_block5 [label=""Else""]
cfg0_block4 -> cfg0_block6 [label=""WhenTrue""]
cfg0_block5 -> cfg0_block6
cfg0_block6 -> NoDestination_cfg0_block6 [label=""StructuredExceptionHandling""]
cfg0_block0 -> cfg0_block1
cfg0_block2 -> cfg0_block7 [label=""WhenFalse""]
}
");
        }

        [TestMethod]
        public void Serialize_Foreach_TupleVarDeclaration()
        {
            var code = @"
public class Sample
{
    public void Method((string key, string value)[] values)
    {
        foreach (var (key, value) in values)
        {
            string i = key;
            string j = value;
        }
    }
}";
            var dot = CfgSerializer.Serialize(TestHelper.CompileCfgCS(code));
            dot.Should().BeIgnoringLineEndings(
@"digraph ""RoslynCfg"" {
subgraph ""cluster_1"" {
label = ""LocalLifetime region, Captures: #0""
subgraph ""cluster_2"" {
label = ""TryAndFinally region""
subgraph ""cluster_3"" {
label = ""Try region""
subgraph ""cluster_4"" {
label = ""LocalLifetime region, Locals: key, value""
subgraph ""cluster_5"" {
label = ""LocalLifetime region, Locals: i, j""
cfg0_block4 [shape=record label=""{BLOCK #4|0# SimpleAssignmentOperation / VariableDeclaratorSyntax: i = key|1# LocalReferenceOperation / VariableDeclaratorSyntax: i = key|1# LocalReferenceOperation / IdentifierNameSyntax: key|##########|0# SimpleAssignmentOperation / VariableDeclaratorSyntax: j = value|1# LocalReferenceOperation / VariableDeclaratorSyntax: j = value|1# LocalReferenceOperation / IdentifierNameSyntax: value|##########}""]
}
cfg0_block3 [shape=record label=""{BLOCK #3|0# DeconstructionAssignmentOperation / DeclarationExpressionSyntax: var (key, value)|1# DeclarationExpressionOperation / DeclarationExpressionSyntax: var (key, value)|2# TupleOperation / ParenthesizedVariableDesignationSyntax: (key, value)|3# LocalReferenceOperation / SingleVariableDesignationSyntax: key|3# LocalReferenceOperation / SingleVariableDesignationSyntax: value|1# ConversionOperation / DeclarationExpressionSyntax: var (key, value)|2# PropertyReferenceOperation / DeclarationExpressionSyntax: var (key, value)|3# FlowCaptureReferenceOperation: #0 / IdentifierNameSyntax: values|##########}""]
}
cfg0_block2 [shape=record label=""{BLOCK #2|## BranchValue ##|0# InvocationOperation: MoveNext / IdentifierNameSyntax: values|1# FlowCaptureReferenceOperation: #0 / IdentifierNameSyntax: values|##########}""]
}
subgraph ""cluster_6"" {
label = ""Finally region, Captures: #1""
cfg0_block5 [shape=record label=""{BLOCK #5|0# FlowCaptureOperation: #1 / IdentifierNameSyntax: values|1# ConversionOperation / IdentifierNameSyntax: values|2# FlowCaptureReferenceOperation: #0 / IdentifierNameSyntax: values|##########|## BranchValue ##|0# IsNullOperation / IdentifierNameSyntax: values|1# FlowCaptureReferenceOperation: #1 / IdentifierNameSyntax: values|##########}""]
cfg0_block6 [shape=record label=""{BLOCK #6|0# InvocationOperation: Dispose / IdentifierNameSyntax: values|1# FlowCaptureReferenceOperation: #1 / IdentifierNameSyntax: values|##########}""]
cfg0_block7 [shape=record label=""{BLOCK #7}""]
}
}
cfg0_block1 [shape=record label=""{BLOCK #1|0# FlowCaptureOperation: #0 / IdentifierNameSyntax: values|1# InvocationOperation: GetEnumerator / IdentifierNameSyntax: values|2# ConversionOperation / IdentifierNameSyntax: values|3# ParameterReferenceOperation / IdentifierNameSyntax: values|##########}""]
}
cfg0_block0 [shape=record label=""{ENTRY #0}""]
cfg0_block8 [shape=record label=""{EXIT #8}""]
cfg0_block3 -> cfg0_block4
cfg0_block2 -> cfg0_block3 [label=""Else""]
cfg0_block1 -> cfg0_block2
cfg0_block4 -> cfg0_block2
cfg0_block5 -> cfg0_block6 [label=""Else""]
cfg0_block5 -> cfg0_block7 [label=""WhenTrue""]
cfg0_block6 -> cfg0_block7
cfg0_block7 -> NoDestination_cfg0_block7 [label=""StructuredExceptionHandling""]
cfg0_block0 -> cfg0_block1
cfg0_block2 -> cfg0_block8 [label=""WhenFalse""]
}
");
        }

        [TestMethod]
        public void Serialize_For()
        {
            var code = @"
class Sample
{
    void Method()
    {
        for (var i = 0; i < 10; i++)
        {
            Bar(i);
        }
    }
    private void Bar(int i) { }
}
";
            var dot = CfgSerializer.Serialize(TestHelper.CompileCfgCS(code));
            dot.Should().BeIgnoringLineEndings(
@"digraph ""RoslynCfg"" {
subgraph ""cluster_1"" {
label = ""LocalLifetime region, Locals: i""
cfg0_block1 [shape=record label=""{BLOCK #1|0# SimpleAssignmentOperation / VariableDeclaratorSyntax: i = 0|1# LocalReferenceOperation / VariableDeclaratorSyntax: i = 0|1# LiteralOperation / LiteralExpressionSyntax: 0|##########}""]
cfg0_block2 [shape=record label=""{BLOCK #2|## BranchValue ##|0# BinaryOperation / BinaryExpressionSyntax: i \< 10|1# LocalReferenceOperation / IdentifierNameSyntax: i|1# LiteralOperation / LiteralExpressionSyntax: 10|##########}""]
cfg0_block3 [shape=record label=""{BLOCK #3|0# ExpressionStatementOperation / ExpressionStatementSyntax: Bar(i);|1# InvocationOperation: Bar / InvocationExpressionSyntax: Bar(i)|2# InstanceReferenceOperation / IdentifierNameSyntax: Bar|2# ArgumentOperation / ArgumentSyntax: i|3# LocalReferenceOperation / IdentifierNameSyntax: i|##########|0# ExpressionStatementOperation / PostfixUnaryExpressionSyntax: i++|1# IncrementOrDecrementOperation / PostfixUnaryExpressionSyntax: i++|2# LocalReferenceOperation / IdentifierNameSyntax: i|##########}""]
}
cfg0_block0 [shape=record label=""{ENTRY #0}""]
cfg0_block4 [shape=record label=""{EXIT #4}""]
cfg0_block0 -> cfg0_block1
cfg0_block1 -> cfg0_block2
cfg0_block3 -> cfg0_block2
cfg0_block2 -> cfg0_block3 [label=""Else""]
cfg0_block2 -> cfg0_block4 [label=""WhenFalse""]
}
");
        }

        [TestMethod]
        public void Serialize_Using()
        {
            var code = @"
class Sample
{
    void Method()
    {
        using (var x = new System.IO.MemoryStream())
        {
            Bar();
        }
    }
    private void Bar() { }
}";
            var dot = CfgSerializer.Serialize(TestHelper.CompileCfgCS(code));
            dot.Should().BeIgnoringLineEndings(
@"digraph ""RoslynCfg"" {
subgraph ""cluster_1"" {
label = ""LocalLifetime region, Locals: x""
subgraph ""cluster_2"" {
label = ""TryAndFinally region""
subgraph ""cluster_3"" {
label = ""Try region""
cfg0_block2 [shape=record label=""{BLOCK #2|0# ExpressionStatementOperation / ExpressionStatementSyntax: Bar();|1# InvocationOperation: Bar / InvocationExpressionSyntax: Bar()|2# InstanceReferenceOperation / IdentifierNameSyntax: Bar|##########}""]
}
subgraph ""cluster_4"" {
label = ""Finally region""
cfg0_block3 [shape=record label=""{BLOCK #3|## BranchValue ##|0# IsNullOperation / VariableDeclaratorSyntax: x = new System.IO.MemoryStream()|1# LocalReferenceOperation / VariableDeclaratorSyntax: x = new System.IO.MemoryStream()|##########}""]
cfg0_block4 [shape=record label=""{BLOCK #4|0# InvocationOperation: Dispose / VariableDeclaratorSyntax: x = new System.IO.MemoryStream()|1# ConversionOperation / VariableDeclaratorSyntax: x = new System.IO.MemoryStream()|2# LocalReferenceOperation / VariableDeclaratorSyntax: x = new System.IO.MemoryStream()|##########}""]
cfg0_block5 [shape=record label=""{BLOCK #5}""]
}
}
cfg0_block1 [shape=record label=""{BLOCK #1|0# SimpleAssignmentOperation / VariableDeclaratorSyntax: x = new System.IO.MemoryStream()|1# LocalReferenceOperation / VariableDeclaratorSyntax: x = new System.IO.MemoryStream()|1# ObjectCreationOperation / ObjectCreationExpressionSyntax: new System.IO.MemoryStream()|##########}""]
}
cfg0_block0 [shape=record label=""{ENTRY #0}""]
cfg0_block6 [shape=record label=""{EXIT #6}""]
cfg0_block1 -> cfg0_block2
cfg0_block3 -> cfg0_block4 [label=""Else""]
cfg0_block3 -> cfg0_block5 [label=""WhenTrue""]
cfg0_block4 -> cfg0_block5
cfg0_block5 -> NoDestination_cfg0_block5 [label=""StructuredExceptionHandling""]
cfg0_block0 -> cfg0_block1
cfg0_block2 -> cfg0_block6
}
");
        }

        [TestMethod]
        public void Serialize_Lock()
        {
            var code = @"
class Sample
{
    object x;

    void Method()
    {
        lock (x)
        {
            Bar();
        }
    }
    private void Bar() { }
}";
            var dot = CfgSerializer.Serialize(TestHelper.CompileCfgCS(code));
            dot.Should().BeIgnoringLineEndings(
@"digraph ""RoslynCfg"" {
subgraph ""cluster_1"" {
label = ""LocalLifetime region, Locals: N/A, Captures: #0""
subgraph ""cluster_2"" {
label = ""TryAndFinally region""
subgraph ""cluster_3"" {
label = ""Try region""
cfg0_block2 [shape=record label=""{BLOCK #2|0# InvocationOperation: Enter / IdentifierNameSyntax: x|1# ArgumentOperation / IdentifierNameSyntax: x|2# FlowCaptureReferenceOperation: #0 / IdentifierNameSyntax: x|1# ArgumentOperation / IdentifierNameSyntax: x|2# LocalReferenceOperation / IdentifierNameSyntax: x|##########|0# ExpressionStatementOperation / ExpressionStatementSyntax: Bar();|1# InvocationOperation: Bar / InvocationExpressionSyntax: Bar()|2# InstanceReferenceOperation / IdentifierNameSyntax: Bar|##########}""]
}
subgraph ""cluster_4"" {
label = ""Finally region""
cfg0_block3 [shape=record label=""{BLOCK #3|## BranchValue ##|0# LocalReferenceOperation / IdentifierNameSyntax: x|##########}""]
cfg0_block4 [shape=record label=""{BLOCK #4|0# InvocationOperation: Exit / IdentifierNameSyntax: x|1# ArgumentOperation / IdentifierNameSyntax: x|2# FlowCaptureReferenceOperation: #0 / IdentifierNameSyntax: x|##########}""]
cfg0_block5 [shape=record label=""{BLOCK #5}""]
}
}
cfg0_block1 [shape=record label=""{BLOCK #1|0# FlowCaptureOperation: #0 / IdentifierNameSyntax: x|1# FieldReferenceOperation / IdentifierNameSyntax: x|2# InstanceReferenceOperation / IdentifierNameSyntax: x|##########}""]
}
cfg0_block0 [shape=record label=""{ENTRY #0}""]
cfg0_block6 [shape=record label=""{EXIT #6}""]
cfg0_block1 -> cfg0_block2
cfg0_block3 -> cfg0_block4 [label=""Else""]
cfg0_block3 -> cfg0_block5 [label=""WhenFalse""]
cfg0_block4 -> cfg0_block5
cfg0_block5 -> NoDestination_cfg0_block5 [label=""StructuredExceptionHandling""]
cfg0_block0 -> cfg0_block1
cfg0_block2 -> cfg0_block6
}
");
        }

        [TestMethod]
        public void Serialize_Regions()
        {
            var code = @"
class Sample
{
    void Method()
    {
        Before();
        try
        {
            InTry();
        }
        finally
        {
            InFinally();
        }
        After();
    }
    private void Before() { }
    private void InTry() { }
    private void InFinally() { }
    private void After() { }
}";
            var dot = CfgSerializer.Serialize(TestHelper.CompileCfgCS(code));
            dot.Should().BeIgnoringLineEndings(
@"digraph ""RoslynCfg"" {
subgraph ""cluster_1"" {
label = ""TryAndFinally region""
subgraph ""cluster_2"" {
label = ""Try region""
cfg0_block2 [shape=record label=""{BLOCK #2|0# ExpressionStatementOperation / ExpressionStatementSyntax: InTry();|1# InvocationOperation: InTry / InvocationExpressionSyntax: InTry()|2# InstanceReferenceOperation / IdentifierNameSyntax: InTry|##########}""]
}
subgraph ""cluster_3"" {
label = ""Finally region""
cfg0_block3 [shape=record label=""{BLOCK #3|0# ExpressionStatementOperation / ExpressionStatementSyntax: InFinally();|1# InvocationOperation: InFinally / InvocationExpressionSyntax: InFinally()|2# InstanceReferenceOperation / IdentifierNameSyntax: InFinally|##########}""]
}
}
cfg0_block0 [shape=record label=""{ENTRY #0}""]
cfg0_block1 [shape=record label=""{BLOCK #1|0# ExpressionStatementOperation / ExpressionStatementSyntax: Before();|1# InvocationOperation: Before / InvocationExpressionSyntax: Before()|2# InstanceReferenceOperation / IdentifierNameSyntax: Before|##########}""]
cfg0_block4 [shape=record label=""{BLOCK #4|0# ExpressionStatementOperation / ExpressionStatementSyntax: After();|1# InvocationOperation: After / InvocationExpressionSyntax: After()|2# InstanceReferenceOperation / IdentifierNameSyntax: After|##########}""]
cfg0_block5 [shape=record label=""{EXIT #5}""]
cfg0_block1 -> cfg0_block2
cfg0_block3 -> NoDestination_cfg0_block3 [label=""StructuredExceptionHandling""]
cfg0_block0 -> cfg0_block1
cfg0_block2 -> cfg0_block4
cfg0_block4 -> cfg0_block5
}
");
        }

        [TestMethod]
        public void Serialize_Region_ExceptionType()
        {
            var code = @"
class Sample
{
    void Method()
    {
        try { }
        catch(System.InvalidOperationException ex) { }
    }
}";
            var dot = CfgSerializer.Serialize(TestHelper.CompileCfgCS(code));
            dot.Should().BeIgnoringLineEndings(
@"digraph ""RoslynCfg"" {
subgraph ""cluster_1"" {
label = ""TryAndCatch region""
subgraph ""cluster_2"" {
label = ""Try region""
cfg0_block1 [shape=record label=""{BLOCK #1}""]
}
subgraph ""cluster_3"" {
label = ""Catch region: System.InvalidOperationException, Locals: ex""
cfg0_block2 [shape=record label=""{BLOCK #2|0# SimpleAssignmentOperation / CatchDeclarationSyntax: (System.InvalidOperationException ex)|1# LocalReferenceOperation / CatchDeclarationSyntax: (System.InvalidOperationException ex)|1# CaughtExceptionOperation / CatchDeclarationSyntax: (System.InvalidOperationException ex)|##########}""]
}
}
cfg0_block0 [shape=record label=""{ENTRY #0}""]
cfg0_block3 [shape=record label=""{EXIT #3}""]
cfg0_block0 -> cfg0_block1
cfg0_block1 -> cfg0_block3
cfg0_block2 -> cfg0_block3
}
");
        }

        [TestMethod]
        public void Serialize_LocalFunctions()
        {
            var code = @"
class Sample
{
    void Method()
    {
        var fourty = 40;
        Local();

        int Local() => fourty + LocalStatic();

        static int LocalStatic() => LocalStaticArg(1);
        static int LocalStaticArg(int one) => one + 1; // Overloaded
    }
}";
            var dot = CfgSerializer.Serialize(TestHelper.CompileCfgCS(code));
            dot.Should().BeIgnoringLineEndings(
@"digraph ""RoslynCfg"" {
subgraph ""cluster_1"" {
label = ""LocalLifetime region, Locals: fourty""
cfg0_block1 [shape=record label=""{BLOCK #1|0# SimpleAssignmentOperation / VariableDeclaratorSyntax: fourty = 40|1# LocalReferenceOperation / VariableDeclaratorSyntax: fourty = 40|1# LiteralOperation / LiteralExpressionSyntax: 40|##########|0# ExpressionStatementOperation / ExpressionStatementSyntax: Local();|1# InvocationOperation: Local / InvocationExpressionSyntax: Local()|##########}""]
}
cfg0_block0 [shape=record label=""{ENTRY #0}""]
cfg0_block2 [shape=record label=""{EXIT #2}""]
subgraph ""cluster_3"" {
label = ""RoslynCfg.Local""
cfg2_block0 [shape=record label=""{ENTRY #0}""]
cfg2_block1 [shape=record label=""{BLOCK #1|## BranchValue ##|0# BinaryOperation / BinaryExpressionSyntax: fourty + LocalStatic()|1# LocalReferenceOperation / IdentifierNameSyntax: fourty|1# InvocationOperation: LocalStatic / InvocationExpressionSyntax: LocalStatic()|##########}""]
cfg2_block2 [shape=record label=""{EXIT #2}""]
}
subgraph ""cluster_5"" {
label = ""RoslynCfg.LocalStatic""
cfg4_block0 [shape=record label=""{ENTRY #0}""]
cfg4_block1 [shape=record label=""{BLOCK #1|## BranchValue ##|0# InvocationOperation: LocalStaticArg / InvocationExpressionSyntax: LocalStaticArg(1)|1# ArgumentOperation / ArgumentSyntax: 1|2# LiteralOperation / LiteralExpressionSyntax: 1|##########}""]
cfg4_block2 [shape=record label=""{EXIT #2}""]
}
subgraph ""cluster_7"" {
label = ""RoslynCfg.LocalStaticArg""
cfg6_block0 [shape=record label=""{ENTRY #0}""]
cfg6_block1 [shape=record label=""{BLOCK #1|## BranchValue ##|0# BinaryOperation / BinaryExpressionSyntax: one + 1|1# ParameterReferenceOperation / IdentifierNameSyntax: one|1# LiteralOperation / LiteralExpressionSyntax: 1|##########}""]
cfg6_block2 [shape=record label=""{EXIT #2}""]
}
cfg0_block0 -> cfg0_block1
cfg0_block1 -> cfg0_block2
cfg2_block0 -> cfg2_block1
cfg2_block1 -> cfg2_block2 [label=""Return""]
cfg4_block0 -> cfg4_block1
cfg4_block1 -> cfg4_block2 [label=""Return""]
cfg6_block0 -> cfg6_block1
cfg6_block1 -> cfg6_block2 [label=""Return""]
}
");
        }

        [TestMethod]
        public void Serialize_Lambdas()
        {
            var code = @"
class Sample
{
    void Method(int arg)
    {
        Bar(x => { return arg + 1; });
        Bar(x => arg - 1);
    }
    private void Bar(System.Func<int, int> f) { }
}";
            var dot = CfgSerializer.Serialize(TestHelper.CompileCfgCS(code));
            dot.Should().BeIgnoringLineEndings(
@"digraph ""RoslynCfg"" {
cfg0_block0 [shape=record label=""{ENTRY #0}""]
cfg0_block1 [shape=record label=""{BLOCK #1|0# ExpressionStatementOperation / ExpressionStatementSyntax: Bar(x =\> \{ return arg + 1; \});|1# InvocationOperation: Bar / InvocationExpressionSyntax: Bar(x =\> \{ return arg + 1; \})|2# InstanceReferenceOperation / IdentifierNameSyntax: Bar|2# ArgumentOperation / ArgumentSyntax: x =\> \{ return arg + 1; \}|3# DelegateCreationOperation / SimpleLambdaExpressionSyntax: x =\> \{ return arg + 1; \}|4# FlowAnonymousFunctionOperation / SimpleLambdaExpressionSyntax: x =\> \{ return arg + 1; \}|##########|0# ExpressionStatementOperation / ExpressionStatementSyntax: Bar(x =\> arg - 1);|1# InvocationOperation: Bar / InvocationExpressionSyntax: Bar(x =\> arg - 1)|2# InstanceReferenceOperation / IdentifierNameSyntax: Bar|2# ArgumentOperation / ArgumentSyntax: x =\> arg - 1|3# DelegateCreationOperation / SimpleLambdaExpressionSyntax: x =\> arg - 1|4# FlowAnonymousFunctionOperation / SimpleLambdaExpressionSyntax: x =\> arg - 1|##########}""]
cfg0_block2 [shape=record label=""{EXIT #2}""]
subgraph ""cluster_2"" {
label = ""RoslynCfg.anonymous""
cfg1_block0 [shape=record label=""{ENTRY #0}""]
cfg1_block1 [shape=record label=""{BLOCK #1|## BranchValue ##|0# BinaryOperation / BinaryExpressionSyntax: arg + 1|1# ParameterReferenceOperation / IdentifierNameSyntax: arg|1# LiteralOperation / LiteralExpressionSyntax: 1|##########}""]
cfg1_block2 [shape=record label=""{EXIT #2}""]
}
subgraph ""cluster_4"" {
label = ""RoslynCfg.anonymous""
cfg3_block0 [shape=record label=""{ENTRY #0}""]
cfg3_block1 [shape=record label=""{BLOCK #1|## BranchValue ##|0# BinaryOperation / BinaryExpressionSyntax: arg - 1|1# ParameterReferenceOperation / IdentifierNameSyntax: arg|1# LiteralOperation / LiteralExpressionSyntax: 1|##########}""]
cfg3_block2 [shape=record label=""{EXIT #2}""]
}
cfg0_block0 -> cfg0_block1
cfg0_block1 -> cfg0_block2
cfg1_block0 -> cfg1_block1
cfg1_block1 -> cfg1_block2 [label=""Return""]
cfg3_block0 -> cfg3_block1
cfg3_block1 -> cfg3_block2 [label=""Return""]
}
");
        }

        [TestMethod]
        public void Serialize_CaptureId()
        {
            var code = @"
class Sample
{
    void Method(bool arg)
    {
        bool b = arg && false;
        b = arg || true;
    }
}";
            var dot = CfgSerializer.Serialize(TestHelper.CompileCfgCS(code));
            dot.Should().BeIgnoringLineEndings(
@"digraph ""RoslynCfg"" {
subgraph ""cluster_1"" {
label = ""LocalLifetime region, Locals: b""
subgraph ""cluster_2"" {
label = ""LocalLifetime region, Captures: #0""
cfg0_block1 [shape=record label=""{BLOCK #1|## BranchValue ##|0# ParameterReferenceOperation / IdentifierNameSyntax: arg|##########}""]
cfg0_block2 [shape=record label=""{BLOCK #2|0# FlowCaptureOperation: #0 / LiteralExpressionSyntax: false|1# LiteralOperation / LiteralExpressionSyntax: false|##########}""]
cfg0_block3 [shape=record label=""{BLOCK #3|0# FlowCaptureOperation: #0 / IdentifierNameSyntax: arg|1# LiteralOperation / IdentifierNameSyntax: arg|##########}""]
cfg0_block4 [shape=record label=""{BLOCK #4|0# SimpleAssignmentOperation / VariableDeclaratorSyntax: b = arg && false|1# LocalReferenceOperation / VariableDeclaratorSyntax: b = arg && false|1# FlowCaptureReferenceOperation: #0 / BinaryExpressionSyntax: arg && false|##########}""]
}
subgraph ""cluster_3"" {
label = ""LocalLifetime region, Captures: #1, #2""
cfg0_block5 [shape=record label=""{BLOCK #5|0# FlowCaptureOperation: #1 / IdentifierNameSyntax: b|1# LocalReferenceOperation / IdentifierNameSyntax: b|##########|## BranchValue ##|0# ParameterReferenceOperation / IdentifierNameSyntax: arg|##########}""]
cfg0_block6 [shape=record label=""{BLOCK #6|0# FlowCaptureOperation: #2 / LiteralExpressionSyntax: true|1# LiteralOperation / LiteralExpressionSyntax: true|##########}""]
cfg0_block7 [shape=record label=""{BLOCK #7|0# FlowCaptureOperation: #2 / IdentifierNameSyntax: arg|1# LiteralOperation / IdentifierNameSyntax: arg|##########}""]
cfg0_block8 [shape=record label=""{BLOCK #8|0# ExpressionStatementOperation / ExpressionStatementSyntax: b = arg \|\| true;|1# SimpleAssignmentOperation / AssignmentExpressionSyntax: b = arg \|\| true|2# FlowCaptureReferenceOperation: #1 / IdentifierNameSyntax: b|2# FlowCaptureReferenceOperation: #2 / BinaryExpressionSyntax: arg \|\| true|##########}""]
}
}
cfg0_block0 [shape=record label=""{ENTRY #0}""]
cfg0_block9 [shape=record label=""{EXIT #9}""]
cfg0_block0 -> cfg0_block1
cfg0_block1 -> cfg0_block2 [label=""Else""]
cfg0_block1 -> cfg0_block3 [label=""WhenFalse""]
cfg0_block2 -> cfg0_block4
cfg0_block3 -> cfg0_block4
cfg0_block4 -> cfg0_block5
cfg0_block5 -> cfg0_block6 [label=""Else""]
cfg0_block5 -> cfg0_block7 [label=""WhenTrue""]
cfg0_block6 -> cfg0_block8
cfg0_block7 -> cfg0_block8
cfg0_block8 -> cfg0_block9
}
");
        }

        [TestMethod]
        public void Serialize_InvalidOperation()
        {
            var code = @"
class Sample
{
    void Method()
    {
        undefined();
    }
}";
            var dot = CfgSerializer.Serialize(TestHelper.CompileCfgCS(code, ignoreErrors: true));
            dot.Should().BeIgnoringLineEndings(
@"digraph ""RoslynCfg"" {
cfg0_block0 [shape=record label=""{ENTRY #0}""]
cfg0_block1 [shape=record label=""{BLOCK #1|0# ExpressionStatementOperation / ExpressionStatementSyntax: undefined();|1# INVALID / InvocationExpressionSyntax: undefined()|2# INVALID / IdentifierNameSyntax: undefined|##########}""]
cfg0_block2 [shape=record label=""{EXIT #2}""]
cfg0_block0 -> cfg0_block1
cfg0_block1 -> cfg0_block2
}
");
        }

        [TestMethod]
        public void Serialize_TryCatchChain()
        {
            var code = @"
class Sample
{
    void Method()
    {
        try { }
        catch { }

        try { }
        catch { }
    }
}";
            var dot = CfgSerializer.Serialize(TestHelper.CompileCfgCS(code));
            dot.Should().BeIgnoringLineEndings(
@"digraph ""RoslynCfg"" {
subgraph ""cluster_1"" {
label = ""TryAndCatch region""
subgraph ""cluster_2"" {
label = ""Try region""
cfg0_block1 [shape=record label=""{BLOCK #1}""]
}
subgraph ""cluster_3"" {
label = ""Catch region: object""
cfg0_block2 [shape=record label=""{BLOCK #2}""]
}
}
subgraph ""cluster_4"" {
label = ""TryAndCatch region""
subgraph ""cluster_5"" {
label = ""Try region""
cfg0_block3 [shape=record label=""{BLOCK #3}""]
}
subgraph ""cluster_6"" {
label = ""Catch region: object""
cfg0_block4 [shape=record label=""{BLOCK #4}""]
}
}
cfg0_block0 [shape=record label=""{ENTRY #0}""]
cfg0_block5 [shape=record label=""{EXIT #5}""]
cfg0_block0 -> cfg0_block1
cfg0_block1 -> cfg0_block3
cfg0_block2 -> cfg0_block3
cfg0_block3 -> cfg0_block5
cfg0_block4 -> cfg0_block5
}
");
        }
    }
}
