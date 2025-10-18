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

    public ExpressionToken? PrevParsed;
    public ExpressionToken Current => Tokens[Pos];


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
            var result = ParseToken(ref context, out var parsed);
            if (!result.Ok) return new Result<List<BinaryTreeNode<ExpressionToken>>?, Error?>(null, result.Error);
            if (result.Value == null) return new Result<List<BinaryTreeNode<ExpressionToken>>?, Error?>(null, Error.SmthWentWrong);

            context.PrevParsed = result.Value.Data;
            nodes.AddRange(parsed);
        }
        
        return new Result<List<BinaryTreeNode<ExpressionToken>>?, Error?>(nodes, null);
    }

    private Result<BinaryTreeNode<ExpressionToken>?, Error?> ParseToken(ref LexerContext context, out List<BinaryTreeNode<ExpressionToken>> parsed) {
        var token = context.Current;
        parsed = [];

        if (Converter.IsIfStatement(token)) {
            return ParseIfStatement(ref context, out parsed);
        } if (token.Type == ExpressionTokenType.LeftBracket) {
            if (context.Pos <= 0
             || context.PrevParsed == null) return ParseArray(ref context, out parsed);
            
            return Converter.IsIndexable(context.PrevParsed)? 
                       ParseIndex(ref context, out parsed) :
                       ParseArray(ref context, out parsed);
        }

        var resultToken = context.Tokens[context.Pos++];
        parsed.Add(new BinaryTreeNode<ExpressionToken>(resultToken));
        return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(new BinaryTreeNode<ExpressionToken>(resultToken), null);
    }

    private Result<BinaryTreeNode<ExpressionToken>?, Error?> ParseArray(ref LexerContext context, out List<BinaryTreeNode<ExpressionToken>> parsed) {
        var tokens = new List<ExpressionToken>();
        parsed = [];

        context.IsArray = false;
        var quit = false;
        
        for (; context.Pos < context.Tokens.Count;) {
            if (quit) break;
            
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
                var result = ParseToken(ref context, out parsed);
                if (!result.Ok) return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(null, result.Error);
                if (result.Value == null) return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(null, Error.SmthWentWrong);

                if (result.Value.Data.Type is ExpressionTokenType.RightBracket) {
                    quit = true;
                    context.IsArray = false;
                    break;
                } if (result.Value.Data.Type is ExpressionTokenType.Separator) break;
                
                if (context.Pos >= context.Tokens.Count) {
                    return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(null, Error.InvalidSyntax);
                }

                context.PrevParsed = result.Value.Data;
                tempTokens.AddRange(parsed.Select(x => x.Data));
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
        
        parsed.Clear();
        parsed.Add(token);
        
        return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(token, null);
    }

    private Result<BinaryTreeNode<ExpressionToken>?, Error?> ParseIndex(ref LexerContext context, out List<BinaryTreeNode<ExpressionToken>> parsed) {
        var tokens = new List<ExpressionToken>();
        parsed = [];
        
        if (context.Current.Type is ExpressionTokenType.LeftBracket) ++context.Pos;
        
        do {
            var result = ParseToken(ref context, out parsed);
            if (!result.Ok) return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(null, result.Error);
            if (result.Value == null) return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(null, Error.SmthWentWrong);
            
            if (result.Value.Data.Type is ExpressionTokenType.RightBracket) break;
            if (context.Pos >= context.Tokens.Count) {
                return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(null, Error.InvalidSyntax);
            }
            
            if (result.Value.Data.Type is ExpressionTokenType.RightBracket) break;
            
            context.PrevParsed = result.Value.Data;
            tokens.AddRange(parsed.Select(x => x.Data));
        } while (context.Pos < context.Tokens.Count);

        var parseResult = Parser.Parse(tokens
                                      .Select(x => new BinaryTreeNode<ExpressionToken>(x))
                                      .ToList());
        if (!parseResult.Ok) return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(null, parseResult.Error);
        if (parseResult.Value == null) return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(null, Error.InvalidSyntax);
        
        var opToken = new ExpressionToken("[]", ExpressionTokenType.Operation);
        var index = parseResult.Value.Root;
        index.Data.Type = ExpressionTokenType.Number;
        
        parsed.Clear();
        parsed.AddRange([new BinaryTreeNode<ExpressionToken>(opToken), index,]);
        return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(index, null);
    }

    private Result<BinaryTreeNode<ExpressionToken>?, Error?> ParseIfStatement(ref LexerContext context, out List<BinaryTreeNode<ExpressionToken>> parsed) {
        var keyword = context.Current;
        parsed = [];
        
        ++context.Pos;
        var parseConditionResult = ParseCondition(ref context);
        if (keyword.Type != ExpressionTokenType.Else) {
            if (!parseConditionResult.Ok) return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(null, parseConditionResult.Error);
            if (parseConditionResult.Value == null) return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(null, Error.InvalidSyntax);
        }
        
        var parseBodyResult = ParseIfBody(ref context);
        if (!parseBodyResult.Ok) return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(null, parseBodyResult.Error);
        if (parseBodyResult.Value == null) return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(null, Error.InvalidSyntax);
        
        var token = new BinaryTreeNode<ExpressionToken>(keyword, parent: null, parseConditionResult.Value, parseBodyResult.Value);
        parsed.Add(token);
        return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(token, null);
    }

    private Result<BinaryTreeNode<ExpressionToken>?, Error?> ParseCondition(ref LexerContext context) {
        var tokens = new List<ExpressionToken>();

        var foundConditionEnd = false;
        for (; context.Pos < context.Tokens.Count;) {
            var result = ParseToken(ref context, out var parsed);
            if (!result.Ok) return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(null, result.Error);
            if (result.Value == null) return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(null, Error.InvalidSyntax);

            if (result.Value.Data.Type is ExpressionTokenType.ConditionEnd) {
                foundConditionEnd = true;
                break;
            }
            
            context.PrevParsed = result.Value.Data;
            tokens.AddRange(parsed.Select(x => x.Data));
        }
        
        if (!foundConditionEnd) return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(null, Error.InvalidSyntax);
        
        var parseResult = Parser.Parse(tokens
                                      .Select(x => new BinaryTreeNode<ExpressionToken>(x))
                                      .ToList());
        if (!parseResult.Ok) return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(null, parseResult.Error);
        if (parseResult.Value == null) return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(null, Error.InvalidSyntax);

        return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(parseResult.Value.Root, parseResult.Error);
    }
    
    private Result<BinaryTreeNode<ExpressionToken>?, Error?> ParseIfBody(ref LexerContext context) {
        var tokens = new List<BinaryTreeNode<ExpressionToken>>();

        var foundIfEnd = false;
        for (; context.Pos < context.Tokens.Count;) {
            var result = ParseToken(ref context, out var parsed);
            if (!result.Ok) return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(null, result.Error);
            if (result.Value == null) return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(null, Error.InvalidSyntax);

            if (result.Value.Data.Type is ExpressionTokenType.IfEnd) {
                foundIfEnd = true;
                break;
            }
            
            context.PrevParsed = result.Value.Data;
            tokens.AddRange(parsed);
        }
        
        if (!foundIfEnd) return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(null, Error.InvalidSyntax);
        
        var parseResult = Parser.Parse(tokens);
        if (!parseResult.Ok) return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(null, parseResult.Error);
        if (parseResult.Value == null) return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(null, Error.InvalidSyntax);
            
        return new Result<BinaryTreeNode<ExpressionToken>?, Error?>(parseResult.Value.Root, parseResult.Error);
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