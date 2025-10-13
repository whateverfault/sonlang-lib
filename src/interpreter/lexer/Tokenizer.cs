using System.Text;
using sonlanglib.interpreter.conversion;
using sonlanglib.interpreter.error;
using sonlanglib.interpreter.executor;
using sonlanglib.interpreter.parser;
using sonlanglib.shared;
using sonlanglib.shared.trees;

namespace sonlanglib.interpreter.lexer;

public class Tokenizer {
    private readonly Interpreter _interpreter;
    
    private TypeConverter Converter => _interpreter.TypeConverter;
    private TokenParser Parser => _interpreter.Parser;
    private OperationExecutor Executor => _interpreter.Executor;
    
    
    public Tokenizer(Interpreter interpreter) {
        _interpreter = interpreter;
    }
    
    public Result<List<BinaryTreeNode<ExpressionToken>>?, Error?> Tokenize(string expression) {
        var tokens = new List<BinaryTreeNode<ExpressionToken>>();
        
        for (var pos = 0; pos < expression.Length;) {
            var result = ParseToken(expression, out var end, pos);
            if (!result.Ok) return new Result<List<BinaryTreeNode<ExpressionToken>>?, Error?>(null, result.Error);
            if (result.Value == null) return new Result<List<BinaryTreeNode<ExpressionToken>>?, Error?>(null, Error.SmthWentWrong);
            
            pos = end;
            tokens.AddRange(result.Value.Select(token => new BinaryTreeNode<ExpressionToken>(token)));
        }
        
        return new Result<List<BinaryTreeNode<ExpressionToken>>?, Error?>(tokens, null);
    }

    private Result<List<ExpressionToken>?, Error?> ParseToken(string expression, out int end, int pos) {
        var tokens = new List<ExpressionToken>();
        var sb = new StringBuilder();
        
        end = pos;
        
        while (pos < expression.Length && char.IsWhiteSpace(expression[pos])) ++pos;
        
        var isDigit = false;
        while (pos < expression.Length && (char.IsDigit(expression[pos]) || expression[pos] == '.')) {
            sb.Append(expression[pos++]);
            isDigit = true;
        }
        
        if (isDigit) {
            AddToken(tokens, sb);
            end = pos;
        } else {
            if (expression[pos] == '[') {
                var parseArrayResult = ParseArray(expression, out end, pos);
                if (!parseArrayResult.Ok) return new Result<List<ExpressionToken>?, Error?>(null, parseArrayResult.Error);
                if (parseArrayResult.Value == null) return new Result<List<ExpressionToken>?, Error?>(null, Error.SmthWentWrong);
                
                tokens.Add(parseArrayResult.Value);
            } else {
                var parseTokenResult = ParseNonNumberToken(expression, out end, pos);
                if (!parseTokenResult.Ok) return new Result<List<ExpressionToken>?, Error?>(null, parseTokenResult.Error);
                if (parseTokenResult.Value == null) return new Result<List<ExpressionToken>?, Error?>(null, Error.SmthWentWrong);

                tokens.AddRange(parseTokenResult.Value);
            }
        }
        
        return new Result<List<ExpressionToken>?, Error?>(tokens, null);
    }
    
