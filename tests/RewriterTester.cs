// Copyright 2021 ShiftLeft, Inc.
// Author: Leandro T. C. Melo

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

using SharpSyntaxRewriter.Extensions;

namespace Tests
{
    public abstract class RewriterTester : BaseTester
    {
        protected abstract SyntaxTree ApplyRewrite(SyntaxTree tree,
                                                   Compilation compilation);

        private void TestRewrite(string original,
                                 string expected,
                                 string extra,
                                 Func<string, string> format)
        {
            var (compilation, trees) =
                (extra == null)
                    ? TestCompilationFromSourceTexts(original)
                    : TestCompilationFromSourceTexts(original, extra);

            var rewrittenTree = ApplyRewrite(trees.First(), compilation);

            Assert.AreEqual($"\n{format(expected)}\n",
                            $"\n{format(rewrittenTree.ToString())}\n");

            if (extra != null)
            {
                TestCompilationFromSyntaxTrees(
                    rewrittenTree,
                    CSharpSyntaxTree.ParseText(extra));
            }
            else
            {
                TestCompilationFromSyntaxTrees(rewrittenTree);
            }
        }

        protected virtual void TestRewrite_LineIgnore(
                string original,
                string expected,
                string extra = null)
        {
            TestRewrite(original,
                        expected,
                        extra,
                        StringExtensionMethods.WithoutAnySpace);
        }

        protected virtual void TestRewrite_LinePreserve(
                string original,
                string expected,
                string extra = null)
        {
            TestRewrite(original,
                        expected,
                        extra,
                        StringExtensionMethods.WithoutWhiteSpace);
        }
    }
}
