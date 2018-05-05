grammar Query;

// query
query: SELECT source optionalWhere EOF |;

source: PATH_PATTERN | '(' query ')';

optionalWhere: WHERE comparison | ;

// expressions
comparison: expression REL_OP expression | expression;

expression: expression ADD_SUB multiplication | multiplication; 

multiplication: multiplication MULT_DIV factor | factor;

factor: '(' comparison ')' | ID | INT | REAL;

// lexer
SELECT: 'SELECT';

WHERE: 'WHERE';

PATH_PATTERN: '"' ~('\n' | '\r' | '"')+ '"';

ID: [a-zA-Z_][a-zA-Z0-9_]*;

INT: [0-9]+;

REAL: [0-9]+'.'[0-9]+;

ADD_SUB: ('+' | '-');

MULT_DIV: ('*' | '/');

REL_OP: ('=' | '!=' | '<' | '<=' | '>' | '>=');

WS : [ \t\r\n]+ -> skip ;