// Copyright 2021 ShiftLeft, Inc.
// Author: Leandro T. C. Melo

#define DEBUG_SYNTAX

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Tests
{
    public class BaseTester
    {
        private Compiler __compiler;

        [TestInitialize]
        public void InitializeTest()
        {
            __compiler = new();
        }

        [TestCleanup]
        public void CleanupTest()
        {}

        protected Compilation TestCompilationFromSyntaxTrees(params SyntaxTree[] trees)
        {
            var compilation = __compiler.CompileSyntaxTrees(trees);
            CheckCompilation(compilation);
            return compilation;
        }

        protected (Compilation, List<SyntaxTree>)
            TestCompilationFromSourceTexts(params string[] srcs)
        {
            var (compilation, trees) = __compiler.CompileSourceTexts(srcs);
            CheckCompilation(compilation);
            return (compilation, trees);
        }

        protected static void CheckCompilation(Compilation compilation)
        {
            var errorCnt = compilation.GetDiagnostics()
                    .Where(d => d.Severity == DiagnosticSeverity.Error
                                && d.Descriptor.Id != "CS1547")
                    .Count();

#if DEBUG_SYNTAX
            if (errorCnt != 0)
            {
                compilation.GetDiagnostics()
                    .Where(d => d.Severity == DiagnosticSeverity.Error)
                    .ToList().ForEach(Console.WriteLine);
                compilation.SyntaxTrees.ToList().ForEach(Console.WriteLine);
            }
#endif

            Assert.AreEqual(0, errorCnt, "rewritten syntax tree has errors");
        }

        protected void CompileAsExecutable()
        {
            __compiler.OutputKindCompilationOpt = OutputKind.ConsoleApplication;
        }
    }
}
