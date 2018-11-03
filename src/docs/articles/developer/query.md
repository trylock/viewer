# Viewer.Query

## Compilation

Expressions in the `where` part and the `order by` part of the query are compiled to a C# code using [Expression Trees](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/expression-trees/). Since we don't know the type of an attribute, many decisions have to be made at runtime. The generated code will therefore mostly just call runtime functions. Expression trees are used mainly for convenience (no need to write an interpreter). 

The compiled function takes an [entity](xref:Viewer.Data.IEntity) and returns a [value](xref:Viewer.Data.BaseValue). If the returned value in the `where` part of a query is null (i.e., [IsNull](xref:Viewer.Data.BaseValue#Viewer_Data_BaseValue_IsNull) is true), it is interpreted as `fales` (i.e., the entity is not included in the result).

## Execution

Queries are executed lazily by calling the [Execute](xref:Viewer.Query.IExecutableQuery) method. It returns an enumerator which loads entities. Even queries which contain `UNION`, `EXCEPT` and `INTERSECT` operators are evaluated lazily. This is possible due to the [Match](xref:Viewer.Query.IExecutableQuery) method which can determine statically (i.e., without looking at any other entity) whether or not an entity matches given query.

> [!NOTE]
> Returned entities are not sorted. This allows for a lazy execution even if the evaluated query is ordered and it drastically reduces latency since user does not have to wait for the query to search all entities.

The [Execute](xref:Viewer.Query.IExecutableQuery) method supports standard cancellation and progress even though it is not an async method and it executes completely synchronously.  

### Search order

When we first see a directory, we don't know much about the files it contains. The program will just simply search all files there in a BFS order. Performance of this approach depends on file distribution in said direcotry. If we are lucky, all files from the result set will be in the first searched folder. Unfortunately, we could also be really unlucky and all files from the result set will be in the last direcotry. 

So, it wouldn't be a good idea to search the same folder in a BFS order again but what should we do about it? We could remember the distribution of individual attribute names and use that. Consider this example of a possible query predicate: `a and not b`. What if we knew `a` and `b` is there is a 1000 times? Well, we don't know much about `a and not b`, since in the best case none of the files with `a` contain `b`, but in the worst case all of them do. It would be wrong to assume `a` and `b` independent without additinal information. That's why the query evaluator remembers distribution of attribute name subsets rather than individual attribute names. We can afford to do that because, in a typical situation, the number of used subsets is linear with the number of used attribute names and we don't have to wory about subsets which are not used.

### We know the distribution. What now?

We have to come up with an optimal search order of directories. The query evaluator looks at the query predicate and detemrines which attribute name subsets will likely cause the predicate to evaluate to true. Note, we still don't know for sure whether the predicate evaluates to true for given subset since it could have a form: `a = "value"`. We do know, however, that all subsets we have eliminated using this process will evaluate to false (unless user defines a custom function like `rand`). Finally, we can just add up the numbers since they represent sizes of disjoint sets. This will be our search priority.

## Grammar

The program uses ANTLR4 to generate query parser and lexer. 

[!code-csharp[Grammar](../../../Viewer.Query/QueryParser.g4)]