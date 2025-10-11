using sonlanglib.interpreter.calculator;
using sonlanglib.interpreter.conversion;
using sonlanglib.interpreter.data;
using sonlanglib.interpreter.error;
using sonlanglib.interpreter.executor;
using sonlanglib.interpreter.lexer;
using sonlanglib.interpreter.parser;
using sonlanglib.shared;

namespace sonlanglib.interpreter;

public class Interpreter {
    private readonly InterpreterMetadata _metaData;
    private readonly OperationExecutor _executor;
    private readonly ExpressionLexer _lexer;
    private readonly TokenParser _parser;
    
    public Calculator? Calculator { get; private set; }
    public TypeConverter? TypeConverter { get; private set; }

    public event EventHandler<Variable>? OnVariableChanged; 


    public Interpreter() {
        OperationList.Initialize(this);
        
        TypeConverter = new TypeConverter(this);
        Calculator = new Calculator(TypeConverter);
        
        _metaData = new InterpreterMetadata();
        _parser = new TokenParser(this);
        _lexer = new ExpressionLexer(TypeConverter);
        _executor = new OperationExecutor(TypeConverter);
    }
    
    public Result<ExpressionToken?, string?> Evaluate(string expression) {
        if (string.IsNullOrEmpty(expression)) return new Result<ExpressionToken?, string?>(new ExpressionToken(ExpressionTokenType.String, string.Empty), null);
        
        var lexResult = _lexer.Lex(expression);
        if (!lexResult.Ok) return new Result<ExpressionToken?, string?>(null, Errors.GetErrorString(lexResult.Error));
        if (lexResult.Value == null) return new Result<ExpressionToken?, string?>(null, Errors.GetErrorString(Error.SmthWentWrong));
        
        var parseResult = _parser.Parse(lexResult.Value);
        if (!parseResult.Ok) return new Result<ExpressionToken?, string?>(null, Errors.GetErrorString(parseResult.Error));
        if (parseResult.Value == null) return new Result<ExpressionToken?, string?>(null, Errors.GetErrorString(Error.SmthWentWrong));
        
        var executionResult =  _executor.Execute(parseResult.Value);
        if (!executionResult.Ok) return new Result<ExpressionToken?, string?>(null, Errors.GetErrorString(executionResult.Error));
        if (executionResult.Value == null) return new Result<ExpressionToken?, string?>(null, Errors.GetErrorString(Error.SmthWentWrong));

        return new Result<ExpressionToken?, string?>(executionResult.Value, null);
    }
    
    public Variable? SetVariable(string name, List<string> vals, VariableType type) {
        if (string.IsNullOrEmpty(name)) return null;
        
        var result = _metaData.SetVariable(name, type, vals);
        OnVariableChanged?.Invoke(this, result);

        return result;
    }

    public Variable? SetVariable(string name, string val, VariableType type) {
        return SetVariable(name, [val,], type);
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