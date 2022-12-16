using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpSyntaxRewriter.Rewriters;

namespace Tests
{
    [TestClass]
    public class TestRenameFileScopedType : RewriterTester
    {
        protected override SyntaxTree ApplyRewrite(SyntaxTree tree, Compilation compilation)
        {
            var rw = new RenameFileScopedType();
            return rw.Apply(tree, compilation.GetSemanticModel(tree));
        }

        [DataTestMethod]
        [DataRow("class")]
        [DataRow("interface")]
        [DataRow("enum")]
        [DataRow("struct")]
        [DataRow("record")]
        public void TestEmptyFileScopedTypeDeclaration(string declKind)
        {
            var original = $@"
using System;
file {declKind} C
{{
}}";

            var expected = $@"
using System;
internal {declKind} __FE3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855__C
{{
}}";
            TestRewrite_LinePreserve(original, expected);
        }

        [DataTestMethod]
        [DataRow("class")]
        [DataRow("interface")]
        [DataRow("struct")]
        [DataRow("record")]
        public void TestEmptyGenericFileScopedTypeDeclaration(string declKind)
        {
            var original = $@"
using System.Collections;
file {declKind} C<T> where T : IEnumerable
{{
}}";

            var expected = $@"
using System.Collections;
internal {declKind} __FE3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855__C_1<T> where T : IEnumerable
{{
}}";
            TestRewrite_LinePreserve(original, expected);
        }


        [DataTestMethod]
        [DataRow("class")]
        [DataRow("interface")]
        [DataRow("struct")]
        [DataRow("record")]
        public void TestFileScopedTypeDeclarationWithMemberMethodReturningItsType(string declKind)
        {
            var original = $@"
file {declKind} C
{{
    public static C Create()
    {{
        return default;
    }}
}}";

            var expected = $@"
internal {declKind} __FE3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855__C
{{
    public static __FE3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855__C Create()
    {{
        return default;
    }}
}}";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestObjectCreationOfFileScopedClassDeclaration()
        {
            var original = @"
file class C
{
}

public class Program
{
    void M()
    {
        var x = new C();
    }
}";

            var expected = @"
internal class __FE3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855__C
{
}

public class Program
{
    void M()
    {
        var x = new __FE3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855__C();
    }
}";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestClassDeclarationExtendingFileScopedInterface()
        {
            var original = @"
file interface I
{
}
public class C : I
{
}
";
            var expected = @"
internal interface __FE3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855__I
{
}
public class C : __FE3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855__I
{
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestOccurenceOfFileScopedTypeAsTypeParameter()
        {
            var original = @"
using System.Collections.Generic;
file interface I
{
}
file class Program
{
    private List<I> ListField;
}
";
            var expected = @"
using System.Collections.Generic;
internal interface __FE3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855__I
{
}
internal class __FE3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855__Program
{
    private List<__FE3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855__I> ListField;
}
";
            TestRewrite_LinePreserve(original, expected);


        }

        [DataTestMethod]
        [DataRow("class")]
        [DataRow("interface")]
        [DataRow("struct")]
        [DataRow("record")]
        [DataRow("enum")]
        public void TestOccurrenceOfFileScopedTypeInFieldDeclaration(string declKind)
        {
            var original = $@"
file {declKind} I
{{
}}
file class Program
{{
    private I SomeField;
}}
";
            var expected = $@"
internal {declKind} __FE3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855__I
{{
}}
internal class __FE3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855__Program
{{
    private __FE3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855__I SomeField;
}}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [DataTestMethod]
        [DataRow("class")]
        [DataRow("struct")]
        [DataRow("record")]
        [DataRow("enum")]
        [DataRow("interface")]
        public void TestOccurrenceOfFileScopedTypeInCastingExpression(string declKind)
        {
            var original = $@"
file {declKind} I
{{
}}
file class Program
{{
    void N(I arg)
    {{
        System.Console.WriteLine(arg.ToString());
    }}

    void M()
    {{
        N((I) default);
    }}
}}";

            var expected = $@"
internal {declKind} __FE3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855__I
{{
}}
internal class __FE3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855__Program
{{
    void N(__FE3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855__I arg)
    {{
        System.Console.WriteLine(arg.ToString());
    }}

    void M()
    {{
        N((__FE3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855__I) default);
    }}
}}";
            
            TestRewrite_LinePreserve(original, expected);
        }
    }
}
