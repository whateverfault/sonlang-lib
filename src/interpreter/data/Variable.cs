namespace sonlanglib.interpreter.data;

public enum VariableType {
    String,
    Number,
    Bool,
    Array,
    Reference,
}

public class Variable {
    public string Name { get; }
    
    public VariableType Type { get; }
    
    public List<VarValue> Values { get; private set; }
    
    public VarValue Value {
        get => Values[0];
        set => Values = [value,];
    }
    
    
    public Variable(string name, VariableType type, List<VarValue> values) {
        Name = name;
        Type = type;
        Values = values;
    }
}