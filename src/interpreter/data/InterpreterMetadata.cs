namespace sonlanglib.interpreter.data;

public class InterpreterMetadata {
    private Dictionary<string, Variable> _variables;


    public InterpreterMetadata() {
        _variables = new Dictionary<string, Variable>();
    }
    
    public Variable SetVariable(string name, VariableType type, List<VarValue> vals) {
        var var = new Variable(name, type, vals);
        if (!_variables.TryAdd(name, var)) {
            _variables[name] = var;
        }
        return var;
    }

    public Variable? GetVariable(string name) {
        _variables.TryGetValue(name, out var var);
        return var;
    }

    public void LoadVariables(Dictionary<string, Variable> vars) {
        _variables = vars;
    }
}