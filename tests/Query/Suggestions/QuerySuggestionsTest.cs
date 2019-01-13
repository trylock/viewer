using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Viewer.Data;
using Viewer.IO;
using Viewer.Query;
using Viewer.Query.Suggestions;
using Viewer.Query.Suggestions.Providers;
using Viewer.QueryRuntime;

namespace ViewerTest.Query.Suggestions
{
    [TestClass]
    public class QuerySuggestionsTest
    {
        private Mock<IRuntime> _runtime;
        private Mock<IFileFinder> _fileFinder;
        private Mock<IFileSystem> _fileSystem;
        private Mock<IAttributeCache> _attributeCache;
        private Mock<IQueryViewRepository> _views;
        private QuerySuggestions _suggestions;

        [TestInitialize]
        public void Setup()
        {
            _runtime = new Mock<IRuntime>();
            _fileFinder = new Mock<IFileFinder>();
            _fileSystem = new Mock<IFileSystem>();
            _views = new Mock<IQueryViewRepository>();
            _attributeCache = new Mock<IAttributeCache>();

            _runtime
                .Setup(mock => mock.Functions)
                .Returns(new List<IFunction>
                {
                    new DateTimeFunction()
                });

            _suggestions = new QuerySuggestions(new ISuggestionProviderFactory[]
            {
                new ViewSuggestionProviderFactory(_views.Object),
                new AttributeNameSuggestionProviderFactory(_attributeCache.Object),
                new AttributeValueSuggestionProviderFactory(_attributeCache.Object),
                new DirectorySuggestionProviderFactory(_fileSystem.Object),
                new FunctionSuggestionProviderFactory(_runtime.Object), 
            }, new StateCollectorFactory());
        }

        private List<IQuerySuggestion> ComputeSuggestions(string queryPrefix)
        {
            return _suggestions.Compute(queryPrefix, queryPrefix.Length).ToList();
        }

        private List<IQuerySuggestion> ComputeSuggestions(string queryPrefix, int position)
        {
            Assert.IsTrue(position >= 0);
            Assert.IsTrue(position <= queryPrefix.Length);
            return _suggestions.Compute(queryPrefix, position).ToList();
        }

        private bool ContainsSuggestion(
            IEnumerable<IQuerySuggestion> suggestions, string transformedQuery)
        {
            return suggestions
                .Select(suggestion => suggestion.Apply())
                .Any(actual => actual.Query == transformedQuery);
        }

        [TestMethod]
        public void Compute_EmptyInput()
        {
            const string query = "";

            var suggestions = ComputeSuggestions(query);

            Assert.AreEqual(1, suggestions.Count);
            Assert.IsTrue(ContainsSuggestion(suggestions, "select"));
        }

        [TestMethod]
        public void Compute_SyntaxErrorBeforeCaret()
        {
            var suggestions = ComputeSuggestions("select 1 + 2 ");

            Assert.AreEqual(0, suggestions.Count);
        }

        [TestMethod]
        public void Compute_PartialKeywordAtTheStart()
        {
            const string query = "sel";

            var suggestions = ComputeSuggestions(query);

            Assert.AreEqual(1, suggestions.Count);
            Assert.IsTrue(ContainsSuggestion(suggestions, "select"));
        }

        [TestMethod]
        public void Compute_WrongPartialKeywordAtTheStart()
        {
            const string query = "wher";

            var suggestions = ComputeSuggestions(query);

            Assert.AreEqual(0, suggestions.Count);
        }

        [TestMethod]
        public void Compute_SourceInSelect()
        {
            _views
                .Setup(mock => mock.GetEnumerator())
                .Returns(new List<QueryView>
                {
                    new QueryView("test1", "abc", "test1.vql"),
                    new QueryView("test2", "abcd", "test2.vql"),
                    new QueryView("another", "a", "another.vql"),
                }.GetEnumerator());

            const string query = "select ";

            var suggestions = ComputeSuggestions(query);

            Assert.AreEqual(3, suggestions.Count);
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test1"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test2"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select another"));
        }

