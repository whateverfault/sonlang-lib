using sonlanglib.interpreter.calculator;
using sonlanglib.interpreter.data;
using sonlanglib.interpreter.error;
using sonlanglib.interpreter.executor;
using sonlanglib.interpreter.lexer;
using sonlanglib.interpreter.parser;
using sonlanglib.shared;

namespace sonlanglib.interpreter;

public class Interpreter {
    private readonly InterpreterMetadata _metaData;
    private readonly ExpressionLexer _lexer;
    private readonly TokenParser _parser;
    private readonly OperationExecutor _executor;
    
    public Calculator? Calculator { get; private set; }

    public event EventHandler<Variable>? OnVariableChanged; 


    public Interpreter() {
        OperationList.Initialize(this);
        
        _metaData = new InterpreterMetadata();
        _lexer = new ExpressionLexer(this);
        _parser = new TokenParser(this);
        _executor = new OperationExecutor(this);
        Calculator = new Calculator(this);
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
    
    public Variable? SetVariable(string name, string val, VariableType type) {
        if (string.IsNullOrEmpty(name)) return null;
        
        var result = _metaData.SetVariable(name, type, val);
        OnVariableChanged?.Invoke(this, result);

        return result;
    }

    public ExpressionTokenType VarTypeToTokenType(VariableType varType) {
        return varType switch {
                   VariableType.String => ExpressionTokenType.String,
                   VariableType.Number => ExpressionTokenType.Number,
                   VariableType.Bool => ExpressionTokenType.Bool,
                   _                   => throw new ArgumentOutOfRangeException(nameof(varType), varType, null),
               };
    }
    
    public VariableType TokenTypeToVarType(ExpressionTokenType tokenType) {
        return tokenType switch {
                   ExpressionTokenType.String => VariableType.String,
                   ExpressionTokenType.Number => VariableType.Number,
                   ExpressionTokenType.Bool => VariableType.Bool,
                   _ => throw new ArgumentOutOfRangeException(nameof(tokenType), tokenType, null),
               };
    }

    public bool BoolToNumber(ExpressionToken token, out double value) {
        value = -1.0;
        if (token.Type != ExpressionTokenType.Bool) return false;

        switch (token.Value) {
            case InterpreterConstants.True:
                value = InterpreterConstants.TrueD;
                break;
            case InterpreterConstants.False:
                value = InterpreterConstants.FalseD;
                break;
            default: return false;
        }
        return true;
    }

    public bool NumberToBool(ExpressionToken token, out string value) {
        value = token.Value;
        
        if (token.Type == ExpressionTokenType.Bool) return true;
        if (token.Type != ExpressionTokenType.Number) return false;

        if (!double.TryParse(value, out var valueD)) return false;
        
        switch (valueD) {
            case InterpreterConstants.TrueD:
                value = InterpreterConstants.True;
                break;
            case InterpreterConstants.FalseD:
                value = InterpreterConstants.False;
                break;
            default: return false;
        }

        return true;
    }
    
    public VariableType ImplyVariableType(string value) {
        if (double.TryParse(value, out _)) return VariableType.Number;
        return value switch {
                   InterpreterConstants.False or InterpreterConstants.True => VariableType.Bool,
                   _                                                       => VariableType.String,
               };
    }
    
    public ExpressionTokenType ImplyTokenType(string token) {
        switch (token) {
            case "%":
            case "^":
            case "/":
            case "*":
            case "-":
            case "+":
            case "!":
            case "|":
            case "&":
            case "<":
            case ">":
            case "<=":
            case ">=":
            case "!=":
            case "==": return ExpressionTokenType.ArithmeticOperation;
            case "=":  return ExpressionTokenType.SpecialOperation;
            case "(":  return ExpressionTokenType.OpeningParenthesis;
            case ")":  return ExpressionTokenType.ClosingParenthesis;
            case ";":  return ExpressionTokenType.Semicolon;
        }

        var implied = ImplyVariableType(token);
        return implied == VariableType.String? 
                   ExpressionTokenType.Name :
                   VarTypeToTokenType(implied);
    }
    
    public string LogicalBoolToBool(bool logical) {
        return logical ? InterpreterConstants.True : InterpreterConstants.False;
    }
    
    public bool AssignVarToName(ExpressionToken token) {
        if (token.Type != ExpressionTokenType.Name) return false;
        
        var variable = GetVariable(token.Value);
        if (variable == null) return false;
        
        token.Type = VarTypeToTokenType(variable.Type);
        token.Value = variable.Value;
        return true;
    }

    public bool IsOperation(ExpressionToken token) {
        return token.Type is ExpressionTokenType.ArithmeticOperation or ExpressionTokenType.SpecialOperation;
    }

    public bool IsValue(ExpressionToken token) {
        return token.Type is ExpressionTokenType.String or ExpressionTokenType.Number or ExpressionTokenType.Bool or ExpressionTokenType.Name;
    }
    
    public bool IsLiteral(ExpressionToken token) {
        return token.Type is ExpressionTokenType.String;
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