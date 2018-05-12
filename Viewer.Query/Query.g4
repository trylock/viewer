grammar Query;

// set operations on queries
queryExpression: queryExpression UNION_EXCEPT intersection | intersection;

intersection: queryFactor (INTERSECT queryFactor)*;

queryFactor: query | '(' queryExpression ')';

// query
query: unorderedQuery optionalOrderBy;

unorderedQuery: SELECT source optionalWhere;

source: STRING | '(' query ')';

optionalWhere: WHERE predicate | ;

optionalOrderBy: ORDER BY orderByList | ;

// expressions
orderByList: orderByKey (',' orderByKey)*;

orderByKey: comparison optionalDirection;

optionalDirection: DIRECTION | ;

predicate: predicate OR conjunction | conjunction;

conjunction: conjunction AND comparison | comparison; 

comparison: expression REL_OP expression | expression;

expression: expression ADD_SUB multiplication | multiplication; 

multiplication: multiplication MULT_DIV factor | factor;

factor: '(' comparison ')' | INT | REAL | STRING | COMPLEX_ID | ID | ID '(' argumentList ')';

argumentList: comparison (',' comparison)* | ;

// lexer
SELECT: S E L E C T;

WHERE: W H E R E;

ORDER: O R D E R;

BY:  B Y;

AND: A N D;

OR: O R;

DIRECTION: (D E S C | A S C);

INTERSECT: I N T E R S E C T;

UNION_EXCEPT: (U N I O N | E X C E P T);

ID: [a-zA-Z_][a-zA-Z0-9_]*;

INT: [0-9]+;

REAL: [0-9]+'.'[0-9]+;

COMPLEX_ID: '`' ~('`')+ '`';

STRING: '"' ~('"')* '"'; 

ADD_SUB: ('+' | '-');

MULT_DIV: ('*' | '/');

REL_OP: ('=' | '!=' | '<' | '<=' | '>' | '>=');

WS : [ \t\r\n]+ -> skip;

// fragments
fragment A: [aA];
fragment B: [bB];
fragment C: [cC];
fragment D: [dD];
fragment E: [eE];
fragment F: [fF];
fragment G: [gG];
fragment H: [hH];
fragment I: [iI];
fragment J: [jJ];
fragment K: [kK];
fragment L: [lL];
fragment M: [mM];
fragment N: [nN];
fragment O: [oO];
fragment P: [pP];
fragment Q: [qQ];
fragment R: [rR];
fragment S: [sS];
fragment T: [tT];
fragment U: [uU];
fragment V: [vV];
fragment W: [wW];
fragment X: [xX];
fragment Y: [yY];
fragment Z: [zZ];