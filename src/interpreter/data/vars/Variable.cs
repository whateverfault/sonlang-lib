namespace sonlanglib.interpreter.data.vars;

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

    public readonly VarValue Value;
    
    
    public Variable(string name, VarValue value) {
        Name = name;
        Type = value.Type;
        Value = value;
    }
}