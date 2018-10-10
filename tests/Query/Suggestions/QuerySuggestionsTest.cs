using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Viewer.Data;
using Viewer.Query;
using Viewer.Query.Suggestions;
using Viewer.Query.Suggestions.Providers;

namespace ViewerTest.Query.Suggestions
{
    [TestClass]
    public class QuerySuggestionsTest
    {
        private List<QueryView> _viewList;

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

            _views = new Mock<IQueryViewRepository>();
            _attributeCache = new Mock<IAttributeCache>();

            _views
                .Setup(mock => mock.GetEnumerator())
                .Returns(_viewList.GetEnumerator());

            _suggestions = new QuerySuggestions(new ISuggestionProviderFactory[]
            {
                new ViewSuggestionProviderFactory(_views.Object), 
                new AttributeNameSuggestionProviderFactory(_attributeCache.Object), 
            });
        }

        private List<IQuerySuggestion> ComputeSuggestions(string queryPrefix)
        {
            return _suggestions.Compute(queryPrefix, queryPrefix.Length).ToList();
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
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test order"));
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
            Assert.IsTrue(ContainsSuggestion(suggestions, "select test where a order"));
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
    }
}
