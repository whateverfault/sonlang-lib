using sonlanglib.interpreter.data.vars;

namespace sonlanglib.interpreter.tokenizer;

public enum ExpressionTokenType {
    Name,
    Number,
    Bool,
    String,
    Array,
    Reference,
    Index,
    Assigment,
    Operation,
    RightParenthesis,
    LeftParenthesis,
    LeftBracket,
    RightBracket,
    Comma,
    Semicolon,
}

public class ExpressionToken {
    public ExpressionTokenType Type;
    
    public List<ExpressionToken> Next; 

    public Value Value { get; set; }


    public ExpressionToken(string value, ExpressionTokenType type) {
        Type = type;
        Value = new Value(value, type);
        Next = [];
    }
    
    public ExpressionToken(List<ExpressionToken> tokens, ExpressionTokenType type = ExpressionTokenType.Array) {
        Type = type;
        Next = tokens;
        Value = tokens.Count > 0?
                    tokens[0].Value :
                    new Value(string.Empty, ExpressionTokenType.String);
    }
    
    public ExpressionToken(Value value) {
        Type = value.Type;
        Next = [];
        Value = value;
    }
}