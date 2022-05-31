// Copyright 2021 ShiftLeft, Inc.
// Author: Leandro T. C. Melo

using System;

namespace SharpSyntaxRewriter.Rewriters.Exceptions
{
    public class UnexpectedInaccurateRewriteException : Exception
    {
        public UnexpectedInaccurateRewriteException() { }
        public UnexpectedInaccurateRewriteException(string message) : base(message) { }
        public UnexpectedInaccurateRewriteException(string message, Exception inner) : base(message, inner) { }
    }
}

