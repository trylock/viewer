# Viewer.Query

## Compilation

Expressions in the `where` part and the `order by` part of the query are compiled to a C# code using [Expression Trees](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/expression-trees/). Since we don't know the type of an attribute, many decisions have to be made at runtime. The generated code will therefore mostly just call runtime functions. Expression trees are used mainly for convenience (no need to write an interpreter). 

The compiled function takes an [entity](xref:Viewer.Data.IEntity) and returns a [value](xref:Viewer.Data.BaseValue). If the returned value in the `where` part of a query is null (i.e., [IsNull](xref:Viewer.Data.BaseValue#Viewer_Data_BaseValue_IsNull) is true), it is interpreted as `false` (i.e., the entity is not included in the result).

## Execution

Queries are executed lazily by calling the [Execute](xref:Viewer.Query.IExecutableQuery) method. It returns an enumerator which loads entities. Even queries which contain `UNION`, `EXCEPT` and `INTERSECT` operators are evaluated lazily. This is possible due to the [Match](xref:Viewer.Query.IExecutableQuery) method which can determine statically (i.e., without looking at any other entity) whether or not an entity matches given query.

> [!NOTE]
> Returned entities are not sorted. If we were to sort the values before returning them, we would have to first find all photos. 

The [Execute](xref:Viewer.Query.IExecutableQuery) method supports standard cancellation and progress even though it is not an async method and it executes completely synchronously.  

## Optimizations

As discussed in the section about [data representation](data.md), attributes are stored in custom JPEG segments. This means that whenever we want to load attributes a photo, we just have to load first few disk blocks. Usually, the number of read blocks is fairly small. In my data, it is around 10 disk blocks although this can vary based on used camera and programs which also store custom metadata to JPEG segments. Unfortunately, when we want to read many photos, the blocks are far apart since, at best, there is the encoded image data in-between 2 attribute segments. This is especially problematic on HDDs where seek time and rotational delay are included with each random access.

### Indexing

One of the most effective and simple optimizations the application uses is storing attributes of recently seen photos in a SQLite database. This way, the data is indexed using a B-tree structure. B-trees are especially suited for disk access as only a fraction of blocks is necessary to be loaded from the disk with each lookup operation. Also, the first few levels of the tree can be kept in main memory so that following requests can locate their data even faster.

Usually, photos are stored on an HDD either localy or in a local network on a NAS server. While individual photos take up just a few tens of MBs of disk space, it is common to have collections of thousands of them. Most of the space is occupated by encoded image data. The size of attributes, and by extension the size of the index file, is orders of magnitude smaller than the size of the photo collection. This means that the index structure can be stored on a smaller and faster disk type. For example, it can be stored on a small SSD or even in main memory. 

### Search order

When we first see a directory, we don't know much about the files it contains. The program will just simply search all files in a BFS order. Performance of this approach depends on file distribution in said direcotry. If we are lucky, all files from the result set will be in the first searched folder. Unfortunately, we could also be really unlucky and all files from the result set will be in the last direcotry. 

So, it wouldn't be a good idea to search the same folder in a BFS order again but what should we do about it? We could remember the distribution of individual attribute names and use that. Consider this example of a possible query predicate: `a and not b`. What if we knew `a` and `b` is there is a 1000 times? Well, we don't know much about `a and not b`, since in the best case none of the files with `a` contain `b`, but in the worst case all of them do. It would be wrong to assume `a` and `b` independent without additinal information. That's why the query evaluator remembers distribution of attribute name subsets rather than individual attribute names. We can afford to do that because, in a typical situation, the number of used subsets is linear with the number of used attribute names and we don't have to wory about subsets which are not used.

### We know the distribution. What now?

We have to come up with an optimal search order of directories. The query evaluator looks at the query predicate and detemrines which attribute name subsets will likely cause the predicate to evaluate to true. Note, we still don't know for sure whether the predicate evaluates to true for given subset since it could have a form: `a = "value"`. Finally, we can just add up the numbers since they represent sizes of disjoint sets. This will be our search priority.

### Nested queries 

The compiler will automatically flatten nested queries (including query views).  

TODO: how?

### Set operators

One of the goals of the query evaluator is to deliver the first photo to the user as fast as possible. It achives this goal in many ways. For example, found photos are returnd in an arbitrary order and are sorted incrementally. Another challenge for the evaluator, in this sense, are most of the set operators (namely `intersect` and `except`). A naïve way of evaluating a query of type `A intersect B` would first evaluate `A`, then evaluate `B` and only return files which have been found by both queries. One major disadvatage is that if `A` has to search many files and most of them are not in the query result set, the user will have to wait a long time. We can make an optimization which would load files from both queries at the same time in a round-robin fashion (i.e., load the next file from `A` then load the next file from `B` and repeat). There is an even better way.

Given attributes of a photo and its path, the evaluator can determine whether it belogs to the result set of some query without reading any other files. For simple queries, we can create a regular expression whose language is exactly the set of paths to folders searched by given path pattern. Query predicate can easily be evaluated on loaded attributes. To determine whether a file is in an `intersection`, we check whether it is in both subqueries. `union` can be satisfied if it is in either subquery and for `except` it has to be in the first subquery but not in the second one. In the previous section [Nested queries](#Nested-queries) we have discused how are nested queries transformed to queries without nested subqueries. Thus, by induction, we can do this with zero additional I/O for every query.

Consider a query `A except B`. Let us denote `n` the number of files searched during evaluation of `A` and `m` the number of files searched during the evaluation of `B`. An interesting consequence of the algorithm described in previous paragraph is that we can evaluate the `A except B` query while only searching `n` files (instead of `n + m`).

## Grammar

The program uses ANTLR4 to generate query parser and lexer. 

[!code-csharp[Grammar](../../../Viewer.Query/QueryParser.g4)]