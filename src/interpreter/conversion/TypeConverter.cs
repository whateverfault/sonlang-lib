using System.Text;
using sonlanglib.interpreter.data;
using sonlanglib.interpreter.data.ops;
using sonlanglib.interpreter.data.vars;
using sonlanglib.interpreter.error;
using sonlanglib.interpreter.tokenizer;
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
    
    public string ToString(ExpressionToken token) {
        var str = token.Type switch {
                      ExpressionTokenType.Array  => ArrayToString(token),
                      ExpressionTokenType.String => StringToString(token),
                      _                          => token.Value.Val,
                  };

        return str;
    }
    
    public bool ToNumber(Value val, out double value) {
        var str = val.Val;
        value = 0;

        switch (val.Type) {
            case ExpressionTokenType.Number: {
                return double.TryParse(str, out value);
            } case ExpressionTokenType.Bool: {
                if (!BoolToLogicalBool(val, out var valueB)) return false;
                value = valueB? 1 : 0;
                return true;
            } case ExpressionTokenType.String: {
                value = str.Length;
                return true;
            }
        }

        return false;
    }
    
    public bool ToBool(Value val, out string value) {
        value = val.Val;
        
        if (val.Type == ExpressionTokenType.Bool) return true;
        if (val.Type != ExpressionTokenType.Number 
         && IsValue(val)) {
            return !string.IsNullOrEmpty(val.Val);
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

    private string StringToString(ExpressionToken token) {
        var sb = new StringBuilder();
        sb.Append($"\"{token.Value.Val}\"");
        return sb.ToString();
    }
    
    private string ArrayToString(ExpressionToken token) {
        var sb = new StringBuilder();
        var tokens = token.Next;
            
        sb.Append('[');
        for (var i = 0; i < tokens.Count; i++) {
            var t = tokens[i];
            var s = ToString(t);
            
            if (i < tokens.Count - 1) {
                sb.Append($"{s}, ");
            } else {
                sb.Append($"{s}");  
            }
        }

        sb.Append(']');
        return sb.ToString();
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
            case "=": return ExpressionTokenType.Assigment;
            case "(": return ExpressionTokenType.LeftParenthesis;
            case ")": return ExpressionTokenType.RightParenthesis;
            case "[": return ExpressionTokenType.LeftBracket;
            case "]": return ExpressionTokenType.RightBracket;
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

        var t = VarValueToToken(variable.Value);

        token.Type = t.Type;
        token.Next = t.Next;
        token.Value = t.Value;
        return true;
    }

    public ExpressionToken VarValueToToken(VarValue value) {
        var tokens = new List<ExpressionToken>();
        if (value.Next.Count <= 0) return new ExpressionToken(value.Value.Val, VarTypeToTokenType(value.Type));

        tokens.AddRange(value.Next
                             .Select(var => var.Next.Count > 0? 
                                                VarValueToToken(var) : 
                                                new ExpressionToken(var.Value.Val, VarTypeToTokenType(var.Value.Type))));

        return new ExpressionToken(tokens, VarTypeToTokenType(value.Type));
    }
    
    public VarValue TokenToVarValue(ExpressionToken token) {
        var vars = new List<VarValue>();
        var converted = new List<VarValue>();
        if (token.Next.Count > 0) {
            converted = TokensToVarValues(token.Next);
        }
        
        vars.AddRange(converted);
        var var = new VarValue(vars, value: ValueToSubValue(token.Value), TokenTypeToVarType(token.Type));
        return var;
    }
    
    public bool IsOperation(string value) {
        return value switch {
                   "%" or "^" or "/" or "*" or "-" or "+" or "!" or "|" or "&" or "<" or ">" or "<=" or ">=" or "!="
                    or "==" or "=" => true,
                   _ => false,
               };
    }
    
    public bool IsOperation(ExpressionToken token) {
        return token.Type is ExpressionTokenType.Operation or ExpressionTokenType.Assigment;
    }

    public bool IsValue(ExpressionToken token) {
        return token.Type is ExpressionTokenType.String or ExpressionTokenType.Number or ExpressionTokenType.Bool 
                          or ExpressionTokenType.Name or ExpressionTokenType.Array or ExpressionTokenType.Reference;
    }
    
    public bool IsValue(Value val) {
        return val.Type is ExpressionTokenType.String or ExpressionTokenType.Number or ExpressionTokenType.Bool 
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

    public bool IsIndexable(ExpressionToken token) {
        return token.Type is ExpressionTokenType.Name or ExpressionTokenType.Array or ExpressionTokenType.String;
    }
    
    public bool IsReferenceOperation(Operation op) {
        var name = op.Name;
        var scope = op.Scope;

        if (name.Equals("[]")) return true;
        if (scope != OpScope.Right) return false;
        
        return name.Equals("&") || name.Equals("*");
    }
    
    private VarValue ValueToVarValue(Value value) {
        var varVals = new VarValue(value.Val, TokenTypeToVarType(value.Type));
        return varVals;
    }
    
    private SubValue ValueToSubValue(Value value) {
        var varVals = new SubValue(value.Val, TokenTypeToVarType(value.Type));
        return varVals;
    }
    
    private Result<double?, Error?> ParseNumber(BinaryTreeNode<ExpressionToken>? node) {
        if (node == null) return new Result<double?, Error?>(null, Error.IllegalOperation);
        
        var token = node.Data.Value;
        var result = ToNumber(token, out var number);
        
        return result? 
                   new Result<double?, Error?>(number, null) :
                   new Result<double?, Error?>(null, Error.IllegalOperation);
    }
    
    private Result<double?, Error?> ParseNumber(Value value) {
        var result = ToNumber(value, out var number);
        return result? 
                   new Result<double?, Error?>(number, null) :
                   new Result<double?, Error?>(null, Error.IllegalOperation);
    }
    
    private ExpressionTokenType VarTypeToTokenType(VariableType varType) {
        return varType switch {
                   VariableType.String     => ExpressionTokenType.String,
                   VariableType.Number     => ExpressionTokenType.Number,
                   VariableType.Bool       => ExpressionTokenType.Bool,
                   VariableType.Array      => ExpressionTokenType.Array,
                   VariableType.Reference  => ExpressionTokenType.Reference,
                   _                   => throw new ArgumentOutOfRangeException(nameof(varType), varType, null),
               };
    }
    
    private VariableType TokenTypeToVarType(ExpressionTokenType tokenType) {
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

    private bool BoolToLogicalBool(Value val, out bool value) {
        value = false;
        if (val.Type != ExpressionTokenType.Bool) return false;

        switch (val.Val) {
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

    private List<VarValue> TokensToVarValues(List<ExpressionToken> tokens) {
        var vars = new List<VarValue>();
        foreach (var token in tokens) {
            VarValue? converted = null;
            if (token.Next.Count > 0) {
                var vals = TokensToVarValues(token.Next);
                converted = new VarValue(vals);
            }

            var t = converted ?? ValueToVarValue(token.Value);
            vars.Add(t);
        }
        
        return vars;
    }
    
    private VariableType ImplyVariableType(string value) {
        if (double.TryParse(value, out _)) return VariableType.Number;
        return value switch {
                   InterpreterConstants.False or InterpreterConstants.True => VariableType.Bool,
                   _                                                       => VariableType.String,
               };
    }
}