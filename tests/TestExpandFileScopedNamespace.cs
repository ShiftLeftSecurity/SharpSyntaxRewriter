using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using SharpSyntaxRewriter.Rewriters;

namespace Tests
{
    [TestClass]
    public class TestExpandFileScopedNamespace : RewriterTester
    {
        protected override SyntaxTree ApplyRewrite(SyntaxTree tree, Compilation compilation)
        {
            return new ExpandFileScopedNamespace().Apply(tree, compilation.GetSemanticModel(tree));
        }
        
        [TestMethod]
        public void TestExpandFileScopedNamespaceOverSingleClassDeclaration()
        {
            var original = @"
namespace MyNamespace;
class C {}";

            var expected = @"
namespace MyNamespace
{class C {}};";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestExpandFileScopedNamespaceOverTwoClassDeclarations()
        {
            var original = @"
namespace M;
class C{}

class D{}";

            var expected = @"
namespace M
{class C{}

class D{}};";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestFileScopedNamespaceOverEveryPossibleKindOfDeclarationSimultaneously()
        {
            var original = @"
using System;

namespace SampleFileScopedNamespace;

class SampleClass { }

interface ISampleInterface { }

struct SampleStruct { }

enum SampleEnum { a, b }

delegate void SampleDelegate(int i);";

            var expected = @"
using System;

namespace SampleFileScopedNamespace
{
class SampleClass { }

interface ISampleInterface { }

struct SampleStruct { }

enum SampleEnum { a, b }

delegate void SampleDelegate(int i);};";

            TestRewrite_LinePreserve(original, expected);
        }
    }
}
