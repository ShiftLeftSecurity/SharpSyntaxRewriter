using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpSyntaxRewriter.Rewriters;
using System;

namespace Tests
{
    [TestClass]
    public class TestExtractDeclarationFromPattern : RewriterTester
    {
        protected override SyntaxTree ApplyRewrite(SyntaxTree tree, Compilation compilation)
        {
            ExtractDeclarationFromPattern rw = new();
            return rw.Apply(tree, compilation.GetSemanticModel(tree));
        }

        [TestMethod]
        public void TestExtractDeclarationFromPattern1()
        {
            var original = @"
using System;

class CCC
{
    private void FFF(object ppp)
    {
        var vvv = ppp is DateTime ddd;
    }
}
";

            var expected = @"
using System;

class CCC
{
    private void FFF(object ppp)
    {
        DateTime ddd = default(DateTime); if (ppp is DateTime) ddd = (DateTime)ppp; var vvv = ppp is DateTime;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestExtractDeclarationFromPattern2()
        {
            var original = @"
using System;

class CCC
{
     public int Data;

     private void FFF(object ppp)
     {
         var vvv = new CCC
         {
             Data = ppp is DateTime ddd ? 0 : 1
         };
     }
 }
";

            var expected = @"
using System;

class CCC
{
    public int Data;

    private void FFF(object ppp)
    {
        DateTime ddd = default(DateTime); if (ppp is DateTime) ddd = (DateTime)ppp; var vvv = new CCC
        {
            Data = ppp is DateTime ? 0 : 1
        };
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestExtractDeclarationFromPattern3()
        {
            var original = @"
using System;

class CCC
{
    private void GGG(DateTime ppp) {}
    private void FFF(object ppp)
    {
        if (ppp is DateTime ddd)
            GGG(ddd);
    }
}
";

            var expected = @"
using System;

class CCC
{
    private void GGG(DateTime ppp) {}
    private void FFF(object ppp)
    {
        DateTime ddd = default(DateTime); if (ppp is DateTime) ddd = (DateTime)ppp; if (ppp is DateTime)
            GGG(ddd);
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestExtractDeclarationFromPattern4()
        {
            var original = @"
using System;

class CCC
{
    private void GGG(DateTime ppp) {}
    private void FFF(object ppp)
    {
        if (true)
            if (ppp is DateTime ddd)
                GGG(ddd);
    }
}
";

            var expected = @"
using System;

class CCC
{
    private void GGG(DateTime ppp) {}
    private void FFF(object ppp)
    {
        if (true)
        {   DateTime ddd = default(DateTime); if (ppp is DateTime) ddd = (DateTime)ppp; if (ppp is DateTime)
            GGG(ddd);   }
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestExtractDeclarationFromPattern5()
        {
            var original = @"
using System;

class CCC
{
    private void GGG(DateTime ppp) {}
    private void FFF(object ppp)
    {
        if (true)
        {
            if (ppp is DateTime ddd)
                GGG(ddd);
        }
    }
}
";

            var expected = @"
using System;

class CCC
{
    private void GGG(DateTime ppp) {}
    private void FFF(object ppp)
    {
        if (true)
        {
            DateTime ddd = default(DateTime); if (ppp is DateTime) ddd = (DateTime)ppp; if (ppp is DateTime)
            GGG(ddd);
        }
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestExtractDeclarationFromPattern6()
        {
            var original = @"
using System;

class CCC
{
    private void GGG(DateTime ppp) {}
    private void FFF(object ppp)
    {
        if (true && ppp is DateTime ddd)
            GGG(ddd);
    }
}
";

            var expected = @"
using System;

class CCC
{
    private void GGG(DateTime ppp) {}
    private void FFF(object ppp)
    {
        DateTime ddd = default(DateTime); if (ppp is DateTime) ddd = (DateTime)ppp; if (true && ppp is DateTime)
            GGG(ddd);
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestExtractDeclarationFromPattern7()
        {
            var original = @"
using System;

class CCC
{
     public int Data;
     private int GGG(DateTime ppp) { return 0; }
     private void FFF(object ppp)
     {
         Data = ppp is DateTime ddd ? GGG(ddd) : 1;
     }
 }
";

            var expected = @"
using System;

class CCC
{
    public int Data;
    private int GGG(DateTime ppp) { return 0; }
    private void FFF(object ppp)
    {
        DateTime ddd = default(DateTime); if (ppp is DateTime) ddd = (DateTime)ppp; Data = ppp is DateTime ? GGG(ddd) : 1;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestExtractDeclarationFromPattern8()
        {
            var original = @"
using System;

class CCC
{
     public int Data;
     private int GGG(DateTime ppp) { return 0; }
     private void FFF(object ppp)
     {
         var vvv = new CCC
         {
             Data = ppp is DateTime ddd ? GGG(ddd) : 1
         };
     }
 }
";

            var expected = @"
using System;

class CCC
{
    public int Data;
    private int GGG(DateTime ppp) { return 0; }
    private void FFF(object ppp)
    {
        DateTime ddd = default(DateTime); if (ppp is DateTime) ddd = (DateTime)ppp; var vvv = new CCC
        {
            Data = ppp is DateTime ? GGG(ddd) : 1
        };
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestExtractDeclarationFromPattern9()
        {
            var original = @"
class CCC
{
    private void FFF(object ppp)
    {
        var vvv = ppp is null;
    }
}
";

            var expected = @"
class CCC
{
    private void FFF(object ppp)
    {
        var vvv = ppp is null;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestExtractDeclarationFromPattern10()
        {
            var original = @"
using System;

class CCC
{
    static bool IsConferenceDay(DateTime date) => date is { Year: 2020, Month: 5, Day: 19 or 20 or 21 };
}
";

            var expected = @"
using System;

class CCC
{
    static bool IsConferenceDay(DateTime date) => date is { Year: 2020, Month: 5, Day: 19 or 20 or 21 };
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestExtractDeclarationFromPattern11()
        {
            var original = @"
using System;

class CCC
{
     private void GGG(DateTime? ppp) { }

     private void FFF(object ppp)
     {
         GGG(ppp is DateTime ddd ? ddd : null);
     }
 }
";

            var expected = @"
using System;

class CCC
{
    private void GGG(DateTime? ppp) { }

    private void FFF(object ppp)
    {
        DateTime ddd = default(DateTime); if (ppp is DateTime) ddd = (DateTime)ppp; GGG(ppp is DateTime ? ddd : null);
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }
    }
}

