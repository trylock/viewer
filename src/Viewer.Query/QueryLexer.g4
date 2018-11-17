lexer grammar QueryLexer;

SELECT: S E L E C T;

WHERE: W H E R E;

ORDER: O R D E R;

BY: B Y;

AND: A N D;

OR: O R;

NOT: N O T;

DIRECTION: (D E S C | A S C);

INTERSECT: I N T E R S E C T;

UNION_EXCEPT: (U N I O N | E X C E P T);

ID: [\p{Alpha}_][\p{Alpha}0-9_]*;

INT: [0-9]+;

REAL: [0-9]+'.'[0-9]+;

LPAREN: '(';

RPAREN: ')';

PARAM_DELIMITER: ',';

GRAVE: '`' -> more, mode(COMPLEX_ID_CONTENT);

QUOTE: '"' -> more, mode(STRING_CONTENT);

ADD_SUB: ('+' | '-');

MULT_DIV: ('*' | '/');

REL_OP: ('=' | '!=' | '<' | '<=' | '>' | '>=');

WS : [ \t\r\n]+ -> skip;

ERROR: .;

// match string content
mode STRING_CONTENT;

STRING: ('"' | '\n' | '\r' | EOF) -> mode(DEFAULT_MODE);

CONTENT: . -> more;

mode COMPLEX_ID_CONTENT;

COMPLEX_ID: ('`' | '\n' | '\r' | EOF) -> mode(DEFAULT_MODE);

COMPLEX_ID_CONTENT: . -> more;

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
