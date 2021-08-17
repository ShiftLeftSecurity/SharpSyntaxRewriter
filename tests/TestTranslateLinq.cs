using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using SharpSyntaxRewriter.Rewriters;

namespace Tests
{
    [TestClass]
    public class TestTranslateLinq : RewriterTester
    {
        protected override SyntaxTree ApplyRewrite(SyntaxTree tree, Compilation compilation)
        {
            return new TranslateLinq().Apply(tree);
        }

        private readonly string _mainSource =
@"
using System;
using System.Linq;
using System.Collections.Generic;

class Detail
{
    public int UnitPrice { get; set; }
    public int Quantity { get; set; }
    public int OrderID { get; set; }
    public int ProductID { get; set; }
}

class Order
{
    public int CustomerID { get; set; }
    public int OrderID { get; set; }
    public double Total { get; set; }
    public DateTime OrderDate { get; set; }
    public List<Detail> Details { get; set; }
}

class Customer
{
    public int CustomerID { get; set; }
    public string City { get; set; }
    public string Name { get; set; }
    public int Country { get; set; }
    public int Key { get; set; }
    public int Age { get; set; }
    public List<Order> Orders { get; set; }
}

class Product
{
    public int ProductID { get; set; }
    public string ProductName { get; set; }
}

class App
{
    static void Main()
    {
        var customers = new Customer[10];
        var orders = new Order[1];
        var details = new Detail[1];
        var products = new Product[10];
        var query = MARKER ;
    }
}
";

        private string SurroundLINQ(string text)
        {
            return _mainSource.Replace("MARKER", text);
        }

        [TestMethod]
        public void TestTranslateLinqQueryContinuation()
        {
            var original = @"
from c in customers
group c by c.Country into g
select new { Country = g.Key, CustCount = g.Count() }
";

            var expected = @"
(customers.
GroupBy(c => c.Country)).
Select(g => new { Country = g.Key, CustCount = g.Count() })
";

            TestRewrite_LineIgnore(SurroundLINQ(original),
                                   SurroundLINQ(expected));
        }

        [TestMethod]
        public void TestTranslateLinqGroupBy()
        {
            var original = @"
from c in customers
group c.Name by c.Country
";

            var expected = @"
customers.
GroupBy(c => c.Country, c => c.Name)
";

            TestRewrite_LineIgnore(SurroundLINQ(original),
                                   SurroundLINQ(expected));
        }

        [TestMethod]
        public void TestTranslateLinqSelect()
        {
            var original = @"
from c in customers.Where(c => c.City == ""London"")
select c
";

            var expected = @"
customers.Where(c => c.City == ""London"")
";

            TestRewrite_LineIgnore(SurroundLINQ(original),
                                   SurroundLINQ(expected));
        }

        [TestMethod]
        public void TestTranslateLinqMemberSelect()
        {
            var original = @"
using System;
using System.Linq;
using System.Collections.Generic;

class Thing
{
    public int Whatever = 10;
}

class Entity
{
    public List<Thing> Things = new List<Thing>();
}

class C
{
    void f()
    {
        var entities = new Entity();
        var all = from a in entities.Things select a;
    }
}
";

            var expected = @"
using System;
using System.Linq;
using System.Collections.Generic;

class Thing
{
    public int Whatever = 10;
}

class Entity
{
    public List<Thing> Things = new List<Thing>();
}

class C
{
    void f()
    {
        var entities = new Entity();
        var all = entities.Things;
    }
}
";

            TestRewrite_LineIgnore(original, expected);
        }


        [TestMethod]
        public void TestTranslateLinqDegenerateCase()
        {
            var original = @"
from c in customers
select c
";

            var expected = @"
customers.Select(c => c)
";

            TestRewrite_LineIgnore(SurroundLINQ(original),
                                   SurroundLINQ(expected));
        }

