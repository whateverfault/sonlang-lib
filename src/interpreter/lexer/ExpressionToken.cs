using sonlanglib.interpreter.data;

namespace sonlanglib.interpreter.lexer;

public enum ExpressionTokenType {
    Name,
    Number,
    Bool,
    String,
    Array,
    Reference,
    SpecialOperation,
    Operation,
    ClosingParenthesis,
    OpeningParenthesis,
    Comma,
    Semicolon,
}

public class ExpressionToken {
    public ExpressionTokenType Type;
    
    public List<Value> Values;

    public Value Value {
        get => Values[0];
        set => Values = [value,];
    }


    public ExpressionToken(string value, ExpressionTokenType type) {
        Type = type;
        Values = [new Value(value, type),];
    }
    
    public ExpressionToken(List<ExpressionToken> tokens) {
        Type = ExpressionTokenType.Array;
        Values = [];
        
        foreach (var token in tokens) {
            Values.AddRange(token.Values);
        }
    }
    
    public ExpressionToken(List<Value> values, ExpressionTokenType type) {
        Type = type;
        Values = values;
    }
    
    public ExpressionToken(Value value) {
        Type = value.Type;
        Values = [value,];
    }
}