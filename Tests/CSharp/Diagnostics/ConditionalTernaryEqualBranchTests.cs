using NUnit.Framework;
using RefactoringEssentials.CSharp.Diagnostics;

namespace RefactoringEssentials.Tests.CSharp.Diagnostics
{
    [TestFixture]
    public class ConditionalTernaryEqualBranchTests : InspectionActionTestBase
    {
        [Test]
        public void TestInspectorCase1()
        {
            Analyze<ConditionalTernaryEqualBranchAnalyzer>(@"class Foo
{
	void Bar (string str)
	{
		string c = $str != null ? ""default"" : ""default""$;
	}
}", @"class Foo
{
	void Bar (string str)
	{
		string c = ""default"";
	}
}");

        }

        [Test]
        public void TestMoreComplexBranch()
        {
            Analyze<ConditionalTernaryEqualBranchAnalyzer>(@"class Foo
{
	void Bar (string str)
	{
		var c = $str != null ? 3 + (3 * 4) - 12 * str.Length : 3 + (3 * 4) - 12 * str.Length$;
	}
}", @"class Foo
{
	void Bar (string str)
	{
		var c = 3 + (3 * 4) - 12 * str.Length;
	}
}");

        }

        [Test]
        public void TestNotEqualBranches()
        {
            Analyze<ConditionalTernaryEqualBranchAnalyzer>(@"class Foo
{
	void Bar (string str)
	{
		string c = str != null ? ""default"" : ""default2"";
	}
}");

        }

        [Test]
        public void TestDisable()
        {
            Analyze<ConditionalTernaryEqualBranchAnalyzer>(@"class Foo
{
	void Bar (string str)
	{
#pragma warning disable " + DiagnosticIDs.ConditionalTernaryEqualBranchAnalyzerID + @"
		string c = str != null ? ""default"" : ""default"";
	}
}");

        }

        [Test]
        public void Test()
        {
            Analyze<ConditionalTernaryEqualBranchAnalyzer>(@"
class TestClass
{
	void TestMethod (int i)
	{
		var a = $i > 0 ? 1 + 1 : 1 + 1$;
		var b = i > 1 ? 1 : 2;
	}
}", @"
class TestClass
{
	void TestMethod (int i)
	{
		var a = 1 + 1;
		var b = i > 1 ? 1 : 2;
	}
}");
        }
    }
}

