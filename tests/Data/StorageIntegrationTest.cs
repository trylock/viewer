using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Viewer.Data;
using Viewer.Data.SQLite;
using Viewer.Data.Storage;
using Viewer.IO;
using ViewerTest.Query.Execution;
using Attribute = Viewer.Data.Attribute;

namespace ViewerTest.Data
{
    /// <summary>
    /// This test expects following file structure:
    /// StorageTestData/empty.jpg - a JPEG file without user attributes
    /// </summary>
    /// <remarks>The StorageTestData directory is copied</remarks>
    [TestClass]
    public class StorageIntegrationTest
    {
        private const string DatabaseFile = "test.db";

        /// <summary>
        /// Directory with test data
        /// </summary>
        private static readonly string BaseDir = Path.Combine(
            Path.GetDirectoryName(Path.GetDirectoryName(Environment.CurrentDirectory)),
            "StorageTestData");

        /// <summary>
        /// Directory where the test data will be copied to
        /// </summary>
        private static readonly string TestDataDir = BaseDir + "Copy";

        private CompositionContainer _container;
        private IAttributeStorage _storage;
        
        [TestInitialize]
        public void Setup()
        {
            var catalog = new AggregateCatalog(
                new AssemblyCatalog(Assembly.GetAssembly(typeof(Viewer.Data.IEntity))),
                new AssemblyCatalog(Assembly.GetAssembly(typeof(Viewer.Query.IRuntime))),
                new AssemblyCatalog(Assembly.GetAssembly(typeof(Viewer.QueryRuntime.IntValueAdditionFunction))),
                new AssemblyCatalog(Assembly.GetAssembly(typeof(Viewer.IO.IFileSystem))),
                new TypeCatalog(typeof(ErrorListener)),
                new TypeCatalog(typeof(Configuration)));

            _container = new CompositionContainer(catalog);

            // make SQLite connection factory create and use database in a local file
            var fileSystem = _container.GetExportedValue<IFileSystem>();
            _container.ComposeExportedValue<SQLiteConnectionFactory>(
                new SQLiteConnectionFactory(fileSystem, DatabaseFile));

            _storage = _container.GetExportedValue<IAttributeStorage>();

            // copy data directory 
            try
            {
                Directory.Delete(TestDataDir, true);
            }
            catch (DirectoryNotFoundException)
            {
                // this is ok
            }

            Directory.CreateDirectory(TestDataDir);
            foreach (var file in Directory.EnumerateFiles(BaseDir))
            {
                var target = Path.Combine(TestDataDir, Path.GetFileName(file));
                File.Copy(file, target);
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            _container.Dispose();
            _container = null;
            SQLiteConnection.ClearAllPools();
            File.Delete(DatabaseFile);
            Directory.Delete(TestDataDir, true);
        }

        private static readonly Random _random = new Random();

        /// <summary>
        /// Generate a random string of length <paramref name="minLength"/> to
        /// <paramref name="maxLength"/>. It uses some printable 7 bit ASCII characters
        /// </summary>
        /// <param name="minLength"></param>
        /// <param name="maxLength"></param>
        /// <returns></returns>
        private string GenerateRandomString(int minLength, int maxLength)
        {
            var sb = new StringBuilder();
            int length;
            lock (_random)
            {
                length = _random.Next(minLength, maxLength);
            }

            for (var i = 0; i < length; ++i)
            {
                char next;
                lock (_random)
                {
                    next = (char) (' ' + _random.Next(' ', 0x7F));
                }

                sb.Append(next);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Check if <paramref name="path"/> is an image readable by GDI
        /// </summary>
        /// <param name="path">path to a file</param>
        private void VerifyImage(string path)
        {
            using (Bitmap image = (Bitmap) Image.FromFile(path))
            {
                Assert.IsTrue(image.GetHbitmap() != IntPtr.Zero);
            }
        }

        [TestMethod]
        public void Read_EmptyFileUsingLoadApi()
        {
            var entity = _storage.Load(TestDataDir + "/empty.jpg");
            Assert.IsFalse(entity.Any(attr => attr.Source == AttributeSource.Custom));

            VerifyImage(TestDataDir + "/empty.jpg");
        }

        [TestMethod]
        public void Read_EmptyFileUsingBatchApi()
        {
            using (var reader = _storage.CreateReader())
            {
                var entity = reader.Load(TestDataDir + "/empty.jpg");
                Assert.IsFalse(entity.Any(attr => attr.Source == AttributeSource.Custom));
            }

            VerifyImage(TestDataDir + "/empty.jpg");
        }

        [TestMethod]
        public void Store_AttributesOfDifferentTypes()
        {
            var entity = _storage.Load(TestDataDir + "/empty.jpg");
            entity.SetAttribute(new Attribute("string", new StringValue("value"), AttributeSource.Custom));
            entity.SetAttribute(new Attribute("int", new IntValue(42), AttributeSource.Custom));
            entity.SetAttribute(new Attribute("real", new RealValue(3.14159), AttributeSource.Custom));
            entity.SetAttribute(new Attribute("DateTime", new DateTimeValue(new DateTime(2019, 1, 26)), AttributeSource.Custom));

            _storage.Store(entity);

            var modified = _storage.Load(TestDataDir + "/empty.jpg");
            Assert.AreEqual("value", modified.GetValue<StringValue>("string").Value);
            Assert.AreEqual(42, modified.GetValue<IntValue>("int").Value);
            Assert.AreEqual(3.14159, modified.GetValue<RealValue>("real").Value);
            Assert.AreEqual(new DateTime(2019, 1, 26), modified.GetValue<DateTimeValue>("DateTime").Value);
            
            VerifyImage(TestDataDir + "/empty.jpg");
        }

        [TestMethod]
        public void Store_ManySmallAttributes()
        {
            var tags = new Dictionary<string, string>();
            var entity = _storage.Load(TestDataDir + "/empty.jpg");
            for (var i = 0; i < 10000; ++i)
            {
                string name = GenerateRandomString(1, 15); 
                string value = GenerateRandomString(10, 40);
                tags[name] = value;
                entity.SetAttribute(new Attribute(name, new StringValue(value), AttributeSource.Custom));
            }
            _storage.Store(entity);

            var modified = _storage.Load(TestDataDir + "/empty.jpg");
            foreach (var pair in tags)
            {
                var value = modified.GetValue<StringValue>(pair.Key).Value;
                Assert.AreEqual(value, pair.Value);
            }

            VerifyImage(TestDataDir + "/empty.jpg");
        }

        [TestMethod]
        public void Store_SmallNumberOfVeryLargeAttributes()
        {
            var tags = new Dictionary<string, string>();
            var entity = _storage.Load(TestDataDir + "/empty.jpg");
            for (var i = 0; i < 20; ++i)
            {
                string name = GenerateRandomString(100, 200);
                string value = GenerateRandomString(10000, 31415);
                tags[name] = value;
                entity.SetAttribute(new Attribute(name, new StringValue(value), AttributeSource.Custom));
            }
            _storage.Store(entity);

            var modified = _storage.Load(TestDataDir + "/empty.jpg");
            foreach (var pair in tags)
            {
                var value = modified.GetValue<StringValue>(pair.Key).Value;
                Assert.AreEqual(value, pair.Value);
            }

            VerifyImage(TestDataDir + "/empty.jpg");
        }

        [TestMethod]
        public void Store_MultipleConcurrentReadersAndOneWriter()
        {
            // generate tags to write
            var tags = new Dictionary<string, Attribute>();
            for (var i = 0; i < 1000; ++i)
            {
                string name = GenerateRandomString(1, 12);
                string value = GenerateRandomString(10, 40);
                tags[name] = new Attribute(name, new StringValue(value), AttributeSource.Custom);
            }
            
            var random = new Random();
            Parallel.For(0, 20, i =>
            {
                // add random delay
                int delayTime;
                lock (random)
                {
                    delayTime = random.Next(0, 500);
                }
                Thread.Sleep(delayTime);

                try
                {
                    var entity = _storage.Load(TestDataDir + "/empty.jpg");

                    // i = 0 is writer, others are readers
                    if (i == 0)
                    {
                        foreach (var attr in tags)
                        {
                            entity.SetAttribute(attr.Value);
                        }

                        _storage.Store(entity);
                    }
                    else // reader
                    {
                        // it either sees all attributes written to the file, or none of them
                        var attributes = entity
                            .Where(attr => attr.Source == AttributeSource.Custom)
                            .ToList();
                        var hasAllTags = attributes.All(attr => tags[attr.Name].Equals(attr)) &&
                                         attributes.Count == tags.Count;
                        var doesNotHaveAnyTag = !attributes.Any(attr => tags[attr.Name].Equals(attr));
                        Assert.IsTrue(hasAllTags || doesNotHaveAnyTag);
                    }
                }
                catch (IOException e) when (e.GetType() == typeof(IOException))
                {
                    // a thread tried to read the file while it is being replaced, this is ok
                }
            });

            var userAttributes = _storage.Load(TestDataDir + "/empty.jpg")
                .Where(attr => attr.Source == AttributeSource.Custom)
                .ToList();
            var containsExactlyTags = userAttributes
                 .All(attr => tags[attr.Name].Equals(attr)) && userAttributes.Count == tags.Count;
            Assert.IsTrue(containsExactlyTags);

            VerifyImage(TestDataDir + "/empty.jpg");
        }

        [TestMethod]
        public void Store_MultipleConcurrentReadersAndWriters()
        {
            // generate tags to write
            var tags = new Dictionary<string, Attribute>();
            for (var i = 0; i < 1000; ++i)
            {
                string name = GenerateRandomString(1, 12);
                string value = GenerateRandomString(10, 40);
                tags[name] = new Attribute(name, new StringValue(value), AttributeSource.Custom);
            }

            // stress test reading/writing file with high contention
            var random = new Random();
            Parallel.For(0, 20, i =>
            {
                // add random delay
                int delayTime;
                lock (random)
                {
                    delayTime = random.Next(0, 500);
                }
                Thread.Sleep(delayTime);

                try
                {
                    var entity = _storage.Load(TestDataDir + "/empty.jpg");

                    // writer
                    if (i % 2 == 0)
                    {
                        foreach (var attr in tags)
                        {
                            entity.SetAttribute(attr.Value);
                        }

                        try
                        {
                            _storage.Store(entity);
                        }
                        catch (IOException)
                        {
                            // file is busy
                        }
                    }
                    else // reader
                    {
                        // it either sees all attributes written to the file, or none of them
                        var attributes = entity
                            .Where(attr => attr.Source == AttributeSource.Custom)
                            .ToList();
                        var hasAllTags = attributes.All(attr => tags[attr.Name].Equals(attr)) &&
                                         attributes.Count == tags.Count;
                        var doesNotHaveAnyTag = !attributes.Any(attr => tags[attr.Name].Equals(attr));
                        Assert.IsTrue(hasAllTags || doesNotHaveAnyTag);
                    }
                }
                catch (IOException e) when (e.GetType() == typeof(IOException))
                {
                    // a thread tried to read the file while it is being replaced, this is ok
                }
            });

            var userAttributes = _storage.Load(TestDataDir + "/empty.jpg")
                .Where(attr => attr.Source == AttributeSource.Custom)
                .ToList();
            var containsExactlyTags = userAttributes
                 .All(attr => tags[attr.Name].Equals(attr)) && userAttributes.Count == tags.Count;
            Assert.IsTrue(containsExactlyTags);

            VerifyImage(TestDataDir + "/empty.jpg");
        }
    }
}
