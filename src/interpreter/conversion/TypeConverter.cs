using System.Text;
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
    
    public Result<Pair<double?>?, Error?> ParseNumbers(Value left, Value right) {
        var lResult = ParseNumber(left);
        var rResult = ParseNumber(right);

        if (!(lResult.Ok || rResult.Ok) || (lResult.Value == null && rResult.Value == null)) {
            return new Result<Pair<double?>?, Error?>(null, Error.IllegalOperation);
        }
        
        return new Result<Pair<double?>?, Error?>(new Pair<double?>(lResult.Value, rResult.Value), null);
    }
    
    public Result<double?, Error?> ParseNumber(BinaryTreeNode<ExpressionToken>? token) {
        if (token == null) return new Result<double?, Error?>(null, Error.IllegalOperation);

        var value = token.Data.Value.Val; 
        
        var result = double.TryParse(value, out var val);
        if (token is { Data.Type: ExpressionTokenType.Bool, }) {
            if (!BoolToNumber(token.Data, out val)) {
                return new Result<double?, Error?>(null, Error.IllegalOperation);
            }
            result = true;
        }
        
        double? number = result? val : null;
        return new Result<double?, Error?>(number, null);
    }
    
    public Result<double?, Error?> ParseNumber(Value value) {
        var result = double.TryParse(value.Val, out var val);
        if (value is { Type: ExpressionTokenType.Bool, }) {
            if (!BoolToNumber(value, out val)) {
                return new Result<double?, Error?>(null, Error.IllegalOperation);
            }
            result = true;
        }
        
        double? number = result? val : null;
        return new Result<double?, Error?>(number, null);
    }
    
    public ExpressionTokenType VarTypeToTokenType(VariableType varType) {
        return varType switch {
                   VariableType.String     => ExpressionTokenType.String,
                   VariableType.Number     => ExpressionTokenType.Number,
                   VariableType.Bool       => ExpressionTokenType.Bool,
                   VariableType.Array      => ExpressionTokenType.Array,
                   VariableType.Reference  => ExpressionTokenType.Reference,
                   _                   => throw new ArgumentOutOfRangeException(nameof(varType), varType, null),
               };
    }
    
    public VariableType TokenTypeToVarType(ExpressionTokenType tokenType) {
        return tokenType switch {
                   ExpressionTokenType.String      => VariableType.String,
                   ExpressionTokenType.Number      => VariableType.Number,
                   ExpressionTokenType.Bool        => VariableType.Bool,
                   ExpressionTokenType.Array       => VariableType.Array,
                   ExpressionTokenType.Reference   => VariableType.Reference,
                   ExpressionTokenType.Name        => VariableType.Reference,
                   _                               => throw new ArgumentOutOfRangeException(nameof(tokenType), tokenType, null),
               };
    }

    public bool BoolToNumber(ExpressionToken token, out double value) {
        value = -1.0;
        if (token.Type != ExpressionTokenType.Bool) return false;

        switch (token.Value.Val) {
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
    
    public bool BoolToNumber(Value val, out double value) {
        value = -1.0;
        if (val.Type != ExpressionTokenType.Bool) return false;

        switch (val.Val) {
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

        switch (token.Value.Val) {
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
        value = token.Value.Val;
        
        if (token.Type == ExpressionTokenType.Bool) return true;
        if (token.Type != ExpressionTokenType.Number 
         && IsValue(token)) {
            return !string.IsNullOrEmpty(token.Value.Val);
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

    public ExpressionToken ToReference(Variable var) {
        return new ExpressionToken(var.Name, VarTypeToTokenType(var.Type));
    }
    
    public Variable? FromReference(ExpressionToken reference) {
        return _interpreter.GetVariable(reference.Value.Val);
    }
    
    public bool ArrayToString(ExpressionToken token, out string value) {
        value = string.Empty;
        if (token.Type != ExpressionTokenType.Array) return false;
        
        var sb = new StringBuilder();
        var values = token.Values;
            
        sb.Append('[');
        for (var i = 0; i < values.Count; i++) {
            var val = values[i];
            if (i < values.Count - 1) {
                sb.Append($"{val.Val}, ");
            } else {
                sb.Append($"{val.Val}");  
            }
        }

        sb.Append(']');
        value = sb.ToString();
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
            case "==": return ExpressionTokenType.Operation;
            case "=": return ExpressionTokenType.SpecialOperation;
            case "(": return ExpressionTokenType.OpeningParenthesis;
            case ")": return ExpressionTokenType.ClosingParenthesis;
            case ",": return ExpressionTokenType.Comma;
            case ";": return ExpressionTokenType.Semicolon;
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
        
        var variable = _interpreter.GetVariable(token.Value.Val);
        if (variable == null) return false;
        
        token.Type = VarTypeToTokenType(variable.Type);
        token.Values = VarValuesToValues(variable.Values);
        return true;
    }

    public List<VarValue> ValuesToVarValues(List<Value> values) {
        var varVals = values
                     .Select(val => new VarValue(val.Val, TokenTypeToVarType(val.Type)))
                     .ToList();
        return varVals;
    }
    
    public List<Value> VarValuesToValues(List<VarValue> varVals) {
        var vals = varVals
                     .Select(val => new Value(val.Val, VarTypeToTokenType(val.Type)))
                     .ToList();
        return vals;
    }

    public VarValue ValuesToVarValues(Value value) {
        return new VarValue(value.Val, TokenTypeToVarType(value.Type));
    }
    
    public bool IsOperation(string value) {
        return value switch {
                   "%" or "^" or "/" or "*" or "-" or "+" or "!" or "|" or "&" or "<" or ">" or "<=" or ">=" or "!="
                    or "==" or "=" => true,
                   _ => false,
               };
    }
    
    public bool IsOperation(ExpressionToken token) {
        return token.Type is ExpressionTokenType.Operation or ExpressionTokenType.SpecialOperation;
    }

    public bool IsValue(ExpressionToken token) {
        return token.Type is ExpressionTokenType.String or ExpressionTokenType.Number or ExpressionTokenType.Bool 
                          or ExpressionTokenType.Name or ExpressionTokenType.Array or ExpressionTokenType.Reference;
    }
    
    public bool IsNotArrayValue(ExpressionToken token) {
        return token.Type is ExpressionTokenType.String or ExpressionTokenType.Number or ExpressionTokenType.Bool or ExpressionTokenType.Name;
    }
    
    public bool IsLiteral(ExpressionToken token) {
        return token.Type is ExpressionTokenType.String;
    }
    
    public bool IsLiteral(Value val) {
        return val.Type is ExpressionTokenType.String;
    }

    public bool IsReferencable(ExpressionToken token) {
        return token.Type is ExpressionTokenType.Name or ExpressionTokenType.Reference or ExpressionTokenType.Array;
    }
    
    public bool IsReferencable(Value val) {
        return val.Type is ExpressionTokenType.Name or ExpressionTokenType.Reference;
    }

    public bool IsReferenceOperation(Operation op) {
        var name = op.Name;
        var scope = op.Scope;
        if (scope != OpScope.Right) return false;

        return name.Equals("&") || name.Equals("*");
    }
}