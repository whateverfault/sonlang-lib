using sonlanglib.interpreter.lexer;

namespace sonlanglib.interpreter.data;

public class Value {
    public string Val;
    public ExpressionTokenType Type;


    public Value(string val, ExpressionTokenType type) {
        Val = val;
        Type = type;
    }
}