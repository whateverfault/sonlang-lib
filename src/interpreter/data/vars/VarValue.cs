namespace sonlanglib.interpreter.data.vars;

public class VarValue {
    public readonly VariableType Type;
    
    public readonly List<VarValue> Next; 

    public SubValue Value { get; set; }


    public VarValue(string value, VariableType type) {
        Type = type;
        Value = new SubValue(value, type);
        Next = [];
    }
    
    public VarValue(List<VarValue> tokens, SubValue? value = null, VariableType type = VariableType.Array) {
        Type = type;
        Next = tokens;

        if (tokens.Count <= 0) {
            Value = value ?? new SubValue(string.Empty, VariableType.String);
        } else {
            Value = tokens[0].Value;
        }
    }
    
    public VarValue(SubValue value) {
        Type = value.Type;
        Next = [];
        Value = value;
    }
}