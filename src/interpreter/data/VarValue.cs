namespace sonlanglib.interpreter.data;

public class VarValue {
    public string Val;
    public VariableType Type;


    public VarValue(string val, VariableType type) {
        Val = val;
        Type = type;
    }
}