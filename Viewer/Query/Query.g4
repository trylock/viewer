grammar Query;

// query
query: SELECT source optionalWhere optionalOrderBy EOF |;

source: STRING | '(' query ')';

optionalWhere: WHERE comparison | ;

optionalOrderBy: ORDERBY orderByList | ;

// expressions
orderByList: orderByKey (',' orderByKey)*;

orderByKey: comparison optionalDirection;

optionalDirection: DIRECTION | ;

comparison: expression REL_OP expression | expression;

expression: expression ADD_SUB multiplication | multiplication; 

multiplication: multiplication MULT_DIV factor | factor;

factor: '(' comparison ')' | INT | REAL | STRING | ID | ID '(' argumentList ')';

argumentList: comparison (',' comparison)* | ;

// lexer
SELECT: 'SELECT';

WHERE: 'WHERE';

ORDERBY: 'ORDER BY';

DIRECTION: ('DESC' | 'ASC');

ID: [a-zA-Z_][a-zA-Z0-9_]*;

INT: [0-9]+;

REAL: [0-9]+'.'[0-9]+;

STRING: '"' ~('"')* '"'; 

ADD_SUB: ('+' | '-');

MULT_DIV: ('*' | '/');

REL_OP: ('=' | '!=' | '<' | '<=' | '>' | '>=');

WS : [ \t\r\n]+ -> skip ;