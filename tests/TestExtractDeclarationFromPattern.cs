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
        DateTime ddd = (DateTime)ppp; var vvv = ppp is DateTime;
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
     public DateTime? Data;

     private void FFF(object ppp)
     {
         var vvv = new CCC
         {
             Data = ppp is DateTime ddd ? ddd : null
         };
     }
 }
";

            var expected = @"
using System;

class CCC
{
    public DateTime? Data;

    private void FFF(object ppp)
    {
        DateTime ddd = (DateTime)ppp; var vvv = new CCC
        {
            Data = ppp is DateTime ? ddd : null
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
        DateTime ddd = (DateTime)ppp; if (ppp is DateTime)
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
        {   DateTime ddd = (DateTime)ppp; if (ppp is DateTime)
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
            DateTime ddd = (DateTime)ppp; if (ppp is DateTime)
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
        DateTime ddd = (DateTime)ppp; if (true && ppp is DateTime)
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
        DateTime ddd = (DateTime)ppp; Data = ppp is DateTime ? GGG(ddd) : 1;
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
        DateTime ddd = (DateTime)ppp; var vvv = new CCC
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
        DateTime ddd = (DateTime)ppp; GGG(ppp is DateTime ? ddd : null);
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestExtractDeclarationFromPattern12()
        {
            var original = @"
using System;

class CCC
{
     private int FFF(object ppp)
     {
         return ppp switch
         {
             DateTime ddd => 1,
             _ => 0,
         };
     }
 }
";

            var expected = @"
using System;

class CCC
{
     private int FFF(object ppp)
     {
         DateTime ddd = (DateTime)ppp; return ppp switch
         {
             DateTime => 1,
             _ => 0,
         };
     }
 }
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestExtractDeclarationFromPattern13()
        {
            var original = @"
using System;

class CCC
{
     private int FFF(object ppp)
     {
         return ppp switch
         {
             DateTime => 1,
             _ => 0,
         };
     }
 }
";

            var expected = @"
using System;

class CCC
{
     private int FFF(object ppp)
     {
         return ppp switch
         {
             DateTime => 1,
             _ => 0,
         };
     }
 }
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestExtractDeclarationFromPattern14()
        {
            var original = @"
using System;

class CCC
{
     private int FFF(object ppp)
     {
         return ppp switch
         {
             DateTime _ => 1,
             null => 0,
         };
     }
 }
";

            var expected = @"
using System;

class CCC
{
     private int FFF(object ppp)
     {
         return ppp switch
         {
             DateTime _ => 1,
             null => 0,
         };
     }
 }
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestExtractDeclarationFromPattern15()
        {
            var original = @"
using System;

class CCC
{
     private int FFF(object ppp)
     {
         return ppp switch
         {
             DateTime ddd => 1,
             string sss => 1,
             _ => 0,
         };
     }
 }
";

            var expected = @"
using System;

class CCC
{
     private int FFF(object ppp)
     {
         DateTime ddd = (DateTime)ppp; string sss = (string)ppp; return ppp switch
         {
             DateTime => 1,
             string => 1,
             _ => 0,
         };
     }
 }
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestExtractDeclarationFromPattern16()
        {
            var original = @"
using System;

class CCC
{
     private int FFF(object ppp)
     {
         return ppp switch
         {
             DateTime uuu => 1,
             string uuu => 1,
             _ => 0,
         };
     }
 }
";

            var expected = @"
using System;

class CCC
{
     private int FFF(object ppp)
     {
         return ppp switch
         {
             DateTime => 1,
             string => 1,
             _ => 0,
         };
     }
 }
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestExtractDeclarationFromPattern17()
        {
            var original = @"
using System;

class CCC
{
    public DateTime? DDD;
    public string SSS;

    private void FFF(object ppp)
    {
        var vvv = new CCC
        {
            DDD = ppp is DateTime ddd ? ddd : null,
            SSS = ppp is string sss ? sss : "" ""
        };
    }
}
";

            var expected = @"
using System;

class CCC
{
    public DateTime? DDD;
    public string SSS;

    private void FFF(object ppp)
    {
        DateTime ddd = (DateTime)ppp; string sss = (string)ppp; var vvv = new CCC
        {
            DDD = ppp is DateTime ? ddd : null,
            SSS = ppp is string ? sss : "" ""
        };
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }
    }
}