        [TestMethod]
        public void Compute_DontSuggestCompletedKeywords()
        {
            var suggestions = ComputeSuggestions("select");

            Assert.AreEqual(0, suggestions.Count);

            suggestions = ComputeSuggestions("select view where");

            Assert.AreEqual(0, suggestions.Count);
        }

        [TestMethod]
        public void Compute_QueryViewName()
        {
            _views
                .Setup(mock => mock.GetEnumerator())
                .Returns(new List<QueryView>
                {
                    new QueryView("test1", "abc", "test1.vql"),
                    new QueryView("test2", "abcd", "test2.vql"),
                    new QueryView("another", "a", "another.vql"),
                }.GetEnumerator());

            var suggestions = ComputeSuggestions("select TeS");

            Assert.AreEqual(2, suggestions.Count);
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test1"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test2"));
        }

        [TestMethod]
        public void Compute_ComplexViewName()
        {
            _views
                .Setup(mock => mock.GetEnumerator())
                .Returns(new List<QueryView>
                {
                    new QueryView("complex id", "abc", "complex id.vql"),
                    new QueryView("complex-id", "abcd", "complex-id.vql"),
                    new QueryView("non_complex_id", "abcd", "non_complex_id.vql"),
                }.GetEnumerator());

            var suggestions = ComputeSuggestions("select complex");

            Assert.AreEqual(3, suggestions.Count);
            Assert.IsTrue(ContainsSuggestion(suggestions, "select `complex id`"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select `complex-id`"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select non_complex_id"));
        }

        [TestMethod]
        public void Compute_AfterSelect()
        {
            var suggestions = ComputeSuggestions("select test ");

            Assert.AreEqual(6, suggestions.Count);
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test where"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test order by"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test group by"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test union"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test except"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test intersect"));
        }

        [TestMethod]
        public void Compute_AfterExpressionFactor()
        {
            var suggestions = ComputeSuggestions("select test where a ");

            Assert.AreEqual(7, suggestions.Count);
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test where a and"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test where a or"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test where a order by"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test where a intersect"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test where a union"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test where a except"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test where a group by"));
        }

        [TestMethod]
        public void Compute_AttributeNameSuggestions()
        {
            _attributeCache
                .Setup(mock => mock.GetNames(""))
                .Returns(new[] {"attr1", "attr2"});

            var suggestions = ComputeSuggestions("select test where ");

            Assert.AreEqual(4, suggestions.Count);
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test where not"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test where attr1"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test where attr2"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test where DateTime()"));
        }

        [TestMethod]
        public void Compute_AttributeNamePrefixSuggestion()
        {
            _attributeCache
                .Setup(mock => mock.GetNames("pr"))
                .Returns(new[] {"prefix", "prefix2"});

            var suggestions = ComputeSuggestions("select test where pr");

            Assert.AreEqual(2, suggestions.Count);
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test where prefix"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test where prefix2"));
        }

        [TestMethod]
        public void Compute_AttributeNameSuggestionInSubexpression()
        {
            _attributeCache
                .Setup(mock => mock.GetNames(""))
                .Returns(new[] {"test1", "test2"});

            var suggestions = ComputeSuggestions("select test where a + ");

            Assert.AreEqual(3, suggestions.Count);
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test where a + test1"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test where a + test2"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test where a + DateTime()"));
        }

        [TestMethod]
        public void Compute_AttributeNamesInOrderBy()
        {
            _attributeCache
                .Setup(mock => mock.GetNames("pr"))
                .Returns(new[] {"prefix", "prefix2"});

            var suggestions = ComputeSuggestions("select test order by pr");

            Assert.AreEqual(2, suggestions.Count);
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test order by prefix"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test order by prefix2"));
        }

        [TestMethod]
        public void Compute_DirectionInOrderByClause()
        {
            var suggestions = ComputeSuggestions("select test order by attr ");

            Assert.AreEqual(6, suggestions.Count);
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test order by attr desc"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test order by attr asc"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test order by attr union"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test order by attr intersect"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test order by attr except"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test order by attr group by"));
        }

        [TestMethod]
        public void Compute_DirectorySuggestionsReturnEmptySetForPatterns()
        {
            var suggestions = ComputeSuggestions("select \"a/b?rt");
            Assert.AreEqual(0, suggestions.Count);

            suggestions = ComputeSuggestions("select \"a/b*d");
            Assert.AreEqual(0, suggestions.Count);

            suggestions = ComputeSuggestions("select \"a/**");
            Assert.AreEqual(0, suggestions.Count);
        }

        [TestMethod]
        public void Compute_DirectorySuggestionsAtTheEnd()
        {
            _fileSystem.Setup(mock => mock.CreateFileFinder("a/*")).Returns(_fileFinder.Object);

            _fileFinder
                .Setup(mock => mock.GetDirectories())
                .Returns(new[] {"a/b", "a/c", "a/d"});

            var suggestions = ComputeSuggestions("select \"a/");

            Assert.AreEqual(3, suggestions.Count);
            Assert.IsTrue(ContainsSuggestion(suggestions, "select \"a/b"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select \"a/c"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select \"a/d"));
        }

        [TestMethod]
        public void Compute_DirectorySuggestionsRemoveDuplicities()
        {
            _fileSystem.Setup(mock => mock.CreateFileFinder("a/**/*")).Returns(_fileFinder.Object);

            _fileFinder
                .Setup(mock => mock.GetDirectories())
                .Returns(new[] {"a/b", "a/c", "a/x/b"});

            var suggestions = ComputeSuggestions("select \"a/**/");

            Assert.AreEqual(2, suggestions.Count);
            Assert.IsTrue(ContainsSuggestion(suggestions, "select \"a/**/b"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select \"a/**/c"));
        }

        [TestMethod]
        public void Compute_DirectorySuggestionsInTheMiddle()
        {
            _fileSystem.Setup(mock => mock.CreateFileFinder("a/*x*")).Returns(_fileFinder.Object);

            _fileFinder
                .Setup(mock => mock.GetDirectories())
                .Returns(new[] {"a/xz", "a/xy", "a/yxz"});

            var suggestions = ComputeSuggestions("select \"a/x/b\"", 11);

            Assert.AreEqual(3, suggestions.Count);
            Assert.IsTrue(ContainsSuggestion(suggestions, "select \"a/xy/b\""));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select \"a/xz/b\""));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select \"a/yxz/b\""));
        }

        [TestMethod]
        public void Compute_DirectorySuggestionsWillBeEmptyIfThePatternIsClosed()
        {
            var suggestions = ComputeSuggestions("select \"a/x/b\"");

            Assert.AreEqual(0, suggestions.Count);
        }

        [TestMethod]
        public void Compute_DirectorySuggestionsAfterDriveLetter()
        {
            _fileSystem.Setup(mock => mock.CreateFileFinder("D:/*a*")).Returns(_fileFinder.Object);

            _fileFinder
                .Setup(mock => mock.GetDirectories())
                .Returns(new string[] { });

            var suggestions = ComputeSuggestions("select \"D:/a");

            Assert.AreEqual(0, suggestions.Count);
        }

        [TestMethod]
        public void Compute_DirectorySuggestionsWillBeEmptyIfThePatternContainsInvalidCharacters()
        {
            _fileSystem
                .Setup(mock => mock.CreateFileFinder("*x <> y*"))
                .Throws(new ArgumentException());

            var suggestions = ComputeSuggestions("select \"x <> y");

            Assert.AreEqual(0, suggestions.Count);
        }

        [TestMethod]
        public void Compute_SuggestAttributeNamesRightAfterLeftParentesis()
        {
            _attributeCache
                .Setup(mock => mock.GetNames(""))
                .Returns(new[] {"attr1", "attr2"});

            const string query = "select view where (";

            var suggestions = ComputeSuggestions(query);

            Assert.AreEqual(4, suggestions.Count);
            Assert.IsTrue(ContainsSuggestion(suggestions, "select view where (not"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select view where (attr1"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select view where (attr2"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select view where (DateTime()"));
        }

        [TestMethod]
        public void Compute_SuggestStringAttributeValues()
        {
            _attributeCache
                .Setup(mock => mock.GetValues("a"))
                .Returns(new BaseValue[]
                {
                    new StringValue("test1"), new StringValue("test2"), new IntValue(1),
                });

            const string query = "select view where a = \"\n";
            var suggestions = ComputeSuggestions(query, query.Length - 1);

            Assert.AreEqual(2, suggestions.Count);
            Assert.IsTrue(ContainsSuggestion(suggestions, "select view where a = \"test1\""));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select view where a = \"test2\""));
        }

        [TestMethod]
        public void Compute_SuggestionAttributeValuesBasedOnSubstringMatch()
        {
            _attributeCache
                .Setup(mock => mock.GetNames("value"))
                .Returns(Enumerable.Empty<string>());
            _attributeCache
                .Setup(mock => mock.GetValues("a"))
                .Returns(new BaseValue[]
                {
                    new StringValue("contains value"),
                    new StringValue("does not contain val"),
                    new IntValue(1),
                });

            const string query = "select view where a = value";
            var suggestions = ComputeSuggestions(query);

            Assert.AreEqual(1, suggestions.Count);
            Assert.IsTrue(ContainsSuggestion(suggestions, "select view where a = \"contains value\""));
        }

        [TestMethod]
        public void Compute_DontSuggestValuesOutsideOfTheirScope()
        {
            _attributeCache
                .Setup(mock => mock.GetNames(""))
                .Returns(new[] {"a"});
            _attributeCache
                .Setup(mock => mock.GetValues("a"))
                .Returns(new List<BaseValue>
                {
                    new StringValue("value")
                });

            var suggestions = ComputeSuggestions("select view where a = \"value\" and ");

            Assert.AreEqual(3, suggestions.Count);
            Assert.IsTrue(ContainsSuggestion(suggestions, "select view where a = \"value\" and a"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select view where a = \"value\" and not"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select view where a = \"value\" and DateTime()"));
        }

        [TestMethod]
        public void Compute_DontSuggestValuesOutsideOfTheirScopeInOrderBy()
        {
            _attributeCache
                .Setup(mock => mock.GetNames(""))
                .Returns(new[] { "a" });
            _attributeCache
                .Setup(mock => mock.GetValues("a"))
                .Returns(new List<BaseValue>
                {
                    new StringValue("value")
                });

            var suggestions = ComputeSuggestions("select view where a = \"value\" order by ");
            
            Assert.AreEqual(2, suggestions.Count);
            Assert.IsTrue(ContainsSuggestion(suggestions, "select view where a = \"value\" order by a"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select view where a = \"value\" order by DateTime()"));
        }

        [TestMethod]
        public void Compute_SuggestionsContainCaretToken()
        {
            _attributeCache
                .Setup(mock => mock.GetNames("pref"))
                .Returns(new[] {"pref", "prefix"});

            var suggestions = ComputeSuggestions("select view where pref");

            Assert.AreEqual(2, suggestions.Count);
            Assert.IsTrue(ContainsSuggestion(suggestions, "select view where pref"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select view where prefix"));
        }

        [TestMethod]
        public void Compute_SuggestStringValueInString()
        {
            _attributeCache
                .Setup(mock => mock.GetValues("a"))
                .Returns(new BaseValue[]
                {
                    new StringValue("prefix1"),
                    new StringValue("prefix2"),
                    new IntValue(1)
                });

            const string query = "select view where a = \"\"";
            var suggestions = ComputeSuggestions(query, query.Length - 1);

            Assert.AreEqual(2, suggestions.Count);
            Assert.IsTrue(ContainsSuggestion(suggestions, "select view where a = \"prefix1\""));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select view where a = \"prefix2\""));
        }

        [TestMethod]
        public void Compute_SuggestAttributeNamesBeforeEndParenthesis()
        {
            _attributeCache
                .Setup(mock => mock.GetNames(""))
                .Returns(new[]{ "attr1", "attr2" });

            const string query = "select view where (test or )";
            var suggestions = ComputeSuggestions(query, query.Length - 1);

            Assert.AreEqual(4, suggestions.Count);
            Assert.IsTrue(ContainsSuggestion(suggestions, "select view where (test or attr1)"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select view where (test or attr2)"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select view where (test or not)"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select view where (test or DateTime())"));
        }

        [TestMethod]
        public void Compute_SuggestValuesOfComplexIdentifiers()
        {
            _attributeCache
                .Setup(mock => mock.GetValues("complex id"))
                .Returns(new BaseValue[] {new StringValue("string"), new IntValue(1),});

            var suggestions = ComputeSuggestions("select all where `complex id` = ");

            Assert.AreEqual(3, suggestions.Count);
            Assert.IsTrue(ContainsSuggestion(suggestions, "select all where `complex id` = \"string\""));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select all where `complex id` = 1"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select all where `complex id` = DateTime()"));
        }

        [TestMethod]
        public void Compute_SuggestValuesCorrectlyInLiteralRule()
        {
            _attributeCache
                .Setup(mock => mock.GetValues("a"))
                .Returns(new BaseValue[]
                {
                    new IntValue(1),
                    new IntValue(2),
                });

            var suggestions = ComputeSuggestions("select all where not a = ");

            Assert.AreEqual(3, suggestions.Count);
            Assert.IsTrue(ContainsSuggestion(suggestions, "select all where not a = 1"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select all where not a = 2"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select all where not a = DateTime()"));
        }

        [TestMethod]
        public void Compute_DateTimeValueSuggestion()
        {
            _attributeCache
                .Setup(mock => mock.GetValues("a"))
                .Returns(new BaseValue[]
                {
                    new DateTimeValue(new DateTime(2018, 10, 27, 19, 23, 0)),
                    new DateTimeValue(new DateTime(2015, 1, 26, 1, 21, 0)),
                });
            
            var suggestions = ComputeSuggestions("select all where a >= ");

            Assert.AreEqual(3, suggestions.Count);
            Assert.IsTrue(ContainsSuggestion(suggestions, "select all where a >= date(\"2018-10-27 19:23:00\")"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select all where a >= date(\"2015-01-26 01:21:00\")"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select all where a >= DateTime()"));
        }

        [TestMethod]
        public void Compute_FunctionSuggestionWontAddDoubleParentheses()
        {
            const string query = "select all where (";
            var suggestions = ComputeSuggestions(query, query.Length - 1);

            Assert.AreEqual(2, suggestions.Count);
            Assert.IsTrue(ContainsSuggestion(suggestions, "select all where DateTime("));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select all where not("));

            // make sure we moved caret after the first parenthesis
            var suggestion = suggestions.OfType<FunctionSuggestion>().First();
            var result = suggestion.Apply();
            Assert.AreEqual(result.Query.Length, result.Caret);
        }

        [TestMethod]
        public void Compute_FunctionSuggestionWontAddDoubleParentheses2()
        {
            const string query = "select all where ()";
            var suggestions = ComputeSuggestions(query, query.Length - 2);

            Assert.AreEqual(2, suggestions.Count);
            Assert.IsTrue(ContainsSuggestion(suggestions, "select all where DateTime()"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select all where not()"));

            // make sure we moved caret after the first parenthesis
            var suggestion = suggestions.OfType<FunctionSuggestion>().First();
            var result = suggestion.Apply();
            Assert.AreEqual(result.Query.Length - 1, result.Caret);
        }
    }
}
