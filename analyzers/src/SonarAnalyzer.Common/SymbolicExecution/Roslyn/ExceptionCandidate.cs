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
using SonarAnalyzer.Helpers;
using StyleCop.Analyzers.Lightup;

namespace SonarAnalyzer.SymbolicExecution.Roslyn
{
    internal class ExceptionCandidate
    {
        private readonly TypeCatalog typeCatalog;

        public ExceptionCandidate(Compilation compilation) =>
            typeCatalog = new TypeCatalog(compilation);

        public static bool IsMonitorExit(IInvocationOperationWrapper invocation) =>
            invocation.TargetMethod.Is(KnownType.System_Threading_Monitor, "Exit");

        public static bool IsLockRelease(IInvocationOperationWrapper invocation) =>
            invocation.TargetMethod.IsAny(KnownType.System_Threading_ReaderWriterLock, "ReleaseLock", "ReleaseReaderLock", "ReleaseWriterLock")
            || invocation.TargetMethod.IsAny(KnownType.System_Threading_ReaderWriterLockSlim, "ExitReadLock", "ExitWriteLock", "ExitUpgradeableReadLock")
            || invocation.TargetMethod.Is(KnownType.System_Threading_Mutex, "ReleaseMutex")
            || invocation.TargetMethod.Is(KnownType.System_Threading_SpinLock, "Exit");

        public ExceptionState FromOperation(IOperationWrapperSonar operation) =>
            operation.Instance.Kind switch
            {
                OperationKindEx.ArrayElementReference => new ExceptionState(typeCatalog.SystemIndexOutOfRangeException),
                OperationKindEx.Conversion => ConversionExceptionCandidate(operation),
                OperationKindEx.DynamicIndexerAccess => new ExceptionState(typeCatalog.SystemIndexOutOfRangeException),
                OperationKindEx.DynamicInvocation => ExceptionState.UnknownException,      // The raised exception is Microsoft.CSharp.RuntimeBinder.RuntimeBinderException for which we don't have access.
                OperationKindEx.DynamicMemberReference => ExceptionState.UnknownException, // The raised exception is Microsoft.CSharp.RuntimeBinder.RuntimeBinderException for which we don't have access.
                OperationKindEx.DynamicObjectCreation => ExceptionState.UnknownException,  // The raised exception is Microsoft.CSharp.RuntimeBinder.RuntimeBinderException for which we don't have access.
                OperationKindEx.EventReference => FromOperation(IMemberReferenceOperationWrapper.FromOperation(operation.Instance)),
                OperationKindEx.FieldReference => FromOperation(IMemberReferenceOperationWrapper.FromOperation(operation.Instance)),
                OperationKindEx.Invocation => FromOperation(IInvocationOperationWrapper.FromOperation(operation.Instance)),
                OperationKindEx.MethodReference => FromOperation(IMemberReferenceOperationWrapper.FromOperation(operation.Instance)),
                OperationKindEx.ObjectCreation => operation.Instance.Type.DerivesFrom(KnownType.System_Exception) ? null : ExceptionState.UnknownException, // ToDo: Filter out exception constructors assuming that usually they do not throw.
                OperationKindEx.PropertyReference => FromOperation(IMemberReferenceOperationWrapper.FromOperation(operation.Instance)),
                _ => null
            };

        private ExceptionState FromOperation(IMemberReferenceOperationWrapper reference) =>
            reference.IsStaticOrThis() ? null : new ExceptionState(typeCatalog.SystemNullReferenceException);

        private ExceptionState ConversionExceptionCandidate(IOperationWrapperSonar operation)
        {
            if (operation.IsImplicit)
            {
                return null;
            }

            var conversion = IConversionOperationWrapper.FromOperation(operation.Instance);
            return conversion.Operand.Type.DerivesOrImplements(conversion.Type)
                       ? null
                       : new ExceptionState(typeCatalog.SystemInvalidCastException);
        }

        private ExceptionState FromOperation(IInvocationOperationWrapper invocation) =>
            IsMonitorExit(invocation)
            || IsLockRelease(invocation)
            ? null
            : ExceptionState.UnknownException;
    }
}
