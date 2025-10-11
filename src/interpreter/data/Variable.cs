namespace sonlanglib.interpreter.data;

public enum VariableType {
    String,
    Number,
    Bool,
    Array,
}

public class Variable {
    public string Name { get; }
    
    public VariableType Type { get; }
    
    public List<string> Values { get; }
    
    public string Value => Values[0];
    
    
    public Variable(string name, VariableType type, List<string> values) {
        Name = name;
        Type = type;
        Values = values;
    }
}