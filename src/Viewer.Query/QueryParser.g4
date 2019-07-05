parser grammar QueryParser;

options {
  tokenVocab=QueryLexer;
}

entry: queryExpression EOF;

// set operations on queries
queryExpression: intersection (UNION_EXCEPT intersection)*;

intersection: queryFactor (INTERSECT queryFactor)*;

queryFactor: query | LPAREN queryExpression RPAREN;

// query
query: unorderedQuery optionalOrderBy optionalGroupBy;

unorderedQuery: SELECT source optionalWhere;

source: ID | COMPLEX_ID | STRING | LPAREN queryExpression RPAREN;

// WHERE
optionalWhere: WHERE predicate | ;

// ORDER BY
optionalOrderBy: ORDER BY orderByList | ;

orderByList: orderByKey (PARAM_DELIMITER orderByKey)*;

orderByKey: predicate DIRECTION?;

// GROUP BY
optionalGroupBy: GROUP BY predicate | ;

// expressions
predicate: conjunction (OR conjunction)*;

conjunction: literal (AND literal)*; 

literal: comparison | NOT comparison;

comparison: expression (REL_OP expression)?;

expression: multiplication (ADD_SUB multiplication)*;

multiplication: factor (MULT_DIV factor)*;

factor: LPAREN predicate RPAREN | INT | REAL | STRING | COMPLEX_ID  (LPAREN argumentList RPAREN)? | ID (LPAREN argumentList RPAREN)? | ADD_SUB factor;

argumentList: comparison (PARAM_DELIMITER predicate)* | ;
