// Copyright 2021 ShiftLeft, Inc.
// Author: Leandro T. C. Melo

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Tests
{
    public class Compiler
    {
        public Compilation CompileSyntaxTrees(params SyntaxTree[] trees)
        {
            return CSharpCompilation.Create("<default-compilation>",
                                            trees.ToList(),
                                            metadataRefs__,
                                            compilationOpts__);
        }

        public (Compilation, List<SyntaxTree>) CompileSourceTexts(
                params string[] srcs)
        {
            var trees = new List<SyntaxTree>();
            srcs.ToList().ForEach(t => trees.Add(CSharpSyntaxTree.ParseText(t)));
            return (CompileSyntaxTrees(trees.ToArray()), trees);
        }

        private CSharpParseOptions parseOpts__
        {
            get
            {
                return CSharpParseOptions
                            .Default
                            .WithLanguageVersion(LanguageVersion.Latest);
            }
        }

        private CSharpCompilationOptions compilationOpts__
        {
            get
            {
                return
                    new CSharpCompilationOptions(
                        OutputKind.DynamicallyLinkedLibrary,
                        allowUnsafe: true,
                        optimizationLevel: OptimizationLevel.Debug,
                        platform: Platform.AnyCpu,
                        warningLevel: 4);
            }
        }

        private IEnumerable<MetadataReference> metadataRefs__
        {
            get
            {
                return
                    new List<MetadataReference> {
                        MetadataReference.CreateFromFile(typeof(Object).Assembly.Location),
                        MetadataReference.CreateFromFile(Assembly.Load(
                            new AssemblyName("System")).Location),
                        MetadataReference.CreateFromFile(Assembly.Load(
                            new AssemblyName("System.Collections")).Location),
                        MetadataReference.CreateFromFile(Assembly.Load(
                            new AssemblyName("System.Console")).Location),
                        MetadataReference.CreateFromFile(Assembly.Load(
                            new AssemblyName("System.Diagnostics.Process")).Location),
                        MetadataReference.CreateFromFile(Assembly.Load(
                            new AssemblyName("System.IO")).Location),
                        MetadataReference.CreateFromFile(Assembly.Load(
                            new AssemblyName("System.IO.FileSystem")).Location),
                        MetadataReference.CreateFromFile(Assembly.Load(
                            new AssemblyName("System.Linq")).Location),
                        MetadataReference.CreateFromFile(Assembly.Load(
                            new AssemblyName("System.Linq.Expressions")).Location),
                        MetadataReference.CreateFromFile(Assembly.Load(
                            new AssemblyName("System.Linq.Queryable")).Location),
                        MetadataReference.CreateFromFile(Assembly.Load(
                            new AssemblyName("System.ObjectModel")).Location),
                        MetadataReference.CreateFromFile(Assembly.Load(
                            new AssemblyName("System.Private.Uri")).Location),
                        MetadataReference.CreateFromFile(Assembly.Load(
                            new AssemblyName("System.Private.Xml")).Location),
                        MetadataReference.CreateFromFile(Assembly.Load(
                            new AssemblyName("System.Private.Xml.Linq")).Location),
                        MetadataReference.CreateFromFile(Assembly.Load(
                            new AssemblyName("System.Runtime")).Location),
                        MetadataReference.CreateFromFile(Assembly.Load(
                            new AssemblyName("System.Security.Cryptography.Algorithms")).Location),
                        MetadataReference.CreateFromFile(Assembly.Load(
                            new AssemblyName("System.Xml.ReaderWriter")).Location),
                        MetadataReference.CreateFromFile(Assembly.Load(
                            new AssemblyName("System.Xml.XDocument")).Location),
                        MetadataReference.CreateFromFile(Assembly.Load(
                            new AssemblyName("System.Web.HttpUtility")).Location)
                };
            }
        }
    }
}
