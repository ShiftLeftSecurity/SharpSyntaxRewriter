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
public record Person { public string FirstName { get; init; } public Person(string FirstName) {this.FirstName=FirstName;} }
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestUnparameterizeRecordStructDeclarationWithParameterNoBody()
        {
            var original = @"
public record struct C (int X, string S);
";

            var expected = @"
public record struct C { public int X {get;init;} public string S {get;init;} public C(in tX,string S){this.X=X;this.S=S;}}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestUnparameterizeRecordStructDeclarationWithParameterNoBodyReadonly()
        {
            var original = @"
public readonly record struct Point(double X, double Y, double Z);
";

            var expected = @"
public record struct Point { public double X {get;init;} public double Y{get;init;} public double Z {get;init;} public Point(double X,double Y,double Z){this.X=X;this.Y=Y;this.Z=Z;}}
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
public record Person { public string FirstName { get; init; } public Person(string FirstName) {this.FirstName=FirstName;} }
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
    public string FirstName { get; init;} public Person(string FirstName) {this.FirstName=FirstName;} }
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
    public string FirstName { get; init;} public Person(string FirstName){this.FirstName=FirstName;}public string[] PhoneNumbers { get; init; } 
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestUnparameterizeRecordStructDeclarationWithParameterBodyWithExistingMember()
        {
            var original = @"
public record struct Person(string FirstName)
{
    public string[] PhoneNumbers { get; init; } = default;
}
";

            var expected = @"
public record struct Person
{
    public string FirstName { get; init;} public Person(string FirstName){this.FirstName=FirstName;}public string[] PhoneNumbers { get; init; } = default;
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
public record Teacher : Person { public string FirstName { get; init;} public int Grade { get; init;} public Teacher (string FirstName, int Grade) : base(FirstName) {this.FirstName=FirstName;this.Grade=Grade;} }
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
public record Teacher : Person {  public string FirstName { get; init;} public Teacher (string FirstName) : base() {this.FirstName=FirstName;}}
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
    : Person { public string FirstName { get; init;}  public Teacher (string FirstName) : base() {this.FirstName=FirstName;} }
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestUnparameterizeRecordDeclarationEmptyParameterWithNonEmptyParameterAbstractBase()
        {
            var original = @"
public abstract record Person(int ppp);
public record Teacher()
    : Person(1);
";

            var expected = @"
public abstract record Person(int ppp);
public record Teacher
    : Person { public Teacher() : base(1) {} }
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestUnparameterizeRecordDeclarationEmptyParameterWithNonEmptyParameterBase()
        {
            var original = @"
public record Person(int ppp);
public record Teacher()
    : Person(1);
";

            var expected = @"
public record Person { public int ppp {get;init;} public Person(int ppp) { this.ppp=ppp; }}
public record Teacher
    : Person { public Teacher() : base(1) {} }
";

            TestRewrite_LinePreserve(original, expected);
        }

    }
}
