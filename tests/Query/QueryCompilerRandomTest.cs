using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime.Atn;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Viewer.Data.SQLite;
using Viewer.Data.Storage;
using Viewer.IO;
using Viewer.Query;

namespace ViewerTest.Query
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
    public class QueryCompilerRandomTest
    {
        private CompositionContainer _container;

        private IExecutableQuery Compile(string text)
        {
            var compiler = _container.GetExportedValue<IQueryCompiler>();
            return compiler.Compile(text);
        }

        [TestInitialize]
        public void Setup()
        {
            var catalog = new AggregateCatalog(
                new AssemblyCatalog(Assembly.GetAssembly(typeof(Viewer.Data.IEntity))),
                new AssemblyCatalog(Assembly.GetAssembly(typeof(Viewer.Query.IRuntime))),
                new AssemblyCatalog(Assembly.GetAssembly(typeof(Viewer.QueryRuntime.IntValueAdditionFunction))),
                new AssemblyCatalog(Assembly.GetAssembly(typeof(Viewer.IO.IFileSystem))),
                new TypeCatalog(typeof(Execution.ErrorListener)),
                new TypeCatalog(typeof(Execution.Configuration)));

            _container = new CompositionContainer(catalog);
            var compiler = _container.GetExportedValue<IQueryCompiler>();
            compiler.Views.Add(new QueryView("view", "select \"a\"", null));
        }

        private static string GenerateId(Random r)
        {
            var sb = new StringBuilder();
            var length = r.Next(1, 12);
            for (var i = 0; i < length; ++i)
            {
                sb.Append((char) ('a' + r.Next(26)));
            }

            return sb.ToString();
        }
        
        private static string GenerateString(Random r, char bounds)
        {
            var sb = new StringBuilder();
            sb.Append(bounds);
            var length = r.Next(1, 12);
            for (var i = 0; i < length; ++i)
            {
                char character = bounds;
                while (character == bounds)
                {
                    var index = r.Next(0, 256 - ' ');
                    character = (char) (' ' + index);
                }
                
                sb.Append(character);
            }

            sb.Append(bounds);

            return sb.ToString();
        }

        private static readonly string[] Relops =
        {
            "<", "<=", "!=", "=", "==", "<>", "!=", ">=", ">"
        };

        private readonly Dictionary<int, Func<Random, string>> _generateToken = 
            new Dictionary<int, Func<Random, string>>
        {
            { QueryLexer.SELECT, r => "select" },
            { QueryLexer.WHERE, r => "where" },
            { QueryLexer.ORDER, r => "order" },
            { QueryLexer.GROUP, r => "group" },
            { QueryLexer.BY, r => "by" },
            { QueryLexer.AND, r => "and" },
            { QueryLexer.OR, r => "or" },
            { QueryLexer.NOT, r => "not" },
            { QueryLexer.DIRECTION, r => r.Next(2) == 1 ? "asc" : "desc" },
            { QueryLexer.INTERSECT, r => "intersect" },
            { QueryLexer.UNION_EXCEPT, r => r.Next(2) == 1 ? "union" : "except" },
            { QueryLexer.ID, GenerateId },
            { QueryLexer.COMPLEX_ID, r => GenerateString(r, '`') },
            { QueryLexer.STRING, r => GenerateString(r, '"') },
            { QueryLexer.INT, r => r.Next(-10000, 10000).ToString(CultureInfo.InvariantCulture) },
            { QueryLexer.REAL, r => r.NextDouble().ToString(CultureInfo.InvariantCulture) },
            { QueryLexer.LPAREN, r => "(" },
            { QueryLexer.RPAREN, r => ")" },
            { QueryLexer.PARAM_DELIMITER, r => ", " },
            { QueryLexer.ADD_SUB, r => r.Next(2) == 1 ? "+" : "-" },
            { QueryLexer.MULT_DIV, r => r.Next(2) == 1 ? "*" : "/" },
            { QueryLexer.REL_OP, r => Relops[r.Next(Relops.Length)] },
        };
        
        private string GenerateRandomTokens(Random rand, int min, int max)
        {
            var result = new StringBuilder();
            var count = rand.Next(min, max);
            for (var i = 0; i < count; ++i)
            {
                var tokenIndex = rand.Next(_generateToken.Count);
                var generator = _generateToken.ElementAt(tokenIndex).Value;
                result.Append(generator(rand));
                result.Append(' ');
            }

            return result.ToString();
        }

        [TestMethod]
        public void Compile_RandomString()
        {
            var seed = DateTime.Now.Millisecond;
            var rand = new Random(seed);

            try
            {
                var sb = new StringBuilder();
                var length = rand.Next(0, 1000);
                for (var i = 0; i < length; ++i)
                {
                    char value = (char) rand.Next(char.MaxValue);
                    sb.Append(value);
                }
                
                var compiled = Compile(sb.ToString());
            }
            catch (Exception)
            {
                Debug.WriteLine($"Seed: {seed}");
                throw;
            }
        }

        [TestMethod]
        public void Compile_RandomStringOfValidTokens()
        {
            var seed = DateTime.Now.Millisecond;
            var rand = new Random(seed);

            try
            {
                var query = GenerateRandomTokens(rand, 0, 15);
                var compiled = Compile(query);
            }
            catch (Exception)
            {
                Debug.WriteLine($"Seed: {seed}");
                throw;
            }
        }
    }
}
