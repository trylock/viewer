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

namespace ViewerTest.Query.Suggestions
{
    [TestClass]
    public class QuerySuggestionsTest
    {
        private List<QueryView> _viewList;

        private Mock<IFileFinder> _fileFinder;
        private Mock<IFileSystem> _fileSystem;
        private Mock<IAttributeCache> _attributeCache;
        private Mock<IQueryViewRepository> _views;
        private QuerySuggestions _suggestions;

        [TestInitialize]
        public void Setup()
        {
            _viewList = new List<QueryView>
            {
                new QueryView("test1", "abc", "test1.vql"),
                new QueryView("test2", "abcd", "test2.vql"),
                new QueryView("another", "a", "another.vql"),
            };

            _fileFinder = new Mock<IFileFinder>();
            _fileSystem = new Mock<IFileSystem>();
            _views = new Mock<IQueryViewRepository>();
            _attributeCache = new Mock<IAttributeCache>();

            _views
                .Setup(mock => mock.GetEnumerator())
                .Returns(_viewList.GetEnumerator());

            _suggestions = new QuerySuggestions(new ISuggestionProviderFactory[]
            {
                new ViewSuggestionProviderFactory(_views.Object), 
                new AttributeNameSuggestionProviderFactory(_attributeCache.Object), 
                new AttributeValueSuggestionProviderFactory(_attributeCache.Object), 
                new DirectorySuggestionProviderFactory(_fileSystem.Object), 
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
            var suggestions = ComputeSuggestions("select TeS");

            Assert.AreEqual(2, suggestions.Count);
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test1"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test2"));
        }

        [TestMethod]
        public void Compute_AfterSelect()
        {
            var suggestions = ComputeSuggestions("select test ");

            Assert.AreEqual(5, suggestions.Count);
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test where"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test order by"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test union"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test except"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test intersect"));
        }

        [TestMethod]
        public void Compute_AfterExpressionFactor()
        {
            var suggestions = ComputeSuggestions("select test where a ");

            Assert.AreEqual(6, suggestions.Count);
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test where a and"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test where a or"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test where a order by"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test where a intersect"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test where a union"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test where a except"));
        }

        [TestMethod]
        public void Compute_AttributeNameSuggestions()
        {
            _attributeCache
                .Setup(mock => mock.GetNames(""))
                .Returns(new[] { "attr1", "attr2" });

            var suggestions = ComputeSuggestions("select test where ");

            Assert.AreEqual(3, suggestions.Count);
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test where not"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test where attr1"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test where attr2"));
        }

        [TestMethod]
        public void Compute_AttributeNamePrefixSuggestion()
        {
            _attributeCache
                .Setup(mock => mock.GetNames("pr"))
                .Returns(new[] { "prefix", "prefix2" });

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
                .Returns(new[] { "test1", "test2" });
            
            var suggestions = ComputeSuggestions("select test where a + ");

            Assert.AreEqual(2, suggestions.Count);
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test where a + test1"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test where a + test2"));
        }

        [TestMethod]
        public void Compute_AttributeNamesInOrderBy()
        {
            _attributeCache
                .Setup(mock => mock.GetNames("pr"))
                .Returns(new[] { "prefix", "prefix2" });

            var suggestions = ComputeSuggestions("select test order by pr");

            Assert.AreEqual(2, suggestions.Count);
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test order by prefix"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test order by prefix2"));
        }

        [TestMethod]
        public void Compute_DirectionInOrderByClause()
        {
            var suggestions = ComputeSuggestions("select test order by attr ");

            Assert.AreEqual(5, suggestions.Count);
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test order by attr desc"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test order by attr asc"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test order by attr union"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test order by attr intersect"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test order by attr except"));
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
                .Returns(new[] {"a/b", "a/c", "a/d" });

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
                .Returns(new[] { "a/b", "a/c", "a/x/b" });

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
                .Returns(new[] { "a/xz", "a/xy", "a/yxz" });

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
                .Returns(new[] { "attr1", "attr2" });

            const string query = "select view where (";

            var suggestions = ComputeSuggestions(query);

            Assert.AreEqual(3, suggestions.Count);
            Assert.IsTrue(ContainsSuggestion(suggestions, "select view where (not"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select view where (attr1"));
            Assert.IsTrue(ContainsSuggestion(suggestions, "select view where (attr2"));
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
    }
}