        [TestMethod]
        public void TestTranslateLinqDoubleFromAndSelect()
        {
            var original = @"
from c in customers
from o in c.Orders
select new { c.Name, o.OrderID, o.Total }
";

            var expected = @"
customers.
SelectMany(c => c.Orders, (c,o) => new { c.Name, o.OrderID, o.Total })
";

            TestRewrite_LineIgnore(SurroundLINQ(original),
                                   SurroundLINQ(expected));
        }

        [TestMethod]
        public void TestTranslateLinqWhere()
        {
            var original = @"
from c in customers
where (c.Name == ""abc"" || c.Name == ""xyz"")
group c by c.Name
";

            var expected = @"
customers.
Where(c => (c.Name == ""abc"" || c.Name == ""xyz"")).
GroupBy(c => c.Name)
";

            TestRewrite_LineIgnore(SurroundLINQ(original),
                                   SurroundLINQ(expected));
        }

        [TestMethod]
        public void TestTranslateLinqOrderBy()
        {
            var original = @"
from c in customers
orderby c.Key, c.Country, c.Age
group c by c.Name
";

            var expected = @"
customers.
OrderBy(c=>c.Key).ThenBy(c=>c.Country).ThenBy(c=>c.Age).
GroupBy(c=>c.Name)
";

            TestRewrite_LineIgnore(SurroundLINQ(original),
                                   SurroundLINQ(expected));
        }

        [TestMethod]
        public void TestTranslateLinqJoinWithoutIntoAndSelect()
        {
            var original = @"
from c in customers
join o in orders on c.CustomerID equals o.CustomerID
select new { c.Name, o.OrderDate, o.Total }
";

            var expected = @"
customers.Join(orders, c => c.CustomerID, o => o.CustomerID,
    (c, o) => new { c.Name, o.OrderDate, o.Total })
";

            TestRewrite_LineIgnore(SurroundLINQ(original),
                                   SurroundLINQ(expected));
        }

        [TestMethod]
        public void TestTranslateLinqJoinIntoAndSelect()
        {}

        [TestMethod]
        public void TestTranslateLinqLet()
        {
            var original = @"
from o in orders
let t = o.Details.Sum(d => d.UnitPrice * d.Quantity)
where t >= 1000
select new { o.OrderID, Total = t }
";

            var expected = @"
orders.
Select(o => new { o, t = o.Details.Sum(d => d.UnitPrice * d.Quantity) }).
Where(____TRANSPARENT0 => ____TRANSPARENT0.t >= 1000).
Select(____TRANSPARENT0 => new { ____TRANSPARENT0.o.OrderID, Total = ____TRANSPARENT0.t })
";

            TestRewrite_LineIgnore(SurroundLINQ(original),
                                   SurroundLINQ(expected));
        }

        [TestMethod]
        public void TestTranslateLinqDoubleFromAndNonSelect()
        {
            var original = @"
from c in customers
from o in c.Orders
orderby o.Total descending
select new { c.Name, o.OrderID, o.Total }
";

            var expected = @"
customers.
SelectMany(c => c.Orders, (c,o) => new{ c ,o }).
OrderByDescending(____TRANSPARENT0 => ____TRANSPARENT0.o.Total).
Select(____TRANSPARENT0 => new { ____TRANSPARENT0.c.Name, ____TRANSPARENT0.o.OrderID, ____TRANSPARENT0.o.Total })
";

            TestRewrite_LineIgnore(SurroundLINQ(original),
                                   SurroundLINQ(expected));
        }

