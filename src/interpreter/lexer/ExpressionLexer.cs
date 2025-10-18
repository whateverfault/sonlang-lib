using sonlanglib.interpreter.conversion;
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

    public ExpressionToken Prev => Tokens[Pos - 1];
    public ExpressionToken Current => Tokens[Pos];
    public ExpressionToken Next => Tokens[Pos + 1];


    public LexerContext(List<ExpressionToken> tokens) {
        Tokens = tokens;
        IsArray = false;
        Pos = 0;
    }
}

public class ExpressionLexer {
    private readonly Interpreter _interpreter;

    private TokenParser Parser => _interpreter.Parser;
    private TypeConverter Converter => _interpreter.TypeConverter;
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

            Process(nodes, result.Value.Data);
            nodes.Add(result.Value);
        }
        
        return new Result<List<BinaryTreeNode<ExpressionToken>>?, Error?>(nodes, null);
    }

    private Result<BinaryTreeNode<ExpressionToken>?, Error?> ParseToken(ref LexerContext context) {
        var token = context.Current;

        if (Converter.IsIf(token)) {
            return ParseIf(ref context);
        } if (token.Type == ExpressionTokenType.LeftBracket) {
            if (context.Pos <= 0) return ParseArray(ref context);
            
            return Converter.IsIndexable(context.Prev)? 
                       ParseIndex(ref context) :
                       ParseArray(ref context);
        } 
        return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(new BinaryTreeNode<ExpressionToken>(context.Tokens[context.Pos++]), null);
    }

    private Result<BinaryTreeNode<ExpressionToken>?, Error?> ParseArray(ref LexerContext context) {
        var tokens = new List<ExpressionToken>();

        context.IsArray = false;
        for (; context.Pos < context.Tokens.Count;) {
            var t = context.Current;

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
                return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(null, Error.InvalidSyntax);
            }
            
            var tempTokens = new List<ExpressionToken>();

            do {
                var result = ParseToken(ref context);
                if (!result.Ok) return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(null, result.Error);
                if (result.Value == null) return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(null, Error.SmthWentWrong);

                if (result.Value.Data.Type is ExpressionTokenType.RightBracket) {
                    context.IsArray = false;
                    break;
                } if (result.Value.Data.Type is ExpressionTokenType.Separator) break;
                
                if (context.Pos >= context.Tokens.Count) {
                    return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(null, Error.InvalidSyntax);
                }

                tempTokens.Add(result.Value.Data);
            } while (context.Pos < context.Tokens.Count);
            
            var parseResult = Parser.Parse(tempTokens
                                          .Select(x => new BinaryTreeNode<ExpressionToken>(x))
                                          .ToList());
            if (!parseResult.Ok) return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(null, parseResult.Error);
            if (parseResult.Value == null) continue;
            
            var executionResult = Executor.Execute(parseResult.Value);
            if (!executionResult.Ok) return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(null, executionResult.Error);
            if (executionResult.Value == null) return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(null, Error.SmthWentWrong);

            if (executionResult.Value.Type == ExpressionTokenType.None) continue;
            tokens.Add(executionResult.Value);
        }
        
        if (context.IsArray) return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(null, Error.InvalidSyntax);
        
        var token = new BinaryTreeNode<ExpressionToken>(new ExpressionToken(tokens));
        return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(token, null);
    }

    private Result<BinaryTreeNode<ExpressionToken>?, Error?> ParseIndex(ref LexerContext context) {
        var tokens = new List<ExpressionToken>();
        if (context.Current.Type is ExpressionTokenType.LeftBracket) ++context.Pos;
        
        do {
            var result = ParseToken(ref context);
            if (!result.Ok) return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(null, result.Error);
            if (result.Value == null) return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(null, Error.SmthWentWrong);
            
            if (result.Value.Data.Type is ExpressionTokenType.RightBracket) break;
            if (context.Pos >= context.Tokens.Count) {
                return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(null, Error.InvalidSyntax);
            }
            
            if (result.Value.Data.Type is ExpressionTokenType.RightBracket) break;
            
            tokens.Add(result.Value.Data);
        } while (context.Pos < context.Tokens.Count);

        var parseResult = Parser.Parse(tokens
                                      .Select(x => new BinaryTreeNode<ExpressionToken>(x))
                                      .ToList());
        if (!parseResult.Ok) return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(null, parseResult.Error);
        if (parseResult.Value == null) return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(null, Error.InvalidSyntax);

        var token = parseResult.Value.Root;
        token.Data.Type = ExpressionTokenType.Index;
        
        return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(token, null);
    }

    private Result<BinaryTreeNode<ExpressionToken>?, Error?> ParseIf(ref LexerContext context) {
        var keyword = context.Current;
        ++context.Pos;
        
        var parseConditionResult = ParseCondition(ref context);
        if (keyword.Type != ExpressionTokenType.Else) {
            if (!parseConditionResult.Ok) return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(null, parseConditionResult.Error);
            if (parseConditionResult.Value == null) return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(null, Error.InvalidSyntax);
        }
        
        var parseBodyResult = ParseBody(ref context);
        if (!parseBodyResult.Ok) return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(null, parseBodyResult.Error);
        if (parseBodyResult.Value == null) return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(null, Error.InvalidSyntax);
        
        var token = new BinaryTreeNode<ExpressionToken>(keyword, parent: null, parseConditionResult.Value, parseBodyResult.Value);
        return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(token, null);
    }

    private Result<BinaryTreeNode<ExpressionToken>?, Error?> ParseCondition(ref LexerContext context) {
        var tokens = new List<ExpressionToken>();

        for (; context.Pos < context.Tokens.Count;) {
            var result = ParseToken(ref context);
            if (!result.Ok) return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(null, result.Error);
            if (result.Value == null) return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(null, Error.InvalidSyntax);
            
            if (result.Value.Data.Type is ExpressionTokenType.ConditionEnd) break;
            tokens.Add(result.Value.Data);
        }
        
        var parseResult = Parser.Parse(tokens
                                      .Select(x => new BinaryTreeNode<ExpressionToken>(x))
                                      .ToList());
        if (!parseResult.Ok) return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(null, parseResult.Error);
        if (parseResult.Value == null) return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(null, Error.InvalidSyntax);

        return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(parseResult.Value.Root, parseResult.Error);
    }
    
    private Result<BinaryTreeNode<ExpressionToken>?, Error?> ParseBody(ref LexerContext context) {
        var tokens = new List<ExpressionToken>();

        for (; context.Pos < context.Tokens.Count;) {
            var result = ParseToken(ref context);
            if (!result.Ok) return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(null, result.Error);
            if (result.Value == null) return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(null, Error.InvalidSyntax);
            
            if (result.Value.Data.Type is ExpressionTokenType.IfEnd) break;
            tokens.Add(result.Value.Data);
        }
        
        var parseResult = Parser.Parse(tokens
                                      .Select(x => new BinaryTreeNode<ExpressionToken>(x))
                                      .ToList());
        if (!parseResult.Ok) return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(null, parseResult.Error);
        if (parseResult.Value == null) return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(null, Error.InvalidSyntax);
            
        return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(parseResult.Value.Root, parseResult.Error);
    }
    
    private void Process(List<BinaryTreeNode<ExpressionToken>> nodes, ExpressionToken token) {
        switch (token.Type) {
            case ExpressionTokenType.Index: {
                var pasted = new ExpressionToken("[]", ExpressionTokenType.Operation);
                token.Type = ExpressionTokenType.Number;
                nodes.Add(new BinaryTreeNode<ExpressionToken>(pasted)); break;
            }
        }
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