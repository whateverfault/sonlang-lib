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
    private readonly Tokenizer _lexer;
    
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
        
        _metaData = new InterpreterMetadata();
        _lexer = new Tokenizer(this);
        
        OperationList.Initialize(this);
    }
    
    public Result<ExpressionToken?, string?> Evaluate(string expression) {
        if (string.IsNullOrEmpty(expression)) return new Result<ExpressionToken?, string?>(new ExpressionToken(string.Empty, ExpressionTokenType.String), null);

        expression = expression.Replace(" ", "");
        
        var lexResult = _lexer.Tokenize(expression);
        if (!lexResult.Ok) return new Result<ExpressionToken?, string?>(null, Errors.GetErrorString(lexResult.Error));
        if (lexResult.Value == null) return new Result<ExpressionToken?, string?>(null, Errors.GetErrorString(Error.SmthWentWrong));
        
        var parseResult = Parser.Parse(lexResult.Value);
        if (!parseResult.Ok) return new Result<ExpressionToken?, string?>(null, Errors.GetErrorString(parseResult.Error));
        if (parseResult.Value == null) return new Result<ExpressionToken?, string?>(null, Errors.GetErrorString(Error.SmthWentWrong));
        
        var executionResult =  Executor.Execute(parseResult.Value);
        if (!executionResult.Ok) return new Result<ExpressionToken?, string?>(null, Errors.GetErrorString(executionResult.Error));
        if (executionResult.Value == null) return new Result<ExpressionToken?, string?>(null, Errors.GetErrorString(Error.SmthWentWrong));
        
        if (TypeConverter.ArrayToString(executionResult.Value, out var value)) {
            executionResult.Value.Value.Val = value;
        }
        
        return new Result<ExpressionToken?, string?>(executionResult.Value, null);
    }
    
    public Variable? SetVariable(string name, List<VarValue> vals, VariableType type) {
        if (string.IsNullOrEmpty(name)) return null;
        
        var result = _metaData.SetVariable(name, type, vals);
        OnVariableChanged?.Invoke(this, result);

        return result;
    }

    public Variable? SetVariable(string name, List<Value> vals, ExpressionTokenType type) {
        if (string.IsNullOrEmpty(name)) return null;
        return SetVariable(name, TypeConverter.ValuesToVarValues(vals), TypeConverter.TokenTypeToVarType(type));
    }
    
    public Variable? SetVariable(string name, string val, VariableType type) {
        return SetVariable(name, [new VarValue(val, type),], type);
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