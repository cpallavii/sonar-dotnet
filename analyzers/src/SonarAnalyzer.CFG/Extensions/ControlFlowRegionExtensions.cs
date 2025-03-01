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

using System.Collections.Generic;
using System.Linq;
using SonarAnalyzer.CFG.Roslyn;

namespace SonarAnalyzer.Extensions
{
    public static class ControlFlowRegionExtensions
    {
        public static IEnumerable<BasicBlock> Blocks(this ControlFlowRegion region, ControlFlowGraph cfg) =>
            cfg.Blocks.Where((_, i) => region.FirstBlockOrdinal <= i && i <= region.LastBlockOrdinal);

        public static ControlFlowRegion EnclosingNonLocalLifetimeRegion(this ControlFlowRegion region)
        {
            while (region.EnclosingRegion is not null && region.Kind == ControlFlowRegionKind.LocalLifetime)
            {
                region = region.EnclosingRegion;
            }
            return region;
        }

        public static ControlFlowRegion EnclosingRegionOrSelf(this ControlFlowRegion region, ControlFlowRegionKind kind)
        {
            while (region is not null && region.Kind != kind)
            {
                region = region.EnclosingRegion;
            }
            return region;
        }

        public static ControlFlowRegion EnclosingRegion(this ControlFlowRegion region, ControlFlowRegionKind kind) =>
            region.EnclosingRegion.EnclosingRegionOrSelf(kind);

        public static ControlFlowRegion NestedRegion(this ControlFlowRegion region, ControlFlowRegionKind kind) =>
            region.NestedRegions.Single(x => x.Kind == kind);
    }
}
