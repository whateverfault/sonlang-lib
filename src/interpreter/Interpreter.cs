using sonlanglib.interpreter.calculator;
using sonlanglib.interpreter.conversion;
using sonlanglib.interpreter.data;
using sonlanglib.interpreter.data.ops;
using sonlanglib.interpreter.data.vars;
using sonlanglib.interpreter.error;
using sonlanglib.interpreter.executor;
using sonlanglib.interpreter.lexer;
using sonlanglib.interpreter.parser;
using sonlanglib.interpreter.tokenizer;
using sonlanglib.shared;

namespace sonlanglib.interpreter;

public class Interpreter {
    private readonly ExpressionTokenizer _tokenizer;
    private readonly InterpreterMetadata _metaData;
    private readonly ExpressionLexer _lexer;
    
    public Calculator Calculator { get; private set; }
    public TypeConverter TypeConverter { get; private set; }
    public TokenParser Parser { get; private set; }
    public OperationExecutor Executor { get; private set; }

    public event EventHandler<Variable>? OnVariableChanged; 


    public Interpreter() {
        TypeConverter = new TypeConverter(this);
        Calculator = new Calculator(TypeConverter);
        Parser = new TokenParser(this);
        Executor = new OperationExecutor(TypeConverter);
        
        _tokenizer = new ExpressionTokenizer(this);
        _lexer = new ExpressionLexer(this);
        _metaData = new InterpreterMetadata();
        
        OperationList.Initialize(this);
    }
    
    public Result<string?, string?> Evaluate(string expression) {
        if (string.IsNullOrEmpty(expression)) return new Result<string?, string?>(string.Empty, null);
        
        var tokenizeResult = _tokenizer.Tokenize(expression);
        if (!tokenizeResult.Ok) return new Result<string?, string?>(null, Errors.GetErrorString(tokenizeResult.Error));
        if (tokenizeResult.Value == null) return new Result<string?, string?>(null, Errors.GetErrorString(Error.SmthWentWrong));
        
        var lexResult = _lexer.Lex(tokenizeResult.Value);
        if (!lexResult.Ok) return new Result<string?, string?>(null, Errors.GetErrorString(lexResult.Error));
        if (lexResult.Value == null) return new Result<string?, string?>(null, Errors.GetErrorString(Error.SmthWentWrong));
        
        var parseResult = Parser.Parse(lexResult.Value);
        if (!parseResult.Ok) return new Result<string?, string?>(null, Errors.GetErrorString(parseResult.Error));
        if (parseResult.Value == null) return new Result<string?, string?>(null, Errors.GetErrorString(Error.SmthWentWrong));
        
        var executionResult =  Executor.Execute(parseResult.Value);
        if (!executionResult.Ok) return new Result<string?, string?>(null, Errors.GetErrorString(executionResult.Error));
        if (executionResult.Value == null) return new Result<string?, string?>(null, Errors.GetErrorString(Error.SmthWentWrong));

        executionResult.Value.Value.Val = TypeConverter.ToString(executionResult.Value);
        return new Result<string?, string?>(executionResult.Value.Value.Val, null);
    }
    
    private Variable? SetVariable(string name, VarValue val) {
        if (string.IsNullOrEmpty(name)) return null;
        
        var result = _metaData.SetVariable(name, val);
        OnVariableChanged?.Invoke(this, result);

        return result;
    }

    public Variable? SetVariable(string name, ExpressionToken token) {
        if (string.IsNullOrEmpty(name)) return null;
        return SetVariable(name, TypeConverter.TokenToVarValue(token));
    }
    
    public Variable? GetVariable(string name) {
        return string.IsNullOrEmpty(name)? 
                   null :
                   _metaData.GetVariable(name);
    }

    public void LoadVars(Dictionary<string, Variable> vars) {
        _metaData.LoadVariables(vars);
    }
}