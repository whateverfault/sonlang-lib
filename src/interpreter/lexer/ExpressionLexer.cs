using sonlanglib.interpreter.error;
using sonlanglib.interpreter.executor;
using sonlanglib.interpreter.parser;
using sonlanglib.interpreter.tokenizer;
using sonlanglib.shared;
using sonlanglib.shared.trees;

namespace sonlanglib.interpreter.lexer;

internal ref struct LexerContext {
    public readonly List<ExpressionToken> Tokens;
    public bool IsArray;
    public int Pos;


    public LexerContext(List<ExpressionToken> tokens) {
        Tokens = tokens;
        IsArray = false;
        Pos = 0;
    }
}

public class ExpressionLexer {
    private readonly Interpreter _interpreter;

    private TokenParser Parser => _interpreter.Parser;
    private OperationExecutor Executor => _interpreter.Executor;
    
    
    public ExpressionLexer(Interpreter interpreter) {
        _interpreter = interpreter;
    }
    
    public Result<List<BinaryTreeNode<ExpressionToken>>?, Error?> Lex(List<ExpressionToken> tokens) {
        if (!CheckPairs(tokens)) return new Result<List<BinaryTreeNode<ExpressionToken>>?, Error?>(null, Error.InvalidSyntax);
        
        var nodes = new List<BinaryTreeNode<ExpressionToken>>();
        var context = new LexerContext(tokens);
        
        for (; context.Pos < tokens.Count;) {
            var result = ParseToken(ref context);
            if (!result.Ok) return new Result<List<BinaryTreeNode<ExpressionToken>>?, Error?>(null, result.Error);
            if (result.Value == null) return new Result<List<BinaryTreeNode<ExpressionToken>>?, Error?>(null, Error.SmthWentWrong);
            
            nodes.AddRange(result.Value.Select(token => new BinaryTreeNode<ExpressionToken>(token)));
        }
        
        return new Result<List<BinaryTreeNode<ExpressionToken>>?, Error?>(nodes, null);
    }

    private Result<List<ExpressionToken>?, Error?> ParseToken(ref LexerContext context) {
        var tokens = new List<ExpressionToken>();
        var token = context.Tokens[context.Pos];
        
        if (token.Type == ExpressionTokenType.LeftBracket) {
            var parseArrayResult = ParseArray(ref context);
            if (!parseArrayResult.Ok) return new Result<List<ExpressionToken>?, Error?>(null, parseArrayResult.Error);
            if (parseArrayResult.Value == null) return new Result<List<ExpressionToken>?, Error?>(null, Error.SmthWentWrong);
            
            tokens.Add(parseArrayResult.Value);
        } else {
            if (token.Type != ExpressionTokenType.RightBracket) {
                tokens.Add(context.Tokens[context.Pos++]);
            }
        }
        
        return new Result<List<ExpressionToken>?, Error?>(tokens, null);
    }

    private Result<ExpressionToken?, Error?> ParseArray(ref LexerContext context) {
        var tokens = new List<ExpressionToken>();

        context.IsArray = false;
        for (; context.Pos < context.Tokens.Count;) {
            var t = context.Tokens[context.Pos];

            switch (t.Type) {
                case ExpressionTokenType.LeftBracket: {
                    if (context.IsArray) break;
                    context.IsArray = true;
                    ++context.Pos;
                    break;
                }
                case ExpressionTokenType.RightBracket: {
                    if (!context.IsArray) break;
                    context.IsArray = false;
                    break;
                }
            }

            if (!context.IsArray) break;
            if (context.Pos >= context.Tokens.Count) {
                return new Result<ExpressionToken?, Error?>(null, Error.InvalidSyntax);
            }
            
            var quit = false;
            var tempTokens = new List<ExpressionToken>();
            
            do {
                var result = ParseToken(ref context);
                if (!result.Ok) return new Result<ExpressionToken?, Error?>(null, result.Error);
                if (result.Value == null) return new Result<ExpressionToken?, Error?>(null, Error.SmthWentWrong);
                if (context.Pos >= context.Tokens.Count) {
                    return new Result<ExpressionToken?, Error?>(null, Error.InvalidSyntax);
                }
                
                if (context.Tokens[context.Pos].Type == ExpressionTokenType.RightBracket) {
                    context.IsArray = false;
                    quit = true;
                } if (result.Value.Any(x => x.Type == ExpressionTokenType.Comma)) {
                    quit = true;
                }
                
                tempTokens.AddRange(result.Value.Where(x => x.Type != ExpressionTokenType.Comma));
            } while (!quit && context.Pos < context.Tokens.Count);
            
            var parseResult = Parser.Parse(tempTokens.Select(x => new BinaryTreeNode<ExpressionToken>(x)).ToList());
            if (!parseResult.Ok) return new Result<ExpressionToken?, Error?>(null, parseResult.Error);
            if (parseResult.Value == null) continue;
            
            var executionResult = Executor.Execute(parseResult.Value);
            if (!executionResult.Ok) return new Result<ExpressionToken?, Error?>(null, executionResult.Error);
            if (executionResult.Value == null) return new Result<ExpressionToken?, Error?>(null, Error.SmthWentWrong);

            tokens.Add(executionResult.Value);
        }
        
        if (context.IsArray) return new Result<ExpressionToken?, Error?>(null, Error.InvalidSyntax);

        ++context.Pos;
        var token = new ExpressionToken(tokens);
        return new Result<ExpressionToken?, Error?>(token, null);
    }

    private bool CheckPairs(List<ExpressionToken> tokens) {
        var parentheses = tokens.FindAll(x => x.Type == ExpressionTokenType.LeftParenthesis).Count 
                        + tokens.FindAll(x => x.Type == ExpressionTokenType.RightParenthesis).Count;
        if (parentheses % 2 != 0) return false;
        
        var brackets = tokens.FindAll(x => x.Type == ExpressionTokenType.LeftBracket).Count 
                     + tokens.FindAll(x => x.Type == ExpressionTokenType.RightBracket).Count;
        if (brackets % 2 != 0) return false;

        return true;
    }
}