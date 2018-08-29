# Viewer.Query

## Grammar

## Compilation

Expressions in the `where` part and the `order by` part of the query are compiled to a C# code using [Expression Trees](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/expression-trees/). Since we don't know the type of an attribute, many decisions have to be made at runtime. The generated code will therefore mostly just call runtime functions. Expression trees are used mainly for convenience (no need to write an interpreter). 

The compiled function takes an [entity](xref:Viewer.Data.IEntity) and returns a [value](xref:Viewer.Data.BaseValue). If the returned value in the `where` part of a query is null (i.e., [IsNull](xref:Viewer.Data.BaseValue#Viewer_Data_BaseValue_IsNull) is true), it is interpreted as `fales` (i.e., the entity is not included in the result).

## Execution

Queries are executed lazily by calling the [Execute](xref:Viewer.Query.IExecutableQuery) method. It returns an enumerator which loads entities. Even queries which contain `UNION`, `EXCEPT` and `INTERSECT` operators are evaluated lazily. This is possible due to the [Match](xref:Viewer.Query.IExecutableQuery) method which can determine statically (i.e., without looking at any other entity) whether or not an entity matches given query.

> [!NOTE]
> Returned entities are not sorted. This allows for a lazy execution even if the evaluated query is ordered and it drastically reduces latency since user does not have to wait for the query to search all entities.

The [Execute](xref:Viewer.Query.IExecutableQuery) method supports standard cancellation and progress even though it is not an async method and it executes completely synchronously.  
