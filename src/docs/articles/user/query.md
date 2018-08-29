# Query Language

Viewer Query Language can be used to query files in the file system. All keywords and function identifiers are case insensitive (i.e., `select` and `SelECt` is the same keywrod).

## Examples

Here are some examples of queries to quickly introduce you to the VQL. If you don't understand these examples, feel free to skip this part to the query documentation which describes all parts of a query in detail.

Find all photos in the `D:/photos/2018` directory: 
```SQL 
select "d:/photos/2018"
```

Find all photos in the `D:/photos/2018` directory and all its subdirectories:
```SQL
select "d:/photos/2018/**"
```

Find all photos in subdirectoris of `D:/photos` whose name contain `vacation`: 

```SQL
select "d:/photos/2018/**/*vacation*"
```

Find all photos in a city (i.e., which have an attribute named `city`):
```SQL
select "d:/photos/**" where city
```

Find all photos from Edinburgh: 
```SQL
select "d:/photos/**" where city = "Edinburgh"
```

Find all photos taken between August 1st and August 28th in 2018: 

```sql
select "d:/photos/**"
where DateTaken >= date("2018-08-01") and DateTaken <= date("2018-8-28")
```

Find all photos from Ireland except for photos in the `d:/photos/pending` directory and order them by the time of their creation from the newest to the oldest:

```sql
select (
    select "d:/photos/**" where place = "Ireland"
        except
    select "d:/photos/pending"
)
order by DateTaken desc
```

## `select`

Each query starts with the `select` keyword after which can be a directory path pattern. This pattern determines which directories will be searched. It can contain some special characters (for API, see [FileFinder](xref:Viewer.IO.FileFinder)):

1) `*`: matches a sequence of characters except for a directory separator (`/` or `\`). For example, `x` `xa`, `xab` are all matched by `x*`. On the other hand, it does not match `ya` since that does not start with `x`, nor does it match `xa/b` since that contains a directory separator.
2) `?`: matches any character except for a directory separator. For example, the pattern `a?b` matches `axb`, `ayb` but it does not match `ab` or `axyb` since `?` has to be replace with exactly one character.
3) `**`: matches a sequence of characters including a directory separator. For example, `x/y`, `x/a/y` and `x/a/b/y` are all matched by `x/**/y`. The pattern does not match `x/a` since that does not end with `y`. 

### Examples of `select` query

- `select "d:/photos/**"` select all photos and directories in the photos folder and all subfolders.
- `select "d:/photos/**/*cat*"`select photos and directories from folders which contain `cat` in their name and they are in the `d:/photos` directory tree.

## `where`

While you can find many photos with `select` on its own, it's not always good enough. You might want to find photos which are scattered throughout the whole file system. The `where` keyword will help you. It is optional and it follows the `select` part. You can specify which photos you are interested in. The condition can be as simple as a name of an attribute the photo should have but it can be much more complex.

### Values

The program works with several value types:

- a string (text wrapped in quotes, e.g. `"string value"`)
- a number (integer or a real number, e.g. `42` or `3.14159`) 
- a date&time (you have to use functions to produce a date&time value in a query, e.g. `date("2018-08-28")` is the date: August 8th 2018)

There is no boolean type. Instead, all values can be `null` which means the value is missing and this is interpreted as `false`. 

To get value of an attribute in a query expression, simply type its name (e.g. `city` will evaluate as a value of the attribute named `city` or `null` if there is no attribute named `city`).

### Functions

Function has a name and parameters. It takes the parameters and produces a single value as a result. In a query, you call a function by typing its name followed by arguments in parentheses separated by comma (e.g. `func(1, "test")` would call function called `func` with 2 parameters: a number `1` and a string `"test"`). There are several functions in current implementation (see [IFunction](xref:Viewer.Query.IFunction) if you want to implement a custom function).

### Comparison operators

Values can be compared using the following operators. Comparison operators are non-associative (i.e., `1 = 2 = 3` is not allowed, you can only compare 2 values).

- `=` (is equal to), for example: `city = "Edinburgh"` (finds all photos from Edinburgh)
- `<=` (is less than or equal to), for example `DateTaken <= date("2018-08-28")` (finds all photos taken before August 29th 2018, i.e., it includes photos from August 28th)
- `<` (is less than), for example `DateTaken < date("2018-08-28")` (finds all photos taken before August 28th 2018)
- `>=` (is greater than or equal to), for example `DateTaken >= date("2018-08-28")` (finds all photos taken after August 27th 2018, i.e, it includes photos from August 28th)
- `>` (is greater than), for example `DateTaken > date("2018-08-28")` (finds all photos taken after August 28th 2018)
- `!=` (is not equal to), for example `city != "Amsterdam"` (finds all photos which are not from Amsterdam)

Notice, you can compute a value using an expression (e.g. `1 + 1 = 2`, `date("2018-08-28") > date("2018-08-27")` are all valid comparisons whose result is `true`)

### Arithmetic operators

You can use arithmetic operators `+`, `-`, `*`, `/` with number types. For example, `1 + 3` produces an integer value `4`, `3.14159 + 1` produces a real value `4.14159`. Moreover, the `+` operator can be used with strings and it concatenates them (i.e., `"a" + "b" = "ab"`). 

`*` and `/` take precedence over `+` and `-`. For example, `3 * 2 + 1` is evaluated as `(3 * 2) + 1 = 7` and `2.5 / 0.5 - 0.5` is evaluated as `(2.5 / 0.5) - 0.5 = 4.5`.

Operators with the same precedence (`*`, `/` and `+`, `-`) are left associative. This means, that they are evaluated from the left: `3 - 2 + 1` is evaluated as `(3 - 2) + 1 = 2` and `4 / 2 * 0` is evaluated as `(4 / 2) * 0 = 0`

### Logic operators

You can use operators `and`, `or`, `not` to create quite complex queries. From these operators, `not` has the highest precedence, `and` follows and then `or`. For example, `not a and b or c` is evaluated as `((not a) and b) or c` (i.e., in order for a file to match this expression, it either has an attribute called `c` or it has `b` and does not have `a`)

### Implicit value conversions

Whenever you use an operator or a function with parameters whose types don't match the function or operator definition, the values have to be converted. For example, you can use `1 + "string"` in a query expression yet there is no `+` operator which can process integer and string parameters. In this case, types of parameters have to be converted. The program will convert some types on its own. See the following list:

- `integer` type can be converted to a `string` or a `real` number. If the query evaluator can choose from these 2 conversions, it will prefer the convertion to `real` since that preserves the information that we are working with a number.
- `real` type can be converted to a `string`. There is no implicit conversion to `integer` since if the number had a decimal part, we would lose this information.
- `string` type can only be converted to `string`
- `DateTime` type can be converted to a `string`

If there is no suitable implicit conversion, a `null` value will be used. 

### Examples of `where` query

- `select "d:/photos/**" where city` finds photos of a city
- `select "d:/photos/**" where city = "Edinburgh"` finds photos from Edinburgh

## `order by`

Queries can be ordered by multiple keys. Keys are specified in the optional `order by` part of the query. You simply write a list of expressions (same as in the `where` part) separated by comma. There can be a sort direction (`desc` for descending, `asc` for asceding) after each expression. Sort direction is optional and it is `asc` by default. 

### Examples of `order by` queries

Sort files by their size from the largest to the smallest: 
```SQL
select "d:/photos" order by FileSize desc
```

Sort files by their directory name. If 2 files are in the same directory, sort the newest photos first: 

```SQL
select "d:/photos/**" order by Directory, DateTaken desc
```