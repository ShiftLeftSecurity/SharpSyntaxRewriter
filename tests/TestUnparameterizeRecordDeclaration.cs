using System;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using SharpSyntaxRewriter.Rewriters;
namespace Tests
{
    [TestClass]
    public class TestUnparameterizeRecordDeclaration : RewriterTester
    {
        protected override SyntaxTree ApplyRewrite(SyntaxTree tree, Compilation compilation)
        {
            UnparameterizeRecordDeclaration rw = new();
            return rw.Apply(tree);
        }

        [TestMethod]
        public void TestUnparameterizeRecordDeclarationWithParameterNoBody()
        {
            var original = @"
public record Person(string FirstName);
";

            var expected = @"
public record Person { public Person(string FirstName) {} public string FirstName { get; init;} }
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestUnparameterizeRecordDeclarationWithParameterBodyEmptyOneLine()
        {
            var original = @"
public record Person(string FirstName) {}
";

            var expected = @"
public record Person { public Person(string FirstName) {} public string FirstName { get; init; } }
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestUnparameterizeRecordDeclarationWithParameterBodyEmpty()
        {
            var original = @"
public record Person(string FirstName)
{
}
";

            var expected = @"
public record Person
{
    public Person(string FirstName) {} public string FirstName { get; init;} }
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestUnparameterizeRecordDeclarationWithParameterBodyWithExistingMember()
        {
            var original = @"
public record Person(string FirstName)
{
    public string[] PhoneNumbers { get; init; }
}
";

            var expected = @"
public record Person
{
    public string[] PhoneNumbers { get; init; } public Person(string FirstName){} public string FirstName { get; init;}
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestUnparameterizeRecordDeclarationWithParameterBodyWithExistingMemberAndCtor()
        {
            var original = @"
public record Person
{
    public Person(string FirstName){}
    public string[] PhoneNumbers { get; init; }
}
";

            var expected = @"
public record Person
{
    public Person(string FirstName){}
    public string[] PhoneNumbers { get; init; }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestUnparameterizeRecordDeclarationWithAbstractModifier()
        {
            var original = @"
public abstract record Person(string FirstName);
";

            var expected = @"
public abstract record Person(string FirstName);
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestUnparameterizeRecordDeclarationInheritingFromAbstractRecord()
        {
            var original = @"
public abstract record Person(string FirstName);
public record Teacher(string FirstName, int Grade) : Person(FirstName);
";

            var expected = @"
public abstract record Person(string FirstName);
public record Teacher : Person { public Teacher (string FirstName, int Grade) : base(FirstName) {} public string FirstName { get; init;} public int Grade { get; init;} }
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestUnparameterizeRecordDeclarationInheritingFromAbstractRecordNoParameter()
        {
            var original = @"
public abstract record Person();
public record Teacher(string FirstName) : Person();
";

            var expected = @"
public abstract record Person();
public record Teacher : Person { public Teacher (string FirstName) : base() {} public string FirstName { get; init;} }
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestUnparameterizeRecordDeclarationInheritingFromAbstractRecordNoParameter2()
        {
            var original = @"
public abstract record Person();
public record Teacher(string FirstName)
    : Person();
";

            var expected = @"
public abstract record Person();
public record Teacher
    : Person { public Teacher (string FirstName) : base() {} public string FirstName { get; init;} }
";

            TestRewrite_LinePreserve(original, expected);
        }
    }
}
