// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

// <auto-generated />
// This code is not auto-generated but is a copy-paste from https://github.com/DotNetAnalyzers/StyleCopAnalyzers for which
// we want to ignore all issues.

namespace SonarAnalyzer.ShimLayer.CSharp
{
    using System;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;

    internal struct VarPatternSyntaxWrapper : ISyntaxWrapper<CSharpSyntaxNode>
    {
        internal const string WrappedTypeName = "Microsoft.CodeAnalysis.CSharp.Syntax.VarPatternSyntax";
        private static readonly Type WrappedType;

        private static readonly Func<CSharpSyntaxNode, SyntaxToken> VarKeywordAccessor;
        private static readonly Func<CSharpSyntaxNode, CSharpSyntaxNode> DesignationAccessor;

        private static readonly Func<CSharpSyntaxNode, SyntaxToken, CSharpSyntaxNode> WithVarKeywordAccessor;
        private static readonly Func<CSharpSyntaxNode, CSharpSyntaxNode, CSharpSyntaxNode> WithDesignationAccessor;

        private readonly CSharpSyntaxNode node;

        static VarPatternSyntaxWrapper()
        {
            WrappedType = WrapperHelper.GetWrappedType(typeof(VarPatternSyntaxWrapper));
            VarKeywordAccessor = LightupHelpers.CreateSyntaxPropertyAccessor<CSharpSyntaxNode, SyntaxToken>(WrappedType, nameof(VarKeyword));
            DesignationAccessor = LightupHelpers.CreateSyntaxPropertyAccessor<CSharpSyntaxNode, CSharpSyntaxNode>(WrappedType, nameof(Designation));

            WithVarKeywordAccessor = LightupHelpers.CreateSyntaxWithPropertyAccessor<CSharpSyntaxNode, SyntaxToken>(WrappedType, nameof(VarKeyword));
            WithDesignationAccessor = LightupHelpers.CreateSyntaxWithPropertyAccessor<CSharpSyntaxNode, CSharpSyntaxNode>(WrappedType, nameof(Designation));
        }

        private VarPatternSyntaxWrapper(CSharpSyntaxNode node)
        {
            this.node = node;
        }

        public CSharpSyntaxNode SyntaxNode => this.node;

        public SyntaxToken VarKeyword
        {
            get
            {
                return VarKeywordAccessor(this.SyntaxNode);
            }
        }

        public VariableDesignationSyntaxWrapper Designation
        {
            get
            {
                return (VariableDesignationSyntaxWrapper)DesignationAccessor(this.SyntaxNode);
            }
        }

        public static explicit operator VarPatternSyntaxWrapper(PatternSyntaxWrapper node)
        {
            return (VarPatternSyntaxWrapper)node.SyntaxNode;
        }

        public static explicit operator VarPatternSyntaxWrapper(SyntaxNode node)
        {
            if (node == null)
            {
                return new VarPatternSyntaxWrapper();
            }

            if (!IsInstance(node))
            {
                throw new InvalidCastException($"Cannot cast '{node.GetType().FullName}' to '{WrappedTypeName}'");
            }

            return new VarPatternSyntaxWrapper((CSharpSyntaxNode)node);
        }

        public static implicit operator PatternSyntaxWrapper(VarPatternSyntaxWrapper wrapper)
        {
            return PatternSyntaxWrapper.FromUpcast(wrapper.node);
        }

        public static implicit operator CSharpSyntaxNode(VarPatternSyntaxWrapper wrapper)
        {
            return wrapper.node;
        }

        public static bool IsInstance(SyntaxNode node)
        {
            return node != null && LightupHelpers.CanWrapNode(node, WrappedType);
        }

        public VarPatternSyntaxWrapper WithVarKeyword(SyntaxToken varKeyword)
        {
            return new VarPatternSyntaxWrapper(WithVarKeywordAccessor(this.SyntaxNode, varKeyword));
        }

        public VarPatternSyntaxWrapper WithDesignation(VariableDesignationSyntaxWrapper designation)
        {
            return new VarPatternSyntaxWrapper(WithDesignationAccessor(this.SyntaxNode, designation));
        }
    }
}