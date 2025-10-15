namespace sonlanglib.interpreter.data.vars;

public class SubValue {
    public readonly string Val;
    public VariableType Type;


    public SubValue(string val, VariableType type) {
        Val = val;
        Type = type;
    }
}