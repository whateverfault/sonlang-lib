using sonlanglib.interpreter.tokenizer;

namespace sonlanglib.interpreter.data.vars;

public class Value {
    public string Val;
    public ExpressionTokenType Type;


    public Value(string val, ExpressionTokenType type) {
        Val = val;
        Type = type;
    }
}