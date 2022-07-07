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
        public void TestExtractDeclarationFromPatternAsVarInitializer()
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
        public void TestExtractDeclarationFromPatternAsObjectInitializer()
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
        public void TestExtractDeclarationFromPatternInIfStatement()
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
        public void TestExtractDeclarationFromPatternInIfStatementNested()
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
        public void TestExtractDeclarationFromPatternInIfStatementNestedWithBlock()
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
        public void TestExtractDeclarationFromPatternInIfStatmentWithLogicalAND()
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
        public void TestExtractDeclarationFromPatternInConditionalExpression()
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
        public void TestExtractDeclarationFromPatternInConditionalExpressionWithinObjectInitializer()
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
        public void TestExtractDeclarationFromPatternInConditionalExpressionWithinObjectInitializerMultipleProperties()
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

        [TestMethod]
        public void TestExtractDeclarationFromPatternKeepNullPattern()
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
        public void TestExtractDeclarationFromPatternKeepRangePattern()
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
        public void TestExtractDeclarationFromPatternInConditionalExpressionAsArgument()
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
        public void TestExtractDeclarationFromPatternInSwitchExpression()
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
         object ddd = (object)ppp; return ppp switch
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
        public void TestExtractDeclarationFromPatternKeepSwitchArmWithoutDeclarationPattern()
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
        public void TestExtractDeclarationFromPatternKeepUnderscorePattern()
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
        public void TestExtractDeclarationFromPatternInSwitchExpressionWith2Arms()
        {
            var original = @"
using System;

class CCC
{
    private int DDD(DateTime ddd) { return 0; }
    private int SSS(string sss) { return 0; }
    private int FFF(object ppp)
    {
        return ppp switch
        {
            DateTime ddd => DDD(ddd),
            string sss => SSS(sss),
            _ => 0,
        };
    }
 }
";

            var expected = @"
using System;

class CCC
{
    private int DDD(DateTime ddd) { return 0; }
    private int SSS(string sss) { return 0; }
    private int FFF(object ppp)
    {
        object ddd = (object)ppp; object sss = (object)ppp; return ppp switch
        {
            DateTime => DDD((DateTime)ddd),
            string => SSS((string)sss),
            _ => 0,
        };
    }
 }
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestExtractDeclarationFromPatternInSwitchExpressionWith2ArmsButDeclarationWithSameName()
        {
            var original = @"
using System;

class CCC
{
    private int DDD(DateTime ddd) { return 0; }
    private int SSS(string sss) { return 0; }
    private int FFF(object ppp)
    {
        return ppp switch
        {
            DateTime uuu => DDD(uuu),
            string uuu => SSS(uuu),
            _ => 0,
        };
    }
 }
";

            var expected = @"
using System;

class CCC
{
    private int DDD(DateTime ddd) { return 0; }
    private int SSS(string sss) { return 0; }
    private int FFF(object ppp)
    {
        object uuu = (object)ppp; return ppp switch
        {
            DateTime => DDD((DateTime)uuu),
            string => SSS((string)uuu),
            _ => 0,
        };
    }
 }
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestExtractDeclarationFromPatternInSwitchExpressionWith2ArmsButDeclarationWithSameNameThroughRecursivePattern()
        {
            var original = @"
using System;

public class CCC
{
    string FFF(object ppp)
    {
        return ppp switch
        {
            string { Length: >= 5 } sss => sss.Substring(0, 5),
            string sss => sss,
        };
    }
}
";

            var expected = @"
using System;

public class CCC
{
    string FFF(object ppp)
    {
        object sss = (object)ppp; return ppp switch
        {
            string { Length: >= 5 } => (string)sss.Substring(0, 5),
            string => (string)sss,
        };
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

    }
}

