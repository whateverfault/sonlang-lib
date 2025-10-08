namespace sonlanglib.interpreter.data;

public enum VariableType {
    String,
    Number,
    Bool,
}

public class Variable {
    public string Name { get; private set; }
    public VariableType Type { get; private set; }
    public string Value { get; private set; }

    
    public Variable(string name, VariableType type, string value) {
        Name = name;
        Type = type;
        Value = value;
    }
}