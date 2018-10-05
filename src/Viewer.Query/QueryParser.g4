parser grammar QueryParser;

options {
  tokenVocab=QueryLexer;
}

entry: queryExpression EOF;

// set operations on queries
queryExpression: queryExpression UNION_EXCEPT intersection | intersection;

intersection: queryFactor (INTERSECT queryFactor)*;

queryFactor: query | LPAREN queryExpression RPAREN;

// query
query: unorderedQuery optionalOrderBy;

unorderedQuery: SELECT source optionalWhere;

source: ID | STRING | LPAREN queryExpression RPAREN;

// WHERE
optionalWhere: WHERE predicate | ;

// ORDER BY
optionalOrderBy: ORDER BY orderByList | ;

orderByList: orderByKey (PARAM_DELIMITER orderByKey)*;

orderByKey: comparison optionalDirection;

optionalDirection: DIRECTION | ;

// expressions
predicate: predicate OR conjunction | conjunction;

conjunction: conjunction AND literal | literal; 

literal: comparison | NOT comparison;

comparison: expression REL_OP expression | expression;

expression: expression ADD_SUB multiplication | multiplication; 

multiplication: multiplication MULT_DIV factor | factor;

factor: LPAREN predicate RPAREN | INT | REAL | STRING | COMPLEX_ID | ID | ID LPAREN argumentList RPAREN;

argumentList: comparison (PARAM_DELIMITER comparison)* | ;
