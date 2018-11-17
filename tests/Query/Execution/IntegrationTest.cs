using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Viewer.Data;
using Viewer.Data.Storage;
using Viewer.IO;
using Viewer.Query;
using Viewer.Query.Execution;

namespace ViewerTest.Query.Execution
{ 
    [Export(typeof(IQueryErrorListener))]
    public class ErrorListener : IQueryErrorListener
    {
        public void BeforeCompilation()
        {
        }

        public void OnCompilerError(int line, int column, string errorMessage)
        {
            Debug.WriteLine($"[{line}][{column}] Compilation error: {errorMessage}");
        }

        public void OnRuntimeError(int line, int column, string errorMessage)
        {
            Debug.WriteLine($"[{line}][{column}] Runtime error: {errorMessage}");
        }

        public void AfterCompilation()
        {
        }
    }
    
    [Export(typeof(IStorageConfiguration))]
    public class Configuration : IStorageConfiguration
    {
        public TimeSpan CacheLifespan => TimeSpan.FromDays(7);
        public int CacheMaxFileCount => int.MaxValue;
    }

    [TestClass]
    public class IntegrationTest
    {
        private IExecutableQuery Compile(string text)
        {
            var catalog = new AggregateCatalog(
                new AssemblyCatalog(Assembly.GetAssembly(typeof(Viewer.Data.IEntity))),
                new AssemblyCatalog(Assembly.GetAssembly(typeof(Viewer.Query.IRuntime))),
                new AssemblyCatalog(Assembly.GetAssembly(typeof(Viewer.QueryRuntime.IntValueAdditionFunction))),
                new AssemblyCatalog(Assembly.GetAssembly(typeof(Viewer.IO.IFileSystem))),
                new TypeCatalog(typeof(ErrorListener)),
                new TypeCatalog(typeof(Configuration)));

            using (var container = new CompositionContainer(catalog))
            {
                var compiler = container.GetExportedValue<IQueryCompiler>();
                return compiler.Compile(text);
            }
        }

        private class ResultSet : IReadOnlyList<IEntity>
        {
            private readonly List<IEntity> _entities = new List<IEntity>();
            
            public ResultSet(IEnumerable<IEntity> entities)
            {
                _entities.AddRange(entities);
            }
            
            public string[] GetPaths()
            {
                return _entities.Select(item => item.Path).ToArray();
            }

            public IEnumerator<IEntity> GetEnumerator()
            {
                return _entities.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public int Count => _entities.Count;

            public IEntity this[int index] => _entities[index];

            public bool ContainsFileAt(string path)
            {
                path = PathUtils.NormalizePath(path);
                var result = _entities.Find(entity =>
                    string.Equals(entity.Path, path, StringComparison.CurrentCultureIgnoreCase));
                return result != null;
            }
        }

        private ResultSet Execute(string query)
        {
            var compiledQuery = Compile(query);
            return new ResultSet(compiledQuery.Execute(new ExecutionOptions()));
        }

        private static readonly string BaseDir = Path.Combine(
            Path.GetDirectoryName(Path.GetDirectoryName(Environment.CurrentDirectory)),
            "ExecutionTestData");

        [TestMethod]
        public void Select_NonExistentFolder()
        {
            var result = Execute("select \"" + BaseDir + "/**/*error*\"");
            
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void Select_Subfolder()
        {
            var result = Execute("select \"" + BaseDir + "/**/c\"");
            
            Assert.AreEqual(4, result.Count);
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/b/c/item13.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/b/c/item14.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/b/c/item15.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/b/c/item16.jpg"));
        }

        [TestMethod]
        public void Select_Subtree()
        {
            var result = Execute("select \"" + BaseDir + "/**\"");
            
            Assert.AreEqual(19, result.Count);
            // files
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/item1.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/item2.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/item3.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/a/item4.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/a/item5.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/a/item6.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/b/item7.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/b/item8.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/b/item9.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/b/item10.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/b/item11.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/b/item12.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/b/c/item13.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/b/c/item14.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/b/c/item15.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/b/c/item16.jpg"));
            // folders
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/a"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/b"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/b/c"));
        }

        [TestMethod]
        public void Where_AttributeNamePredicate()
        {
            var result = Execute("select \"" + BaseDir + "/**\" where attr1");
            
            Assert.AreEqual(6, result.Count);
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/item1.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/item3.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/a/item4.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/a/item5.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/b/c/item13.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/b/c/item16.jpg"));
        }

        [TestMethod]
        public void Where_NamePredicateInDNF()
        {
            var result = Execute("select \"" + BaseDir + "/**\" where (attr1 and not attr2) or (attr2 and not attr1)");
            
            Assert.AreEqual(11, result.Count);
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/item1.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/item2.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/a/item4.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/a/item5.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/b/item7.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/b/item8.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/b/item9.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/b/item11.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/b/item12.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/b/c/item13.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/b/c/item16.jpg"));
        }

        [TestMethod]
        public void Where_NamePredicateInCNF()
        {
            var result = Execute("select \"" + BaseDir + "/**\" where (attr1 or attr2) and (not attr1 or not attr2)");
            
            Assert.AreEqual(11, result.Count);
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/item1.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/item2.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/a/item4.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/a/item5.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/b/item7.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/b/item8.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/b/item9.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/b/item11.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/b/item12.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/b/c/item13.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/b/c/item16.jpg"));
        }

        [TestMethod]
        public void Where_GreaterThanPredicate()
        {
            var result = Execute("select \"" + BaseDir + "/**\" where attr2 > 24");
            
            Assert.AreEqual(4, result.Count);
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/item2.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/b/item7.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/b/item8.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/b/item9.jpg"));
        }

        [TestMethod]
        public void Where_LessThanPredicate()
        {
            var result = Execute("select \"" + BaseDir + "/**\" where attr2 < 24");
            
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void Where_EqualsPredicate()
        {
            var result = Execute("select \"" + BaseDir + "/**\" where attr2 = 42");
            
            Assert.AreEqual(4, result.Count);
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/item2.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/b/item7.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/b/item8.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/b/item9.jpg"));
        }

        [TestMethod]
        public void Where_NotEqual()
        {
            var result = Execute("select \"" + BaseDir + "/**\" where attr1 != \"value\"");
            
            Assert.AreEqual(1, result.Count);
            
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/b/c/item16.jpg"));
        }

        [TestMethod]
        public void Where_NegatedEquals()
        {
            var result = Execute("select \"" + BaseDir + "/**\" where not (attr1 = \"value\")");
            
            Assert.AreEqual(14, result.Count);

            // files
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/item2.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/a/item6.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/b/item7.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/b/item8.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/b/item9.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/b/item10.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/b/item11.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/b/item12.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/b/c/item14.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/b/c/item15.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/b/c/item16.jpg"));
            // folders
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/a"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/b"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/b/c"));
        }

        [TestMethod]
        public void Intersect_PatternSubset()
        {
            var result = Execute("select \"" + BaseDir + "/**\" intersect select \"" + BaseDir + "/a/**\"");

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/a/item4.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/a/item5.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/a/item6.jpg"));
        }
        
        [TestMethod]
        public void Intersect_PatternSuperset()
        {
            var result = Execute("select \"" + BaseDir + "/a/**\" intersect select \"" + BaseDir + "/**\"");

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/a/item4.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/a/item5.jpg"));
            Assert.IsTrue(result.ContainsFileAt(BaseDir + "/a/item6.jpg"));
        }
    }
}
