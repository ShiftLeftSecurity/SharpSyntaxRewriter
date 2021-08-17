// Copyright 2021 ShiftLeft, Inc.
// Author: Leandro T. C. Melo

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Tests
{
    public class BaseTester
    {
        private Compiler compiler__;

        [TestInitialize]
        public void InitializeTest()
        {
            compiler__ = new();
        }

        [TestCleanup]
        public void CleanupTest()
        {}

        protected Compilation TestCompilationFromSyntaxTrees(params SyntaxTree[] trees)
        {
            var compilation = compiler__.CompileSyntaxTrees(trees);
            CheckCompilation(compilation);
            return compilation;
        }

        protected (Compilation, List<SyntaxTree>)
            TestCompilationFromSourceTexts(params string[] srcs)
        {
            var (compilation, trees) = compiler__.CompileSourceTexts(srcs);
            CheckCompilation(compilation);
            return (compilation, trees);
        }

        protected void CheckCompilation(Compilation compilation)
        {
            // Always write the diagnostics for convenience. (`stdout'
            // is only flushed upon hard failures.)
            compilation.GetDiagnostics().ToList().ForEach(Console.WriteLine);

            Assert.AreEqual(
                0,
                compilation.GetDiagnostics()
                           .Where(d => d.Severity == DiagnosticSeverity.Error)
                           .Count());
        }
    }
}