        [TestMethod]
        public void TestTranslateLinqFromAndJoinAndNonSelect()
        {
            var original = @"
from c in customers
join o in orders on c.CustomerID equals o.CustomerID into co
let n = co.Count()
where n >= 10
select new { c.Name, OrderCount = n }
";

            var expected = @"

customers.
GroupJoin(orders, c => c.CustomerID, o => o.CustomerID,
    (c,co) => new { c, co}).
Select(____TRANSPARENT0 => new {____TRANSPARENT0, n = ____TRANSPARENT0.co.Count() }).
Where(____TRANSPARENT1 => ____TRANSPARENT1.n >= 10).
Select(____TRANSPARENT1 => new {____TRANSPARENT1.____TRANSPARENT0.c.Name, OrderCount = ____TRANSPARENT1.n})
";

            TestRewrite_LineIgnore(SurroundLINQ(original),
                                   SurroundLINQ(expected));
        }

        [TestMethod]
        public void TestTranslateLinqTypedRangeVariable()
        {
            var original = @"
from Customer c in customers
where c.City == ""London""
select c
";

            var expected = @"
customers.
Cast<Customer>().Where(c=>c.City == ""London"").
Select(c => c)
";

            // BUG: generate case only applies on the top query expression.
            TestRewrite_LineIgnore(SurroundLINQ(original),
                                   SurroundLINQ(expected));
        }

        [TestMethod]
        public void TestTranslateLinqMultiTransparentIdentifiers()
        {
            var original = @"
from c in customers
join o in orders on c.CustomerID equals o.CustomerID
join d in details on o.OrderID equals d.OrderID
join p in products on d.ProductID equals p.ProductID
select new { c.Name, o.OrderDate, p.ProductName }
";

            var expected = @"
customers.
Join(orders, c => c.CustomerID, o => o.CustomerID,
    (c, o) => new { c, o }).
Join(details, ____TRANSPARENT0 => ____TRANSPARENT0.o.OrderID, d => d.OrderID,
    (____TRANSPARENT0, d) => new { ____TRANSPARENT0, d }).
Join(products, ____TRANSPARENT1 => ____TRANSPARENT1.d.ProductID, p => p.ProductID,
    (____TRANSPARENT1, p) => new { ____TRANSPARENT1, p }).
Select(____TRANSPARENT2 => new { ____TRANSPARENT2.____TRANSPARENT1.____TRANSPARENT0.c.Name,
                                 ____TRANSPARENT2.____TRANSPARENT1.____TRANSPARENT0.o.OrderDate,
                                 ____TRANSPARENT2.p.ProductName })
";

            TestRewrite_LineIgnore(SurroundLINQ(original),
                                   SurroundLINQ(expected));
        }

        [TestMethod]
        public void TestTranslateLinqMultiTransparentIdentifiers2()
        {
            var original = @"
from c in customers
from o in c.Orders
orderby o.Total descending
select new { c.Name, o.Total }
";

            var expected = @"
customers.
SelectMany(c=>c.Orders,
    (c,o)=>new{c,o}).OrderByDescending(____TRANSPARENT0=>____TRANSPARENT0.o.Total ).
Select(____TRANSPARENT0=>new { ____TRANSPARENT0.c.Name,____TRANSPARENT0.o.Total })
";

            TestRewrite_LineIgnore(SurroundLINQ(original),
                                   SurroundLINQ(expected));
        }

        [TestMethod]
        public void TestTranslateLinqQueryExpressionRemoveOuter()
        {
            var original = @"
using System;
using System.Linq;
using System.Collections.Generic;

namespace ns {
class Customer
{
    public int Country { get; set; }
    public int Key { get; set; }
}

class App
{
    static void Main()
    {
        var customers = new Customer[10];
        var collection =
            from c in customers
            group c by c.Country into g
            select new { Country = g.Key, CustCount = g.Count() };
    }
}
}
";
            var expected = @"
using System;
using System.Linq;
using System.Collections.Generic;

namespace ns {
class Customer
{
    public int Country { get; set; }
    public int Key { get; set; }
}

class App
{
    static void Main()
    {
        var customers = new Customer[10];
        var collection = (customers.GroupBy(c=>c.Country)).Select(g=>new{Country=g.Key,CustCount=g.Count()});
    }
}
}";

            TestRewrite_LineIgnore(original, expected);
        }

