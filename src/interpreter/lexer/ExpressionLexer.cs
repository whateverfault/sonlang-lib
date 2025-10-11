using System.Text;
using sonlanglib.interpreter.conversion;
using sonlanglib.interpreter.error;
using sonlanglib.shared;
using sonlanglib.shared.trees;

namespace sonlanglib.interpreter.lexer;

public class ExpressionLexer {
    private readonly TypeConverter _converter;
    
    
    public ExpressionLexer(TypeConverter converter) {
        _converter = converter;
    }
    
    public Result<List<BinaryTreeNode<ExpressionToken>>?, Error?> Lex(string expression) {
        var tokens = new List<BinaryTreeNode<ExpressionToken>>();
        var sb = new StringBuilder();
        
        for (var pos = 0; pos < expression.Length;) {
            while (pos < expression.Length && char.IsWhiteSpace(expression[pos])) ++pos;

            var isDigit = false;
            while (pos < expression.Length && (char.IsDigit(expression[pos]) || expression[pos] == '.')) {
                sb.Append(expression[pos++]);
                isDigit = true;
            }

            if (isDigit) {
                AddToken(tokens, sb);
            } else {
                var result = ParseNonNumberToken(expression, out var end, pos);
                if (!result.Ok) return new Result<List<BinaryTreeNode<ExpressionToken>>?, Error?>(null, result.Error);
                if (result.Value == null) return new Result<List<BinaryTreeNode<ExpressionToken>>?, Error?>(null, Error.SmthWentWrong);

                pos = end;
                tokens.AddRange(result.Value);
            }
        }
        
        return new Result<List<BinaryTreeNode<ExpressionToken>>?, Error?>(tokens, null);
    }

    private Result<List<BinaryTreeNode<ExpressionToken>>?, Error?> ParseNonNumberToken(string expression, out int end, int pos) {
        end = pos;
        
        var sb = new StringBuilder();
        var tokens = new List<BinaryTreeNode<ExpressionToken>>();
        
        var isArithmetic = IsArithmetic(expression[pos]);
        for (; pos < expression.Length; ++pos) {
            var c = expression[pos];
            
            if (IsArithmetic(expression[pos]) != isArithmetic
              ||char.IsWhiteSpace(expression[pos])) break;
            
            switch (c) {
                case '"' or '\'': {
                    AddToken(tokens, sb);
                
                    var result = ParseString(expression, out end, pos);
                    if (!result.Ok) return new Result<List<BinaryTreeNode<ExpressionToken>>?, Error?>(null, result.Error);
                    if (result.Value == null) return new Result<List<BinaryTreeNode<ExpressionToken>>?, Error?>(null, Error.SmthWentWrong);
                
                    pos = end;
                    AddToken(tokens, result.Value, isString: true); break;
                }
                case ';' or '(' or ')':
                    AddToken(tokens, sb);
                    sb.Append(expression[pos]);
                    AddToken(tokens, sb); break;
                default: {
                    sb.Append(expression[pos]); break;
                }
            }
        }
        
        AddToken(tokens, sb);
        end = pos;
        
        return new Result<List<BinaryTreeNode<ExpressionToken>>?, Error?>(tokens, null);
    }

    private Result<StringBuilder?, Error?> ParseString(string expression, out int end, int pos) {
        var sb = new StringBuilder();
        end = pos;
        
        var isStringDouble = false;
        var isStringSingle = false;

        for (; pos < expression.Length; ++pos) {
            var c = expression[pos];
            end = pos;
            
            if (!isStringSingle && c == '"') {
                isStringDouble = !isStringDouble; 
                if (!isStringDouble) return new Result<StringBuilder?, Error?>(sb, null);
                continue;
            } if (!isStringDouble && c == '\'') {
                isStringSingle = !isStringSingle;
                if (!isStringSingle) return new Result<StringBuilder?, Error?>(sb, null);
                continue;
            }
            
            if (isStringDouble || isStringSingle) sb.Append(c);
        }
        
        return new Result<StringBuilder?, Error?>(null, Error.InvalidSyntax);
    }
    
    private void AddToken(List<BinaryTreeNode<ExpressionToken>> tokens, StringBuilder token, bool isString = false) {
        if (token.Length <= 0 && !isString) return;

        var tokenVal = token.Length <= 0? string.Empty : token.ToString();
        var type = ExpressionTokenType.String;
        if (!isString) {
            type = _converter.ImplyTokenType(tokenVal);
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