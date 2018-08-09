using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Viewer.Data;
using Viewer.Data.Formats;
using Viewer.Data.Storage;
using Viewer.IO;
using Path = System.IO.Path;

namespace Viewer.Query
{
    /// <inheritdoc />
    /// <summary>
    /// Immutable structure which describes a query
    /// </summary>
    public interface IQuery : IEnumerable<IEntity>
    {
        /// <summary>
        /// Textual representation of the query
        /// </summary>
        string Text { get; }

        /// <summary>
        /// Current comparer to sort the query result
        /// </summary>
        IComparer<IEntity> Comparer { get; }

        /// <summary>
        /// Cancellation token source of the query
        /// </summary>
        CancellationTokenSource Cancellation { get; }

        /// <summary>
        /// Set comparer to order the query result
        /// </summary>
        /// <param name="comparer"></param>
        /// <returns></returns>
        IQuery WithComparer(IComparer<IEntity> comparer);

        /// <summary>
        /// Only include entities in the result if <paramref name="predicate"/> returns true
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        IQuery Where(Func<IEntity, bool> predicate);
        
        /// <summary>
        /// Compue set minus on this query and <paramref name="entities"/>
        /// </summary>
        /// <param name="entities">Entities to subtract from this query</param>
        /// <returns>New query</returns>
        IQuery Except(IEnumerable<IEntity> entities);

        /// <summary>
        /// Compue set union on this query and <paramref name="entities"/>
        /// </summary>
        /// <param name="entities">Entities to add to this query</param>
        /// <returns>New query</returns>
        IQuery Union(IEnumerable<IEntity> entities);

        /// <summary>
        /// Compue set intersection on this query and <paramref name="entities"/>
        /// </summary>
        /// <param name="entities"></param>
        /// <returns>New query</returns>
        IQuery Intersect(IEnumerable<IEntity> entities);

        /// <summary>
        /// Create the same query with different textual representation
        /// </summary>
        /// <param name="text">Textual representation of this query</param>
        /// <returns>New query with new textual representation</returns>
        IQuery WithText(string text);
    }

    public interface IQueryFactory
    {
        /// <summary>
        /// Create an empty query
        /// </summary>
        /// <returns>Empty query</returns>
        IQuery CreateQuery();

        /// <summary>
        /// Create a query with given path pattern
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        IQuery CreateQuery(string pattern);
    }

    internal class EntitySource : IEnumerable<IEntity>
    {
        private readonly IFileSystem _fileSystem;
        private readonly IEntityManager _entities;
        private readonly string _pattern;
        private readonly CancellationToken _cancellationToken;

        public EntitySource(IFileSystem fileSystem, IEntityManager entities, string pattern, CancellationToken token)
        {
            _fileSystem = fileSystem;
            _entities = entities;
            _pattern = pattern;
            _cancellationToken = token;
        }

        public IEnumerator<IEntity> GetEnumerator()
        {
            foreach (var dir in EnumerateDirectories())
            {
                _cancellationToken.ThrowIfCancellationRequested();

                // add files
                foreach (var file in _fileSystem.EnumerateFiles(dir))
                {
                    _cancellationToken.ThrowIfCancellationRequested();

                    // load jpeg files only
                    var extension = Path.GetExtension(file).ToLowerInvariant();
                    if (extension != ".jpg" && extension != ".jpeg")
                    {
                        continue;
                    }

                    // load file
                    IEntity entity = null;
                    try
                    {
                        entity = _entities.GetEntity(file);
                    }
                    catch (InvalidDataFormatException)
                    {
                    }

                    if (entity == null)
                    {
                        continue;
                    }

                    yield return entity;
                }

                // add directories
                foreach (var directory in _fileSystem.EnumerateDirectories(dir))
                {
                    _cancellationToken.ThrowIfCancellationRequested();
                    yield return new DirectoryEntity(directory);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private IEnumerable<string> EnumerateDirectories()
        {
            if (_pattern == null)
            {
                return Enumerable.Empty<string>();
            }

            var finder = new FileFinder(_fileSystem, _pattern);
            return finder.GetDirectories();
        }
    }

    public class Query : IQuery
    {
        private readonly IEnumerable<IEntity> _source;

        public string Text { get;}
        public IComparer<IEntity> Comparer { get; }
        public CancellationTokenSource Cancellation { get; }

        public Query(
            IEnumerable<IEntity> source, 
            IComparer<IEntity> comparer, 
            string text,
            CancellationTokenSource cancellation)
        {
            _source = source;
            Text = text;
            Comparer = comparer;
            Cancellation = cancellation;
        }

        public IEnumerator<IEntity> GetEnumerator()
        {
            return _source.GetEnumerator();
        }

        public override string ToString()
        {
            return Text;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IQuery Where(Func<IEntity, bool> predicate)
        {
            return new Query(_source.Where(predicate), Comparer, Text, Cancellation);
        }

        public IQuery WithComparer(IComparer<IEntity> comparer)
        {
            return new Query(_source, comparer, Text, Cancellation);
        }

        public IQuery Except(IEnumerable<IEntity> entities)
        {
            return new Query(_source.Except(entities, EntityPathEqualityComparer.Default), Comparer, Text, Cancellation);
        }

        public IQuery Union(IEnumerable<IEntity> entities)
        {
            return new Query(_source.Union(entities, EntityPathEqualityComparer.Default), Comparer, Text, Cancellation);
        }

        public IQuery Intersect(IEnumerable<IEntity> entities)
        {
            return new Query(_source.Intersect(entities, EntityPathEqualityComparer.Default), Comparer, Text, Cancellation);
        }

        public IQuery WithText(string text)
        {
            return new Query(_source, Comparer, text, Cancellation);
        }
    }
    
    [Export(typeof(IQueryFactory))]
    public class QueryFactory : IQueryFactory
    {
        private readonly IEntityManager _entities;
        private readonly IFileSystem _fileSystem;

        [ImportingConstructor]
        public QueryFactory(
            IEntityManager entities, 
            IFileSystem fileSystem)
        {
            _entities = entities;
            _fileSystem = fileSystem;
        }

        public IQuery CreateQuery()
        {
            return new Query(
                Enumerable.Empty<IEntity>(), 
                new EntityComparer(), 
                "", 
                new CancellationTokenSource());
        }

        public IQuery CreateQuery(string pattern)
        {
            var cancellation = new CancellationTokenSource();
            return new Query(
                new EntitySource(_fileSystem, _entities, pattern, cancellation.Token), 
                new EntityComparer(), 
                "SELECT \"" + pattern + "\"",
                cancellation);
        }
    }
}
