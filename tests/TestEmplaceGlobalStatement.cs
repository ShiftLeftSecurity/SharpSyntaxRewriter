using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using SharpSyntaxRewriter.Rewriters;

namespace Tests
{
    [TestClass]
    public class TestEmplaceGlobalStatement : RewriterTester
    {
        protected override SyntaxTree ApplyRewrite(SyntaxTree tree, Compilation compilation)
        {
            return new EmplaceGlobalStatement().Apply(tree);
        }

        [TestMethod]
        public void TestEmplaceGlobalStatement1()
        {
            var original = @"
using System;
Console.Write(1);
";

            var expected = @"
using System;
internal static class __Program__ { private static void Main(string[] args) { Console.Write(1); } }
";

            CompileAsExecutable();
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestEmplaceGlobalStatement2()
        {
            var original = @"
using System;

Console.Write(1);
";

            var expected = @"
using System;

internal static class __Program__ { private static void Main(string[] args) { Console.Write(1); } }
";

            CompileAsExecutable();
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestEmplaceGlobalStatement3()
        {
            var original = @"
using System;

Console.Write(1);

Console.Write(2);
";

            var expected = @"
using System;

internal static class __Program__ { private static void Main(string[] args) { Console.Write(1);

Console.Write(2); } }
";

            CompileAsExecutable();
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestEmplaceGlobalStatement4()
        {
            var original = @"
using System;

Console.Write(args);
";

            var expected = @"
using System;

internal static class __Program__ { private static void Main(string[] args) { Console.Write(args);  } }
";

            CompileAsExecutable();
            TestRewrite_LinePreserve(original, expected);
        }
    }
}