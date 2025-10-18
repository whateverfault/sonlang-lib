using sonlanglib.interpreter.data.vars;

namespace sonlanglib.interpreter.tokenizer;

public enum ExpressionTokenType {
    None,
    Name,
    Number,
    Bool,
    String,
    Array,
    Reference,
    Assigment,
    Operation,
    RightParenthesis,
    LeftParenthesis,
    LeftBracket,
    RightBracket,
    If,
    Elif,
    Else,
    Separator,
    ConditionEnd,
    StatementEnd,
    IfEnd,
}

public class ExpressionToken {
    
    public ExpressionTokenType Type;
    public List<ExpressionToken> Next; 

    public Value Value { get; set; }

    public static ExpressionToken Empty { get; } = new ExpressionToken(string.Empty, ExpressionTokenType.None);

    
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