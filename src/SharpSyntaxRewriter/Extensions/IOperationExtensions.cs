// Copyright 2021 ShiftLeft, Inc.
// Author: Leandro T. C. Melo

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using System.Diagnostics;

namespace SharpSyntaxRewriter.Extensions
{
    public static class IOperationExtensions
    {
        public static ISymbol ImmediateTargetSymbol(this IOperation op)
        {
            Debug.Assert(op != null);

            if (op is IAnonymousFunctionOperation anonFunOp)
                return anonFunOp.Symbol;

            if (op is IFieldReferenceOperation fldRefOp)
                return fldRefOp.Field;

            if (op is IEventReferenceOperation evtRefOp)
                return evtRefOp.Event;

            if (op is ILocalReferenceOperation locRefOp)
                return locRefOp.Local;

            if (op is ILocalFunctionOperation locFunOp)
                return locFunOp.Symbol;

            if (op is IInvocationOperation callOp)
                return callOp.TargetMethod;

            if (op is IMemberReferenceOperation membRefOp)
                return membRefOp.Member;

            if (op is IMethodReferenceOperation methRefOp)
                return methRefOp.Method;

            if (op is IParameterReferenceOperation parmRefOp)
                return parmRefOp.Parameter;

            if (op is IPropertyReferenceOperation propRefOp)
                return propRefOp.Property;

            return null;
        }

        public static ISymbol TargetSymbol(this IOperation op)
        {
            var sym = op.ImmediateTargetSymbol();
            if (sym != null)
                return sym;

            if (op is IAddressOfOperation addrOfOp)
                return TargetSymbol(addrOfOp.Reference);

            if (op is IArrayElementReferenceOperation elemRefOp)
                return TargetSymbol(elemRefOp.ArrayReference);

            if (op is IConversionOperation convOp)
                return TargetSymbol(convOp.Operand);

            if (op is IDelegateCreationOperation delgOp)
                return TargetSymbol(delgOp.Target);

            if (op is IUnaryOperation unaryOp)
                return TargetSymbol(unaryOp.Operand);

            if (op is IDynamicIndexerAccessOperation dynIdxOp)
                return TargetSymbol(dynIdxOp.Operation);

            return null;
        }
    }
}
