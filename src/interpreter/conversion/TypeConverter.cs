using sonlanglib.interpreter.data;
using sonlanglib.interpreter.error;
using sonlanglib.interpreter.lexer;
using sonlanglib.shared;
using sonlanglib.shared.trees;

namespace sonlanglib.interpreter.conversion;

public class TypeConverter {
    private readonly Interpreter _interpreter;
    
    
    public TypeConverter(Interpreter interpreter) {
        _interpreter = interpreter;
    }
    
    public Result<Pair<double?>?, Error?> ParseNumbers(BinaryTreeNode<ExpressionToken>? left, BinaryTreeNode<ExpressionToken>? right) {
        if (right == null && left == null) return new Result<Pair<double?>?, Error?>(null, Error.IllegalOperation);

        var lResult = ParseNumber(left);
        var rResult = ParseNumber(right);

        if (!(lResult.Ok || rResult.Ok) || (lResult.Value == null && rResult.Value == null)) {
            return new Result<Pair<double?>?, Error?>(null, Error.IllegalOperation);
        }
        
        return new Result<Pair<double?>?, Error?>(new Pair<double?>(lResult.Value, rResult.Value), null);
    }
    
    public Result<double?, Error?> ParseNumber(BinaryTreeNode<ExpressionToken>? token) {
        if (token == null) return new Result<double?, Error?>(null, Error.IllegalOperation);

        var result = double.TryParse(token.Data.Value, out var val);
        if (token is { Data.Type: ExpressionTokenType.Bool, }) {
            if (!BoolToNumber(token.Data, out val)) {
                return new Result<double?, Error?>(null, Error.IllegalOperation);
            }
            result = true;
        }
        
        double? number = result? val : null;
        return new Result<double?, Error?>(number, null);
    }
    
    public ExpressionTokenType VarTypeToTokenType(VariableType varType) {
        return varType switch {
                   VariableType.String => ExpressionTokenType.String,
                   VariableType.Number => ExpressionTokenType.Number,
                   VariableType.Bool   => ExpressionTokenType.Bool,
                   VariableType.Array  => ExpressionTokenType.Array,
                   _                   => throw new ArgumentOutOfRangeException(nameof(varType), varType, null),
               };
    }
    
    public VariableType TokenTypeToVarType(ExpressionTokenType tokenType) {
        return tokenType switch {
                   ExpressionTokenType.String => VariableType.String,
                   ExpressionTokenType.Number => VariableType.Number,
                   ExpressionTokenType.Bool   => VariableType.Bool,
                   ExpressionTokenType.Array  => VariableType.Array,
                   _                          => throw new ArgumentOutOfRangeException(nameof(tokenType), tokenType, null),
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

    public bool BoolToLogicalBool(ExpressionToken token, out bool value) {
        value = false;
        if (token.Type != ExpressionTokenType.Bool) return false;

        switch (token.Value) {
            case InterpreterConstants.True:
                value = true;
                break;
            case InterpreterConstants.False:
                value = false;
                break;
            default: return false;
        }
        return true;
    }
    
    public bool ToBool(ExpressionToken token, out string value) {
        value = token.Value;
        
        if (token.Type == ExpressionTokenType.Bool) return true;
        if (token.Type != ExpressionTokenType.Number 
         && IsValue(token)) {
            return !string.IsNullOrEmpty(token.Value);
        }

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
        
        var variable = _interpreter.GetVariable(token.Value);
        if (variable == null) return false;
        
        token.Type = VarTypeToTokenType(variable.Type);
        token.Value = variable.Value;
        return true;
    }

    public bool IsOperation(ExpressionToken token) {
        return token.Type is ExpressionTokenType.ArithmeticOperation or ExpressionTokenType.SpecialOperation;
    }

    public bool IsValue(ExpressionToken token) {
        return token.Type is ExpressionTokenType.String or ExpressionTokenType.Number or ExpressionTokenType.Bool or ExpressionTokenType.Name or ExpressionTokenType.Array;
    }
    
    public bool IsLiteral(ExpressionToken token) {
        return token.Type is ExpressionTokenType.String;
    }
}