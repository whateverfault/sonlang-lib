using System.Text;
using sonlanglib.interpreter.error;
using sonlanglib.shared;
using sonlanglib.shared.trees;

namespace sonlanglib.interpreter.lexer;

public class ExpressionLexer {
    private readonly Interpreter _interpreter;
    
    
    public ExpressionLexer(Interpreter interpreter) {
        _interpreter = interpreter;
    }
    
    public Result<List<BinaryTreeNode<ExpressionToken>>?, Error?> Lex(string expression) {
        var tokens = new List<BinaryTreeNode<ExpressionToken>>();
        var sb = new StringBuilder();

        var isString = false;
        var isChar = false;
        for (var pos = 0; pos < expression.Length;) {
            while (pos < expression.Length && char.IsWhiteSpace(expression[pos])) ++pos;

            var isDigit = false;
            while (pos < expression.Length && (char.IsDigit(expression[pos]) || expression[pos] == '.')) {
                sb.Append(expression[pos++]);
                isDigit = true;
            }

            if (!isDigit) {
                var isSymbol = IsArithmetic(expression[pos]);
                while (pos < expression.Length 
                    && IsArithmetic(expression[pos]) == isSymbol
                    && !char.IsWhiteSpace(expression[pos])) {
                    var c = expression[pos];
                    if (c is ';' or '(' or ')') {
                        AddToken(tokens, sb);
                        sb.Append(expression[pos++]);
                        AddToken(tokens, sb);
                        continue;
                    }
                    
                    if (!isChar && c == '"') {
                        if (!isString) AddToken(tokens, sb);
                        isString = !isString; 
                        if (!isString) AddToken(tokens, sb, !isString);
                        ++pos; continue;
                    }
                    if (!isString && c == '\'') {
                        if (!isChar) AddToken(tokens, sb);
                        isChar = !isChar; 
                        if (!isChar) AddToken(tokens, sb, !isString);
                        ++pos; continue;
                    }
                    sb.Append(expression[pos++]);
                }
            }

            AddToken(tokens, sb);
        }

        if (isString || isChar) return new Result<List<BinaryTreeNode<ExpressionToken>>?, Error?>(null, Error.InvalidSyntax);
        return new Result<List<BinaryTreeNode<ExpressionToken>>?, Error?>(tokens, null);
    }

    private void AddToken(List<BinaryTreeNode<ExpressionToken>> tokens, StringBuilder token, bool isString = false) {
        if (token.Length <= 0) return;

        var tokenVal = token.ToString();
        var type = ExpressionTokenType.String;
        if (!isString) {
            type = _interpreter.ImplyTokenType(tokenVal);
        }
            
        tokens.Add(new BinaryTreeNode<ExpressionToken>(new ExpressionToken(type, tokenVal)));
        token.Clear();
    }

    private bool IsArithmetic(char c) {
        return c switch {
                   '%' or '^' or '/' or '*' or '-' or '+' or '|' or '&' or '<' or '>' or '!' or '=' => true,
                   _                                                                                => false,
               };
    }
}