    private Result<List<ExpressionToken>?, Error?> ParseNonNumberToken(string expression, out int end, int pos) {
        end = pos;
        
        var sb = new StringBuilder();
        var tokens = new List<ExpressionToken>();
        
        var isOperation = IsOperation(expression[pos]);
        var isSpecial = IsSpecial(expression[pos]);

        if (isOperation) {
            var op = ParseOperation(expression, out end, pos);
            if (op == null) return new Result<List<ExpressionToken>?, Error?>(null, Error.IllegalOperation);
            
            AddToken(tokens, op);
            return new Result<List<ExpressionToken>?, Error?>(tokens, null);
        }
        
        for (; pos < expression.Length; ++pos) {
            var c = expression[pos];
            
            if (IsOperation(expression[pos]) != isOperation
             || IsSpecial(expression[pos]) != isSpecial
             || char.IsWhiteSpace(expression[pos])) break;
            
            switch (c) {
                case '"' or '\'': {
                    AddToken(tokens, sb);
                
                    var result = ParseString(expression, out end, pos);
                    if (!result.Ok) return new Result<List<ExpressionToken>?, Error?>(null, result.Error);
                    if (result.Value == null) return new Result<List<ExpressionToken>?, Error?>(null, Error.SmthWentWrong);
                
                    pos = end;
                    AddToken(tokens, result.Value, isString: true); break;
                }
                case ';' or '(' or ')' or ',':
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
        
        return new Result<List<ExpressionToken>?, Error?>(tokens, null);
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

    private Result<ExpressionToken?, Error?> ParseArray(string expression, out int end, int pos) {
        var tokens = new List<ExpressionToken>();
        end = pos;
        
        var array = false;
        for (; pos < expression.Length; ++pos) {
            var c = expression[pos];
            end = pos;

            switch (c) {
                case '[': {
                    array = true;
                    ++pos;
                    break;
                }
                case ']': {
                    array = false;
                    break;
                }
            }

            if (!array) break;

            var quit = false;
            var tempTokens = new List<ExpressionToken>();
            
            do {
                var result = ParseToken(expression, out end, pos);
                if (!result.Ok) return new Result<ExpressionToken?, Error?>(null, result.Error);
                if (result.Value == null) return new Result<ExpressionToken?, Error?>(null, Error.SmthWentWrong);

                pos = end;
                if (expression[pos] == ']') {
                    array = false;
                    quit = true;
                }
                if (result.Value.Any(x => x.Value.Type == ExpressionTokenType.Comma)) quit = true;
                tempTokens.AddRange(result.Value.Where(x => x.Value.Type != ExpressionTokenType.Comma));
            } while (!quit);

            pos = end - 1;
            var parseResult = Parser.Parse(tempTokens.Select(x => new BinaryTreeNode<ExpressionToken>(x)).ToList());
            if (!parseResult.Ok) return new Result<ExpressionToken?, Error?>(null, parseResult.Error);
            if (parseResult.Value == null) return new Result<ExpressionToken?, Error?>(null, Error.SmthWentWrong);
            
            var executionResult = Executor.Execute(parseResult.Value);
            if (!executionResult.Ok) return new Result<ExpressionToken?, Error?>(null, executionResult.Error);
            if (executionResult.Value == null) return new Result<ExpressionToken?, Error?>(null, Error.SmthWentWrong);

            tokens.Add(executionResult.Value);
        }
        
        if (array) return new Result<ExpressionToken?, Error?>(null, Error.InvalidSyntax);

        ++end;
        var token = new ExpressionToken(tokens);
        return new Result<ExpressionToken?, Error?>(token, null);
    }

    private StringBuilder? ParseOperation(string expression, out int end, int pos) {
        var sb = new StringBuilder();
        end = pos;
        
        for (; pos < expression.Length; ++pos) {
            end = pos;
            if (!IsOperation(expression[pos])) break;
            
            sb.Append(expression[pos]);
        }
        
        do {
            if (Converter.IsOperation(sb.ToString())) break;
            sb.Remove(sb.Length-1, 1); --end;
        } while (true);

        return sb.Length > 0?
                   sb :
                   null;
    }
    
    private void AddToken(List<ExpressionToken> tokens, StringBuilder token, bool isString = false) {
        if (token.Length <= 0 && !isString) return;

        var tokenVal = token.Length <= 0? string.Empty : token.ToString();
        var type = ExpressionTokenType.String;
        if (!isString) type = Converter.ImplyTokenType(tokenVal);
        
        tokens.Add(new ExpressionToken(tokenVal, type));
        token.Clear();
    }
    
    private bool IsOperation(char c) {
        return c switch {
                   '%' or '^' or '/' or '*' or '-' or '+' or '|' or '&' or '<' or '>' or '!' or '=' => true,
                   _                                                                                => false,
               };
    }
    
    private bool IsSpecial(char c) {
        return c switch {
                   ',' or ';' or '[' or ']' => true,
                   _                        => false,
               };
    }
}