        [TestMethod]
        public void TestTranslateLinqNestedJoinWithWhereOnAggregator()
        {
            var original = @"
using System;
using System.Linq;
using System.Collections.Generic;

class Customer
{
    public int Id { get; set; }
}

class App
{
    static void Main()
    {
        var customers = new List<Customer>();
        var data =
            from c in customers
            join ids in customers on c.Id equals 1 into whatever
            from w in whatever
            where w.Id == 33
            select w;
    }
}
            ";

            var expected = @"
using System;
using System.Linq;
using System.Collections.Generic;

class Customer
{
    public int Id { get; set; }
}

class App
{
    static void Main()
    {
        var customers = new List<Customer>();
        var data = customers
            .GroupJoin(customers,c=>c.Id,ids=>1,(c,whatever)=>new{c,whatever})
            .SelectMany(____TRANSPARENT0=>____TRANSPARENT0.whatever,(____TRANSPARENT0,w)=>new{____TRANSPARENT0,w})
            .Where(____TRANSPARENT1=>____TRANSPARENT1.w.Id==33);
    }
}
            ";

            TestRewrite_LineIgnore(original, expected);
        }

        [TestMethod]
        public void TestTranslateLinqNestedJoinWithWhereOnAggregatorMemberAccess()
        {
            var original = @"
using System;
using System.Linq;
using System.Collections.Generic;

class Customer
{
    public int Id { get; set; }
}

class App
{
    static void Main()
    {
        var customers = new List<Customer>();
        var data =
            from c in customers
            join ids in customers on c.Id equals 1 into whatever
            from w in whatever.DefaultIfEmpty()
            where w.Id == 33
            select w;
    }
}
            ";

            var expected = @"
using System;
using System.Linq;
using System.Collections.Generic;

class Customer
{
    public int Id { get; set; }
}

class App
{
    static void Main()
    {
        var customers = new List<Customer>();
        var data = customers
            .GroupJoin(customers,c=>c.Id,ids=>1,(c,whatever)=>new{c,whatever})
            .SelectMany(____TRANSPARENT0=>____TRANSPARENT0.whatever.DefaultIfEmpty(),(____TRANSPARENT0,w)=>new{____TRANSPARENT0,w})
            .Where(____TRANSPARENT1=>____TRANSPARENT1.w.Id==33);
    }
}
            ";

            TestRewrite_LineIgnore(original, expected);
        }

        [TestMethod]
        public void TestTranslateLinqNestedJoinWithDifferentCollections()
        {
            var original = @"
using System;
using System.Linq;
using System.Collections.Generic;

class Customer
{
    public int Id { get; set; }
}

class Stuff
{
    public int Number { get; set; }
}


class App
{
    static void Main()
    {
        var customers = new List<Customer>();
        var things = new List<Stuff>();

        var data =
            from c in customers
            join stuff in things on c.Id equals stuff.Number into whatever
            from w in whatever.DefaultIfEmpty()
            where w.Number == 33
            select w;
    }
}
            ";

            var expected = @"
using System;
using System.Linq;
using System.Collections.Generic;

class Customer
{
    public int Id { get; set; }
}

class Stuff
{
    public int Number { get; set; }
}

class App
{
    static void Main()
    {
        var customers = new List<Customer>();
        var things = new List<Stuff>();

        var data = customers
            .GroupJoin(things ,c=>c.Id ,stuff=>stuff.Number ,(c,whatever)=>new{c,whatever})
            .SelectMany(____TRANSPARENT0=>____TRANSPARENT0.whatever.DefaultIfEmpty(),(____TRANSPARENT0,w)=>new{____TRANSPARENT0,w})
            .Where(____TRANSPARENT1=>____TRANSPARENT1.w.Number == 33);
    }
}
            ";

            TestRewrite_LineIgnore(original, expected);
        }
    }
}
