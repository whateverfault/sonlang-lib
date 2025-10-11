using System.Globalization;
using System.Text;
using sonlanglib.interpreter.calculator;
using sonlanglib.interpreter.conversion;
using sonlanglib.interpreter.error;
using sonlanglib.interpreter.lexer;
using sonlanglib.shared;
using sonlanglib.shared.trees;

namespace sonlanglib.interpreter.data;

public enum OpScope {
    None = 0,
    Left = 1,
    Right = 2,
    LeftRight = 3,
}

public enum Priority {
    SpecialLow,
    Lowest,
    Low,
    High,
    Highest,
}

public static class OperationList {
    private static readonly StringBuilder _sb = new StringBuilder(); 
    private static Interpreter? _interpreter;

    private static Calculator? Calculator => _interpreter?.Calculator;
    private static TypeConverter? Converter => _interpreter?.TypeConverter;
    
    private static readonly List<Operation> _operations = [
                                                              new Operation(
                                                                            "+",
                                                                            Addition,
                                                                            OpScope.LeftRight,
                                                                            Priority.Lowest
                                                                           ),
                                                              new Operation(
                                                                            "+",
                                                                            Addition,
                                                                            OpScope.Right,
                                                                            Priority.Highest
                                                                           ),
                                                              new Operation(
                                                                            "-",
                                                                            Subtraction,
                                                                            OpScope.LeftRight,
                                                                            Priority.Lowest
                                                                           ),
                                                              new Operation(
                                                                            "-",
                                                                            Subtraction,
                                                                            OpScope.Right,
                                                                            Priority.Highest
                                                                           ),
                                                              new Operation(
                                                                            "*",
                                                                            Multiplication,
                                                                            OpScope.LeftRight,
                                                                            Priority.High
                                                                           ),
                                                              new Operation(
                                                                            "/",
                                                                            Division,
                                                                            OpScope.LeftRight,
                                                                            Priority.High
                                                                           ),
                                                              new Operation(
                                                                            "%",
                                                                            Modulo,
                                                                            OpScope.LeftRight,
                                                                            Priority.High
                                                                           ),
                                                              new Operation(
                                                                            "%",
                                                                            Modulo,
                                                                            OpScope.Left,
                                                                            Priority.Highest
                                                                           ),
                                                              new Operation(
                                                                            "^",
                                                                            Exponentiation,
                                                                            OpScope.LeftRight,
                                                                            Priority.Low
                                                                           ),
                                                              new Operation(
                                                                            "&",
                                                                            And,
                                                                            OpScope.LeftRight,
                                                                            Priority.Low
                                                                           ),
                                                              new Operation(
                                                                            "|",
                                                                            Or,
                                                                            OpScope.LeftRight,
                                                                            Priority.Low
                                                                           ),
                                                              new Operation(
                                                                            "!",
                                                                            Not,
                                                                            OpScope.Right,
                                                                            Priority.Highest
                                                                           ),
                                                              new Operation(
                                                                            "==",
                                                                            Equal,
                                                                            OpScope.LeftRight,
                                                                            Priority.Low
                                                                           ),
                                                              new Operation(
                                                                            "!=",
                                                                            NotEqual,
                                                                            OpScope.LeftRight,
                                                                            Priority.Low
                                                                           ),
                                                              new Operation(
                                                                            ">",
                                                                            Greater,
                                                                            OpScope.LeftRight,
                                                                            Priority.Low
                                                                           ),
                                                              new Operation(
                                                                            "<",
                                                                            Less,
                                                                            OpScope.LeftRight,
                                                                            Priority.Low
                                                                           ),
                                                              new Operation(
                                                                            ">=",
                                                                            GreaterOrEqual,
                                                                            OpScope.LeftRight,
                                                                            Priority.Low
                                                                           ),
                                                              new Operation(
                                                                            "<=",
                                                                            LessOrEqual,
                                                                            OpScope.LeftRight,
                                                                            Priority.Low
                                                                           ),
                                                              new Operation(
                                                                            "=",
                                                                            Assigment,
                                                                            OpScope.LeftRight,
                                                                            Priority.SpecialLow
                                                                           ),
                                                          ];
    

    public static void Initialize(Interpreter interpreter) {
        _interpreter = interpreter;
    }
    
    public static Operation? GetOperation(string name, OpScope scope = OpScope.None) {
        if (string.IsNullOrEmpty(name)) return null;
        
        var candidates = _operations.Where(op => op.Name.Equals(name)).ToList();
        if (candidates.Count <= 0) return null;
        
        var result = candidates[0];
        _sb.Clear();
        
        if (scope != OpScope.None && result.Scope != scope) {
            return candidates
                  .Where(candidate => candidate.Scope == scope)
                  .Select(candidate => candidate)
                  .FirstOrDefault();
        }
        return result;
    }

    private static Result<BinaryTree<ExpressionToken>?, Error?> Assigment(BinaryTreeNode<ExpressionToken>? left, BinaryTreeNode<ExpressionToken>? right) {
        if (Converter == null
         || _interpreter == null
         || Calculator == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.InterpreterNotInitialized);
        if (left == null || right == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.InvalidSyntax);
        
        if (left.Data.Type != ExpressionTokenType.Name) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.IllegalOperation);
        
        var result = Calculator.Calculate(new BinaryTree<ExpressionToken>(right));
        if (!result.Ok) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, result.Error);
        if (result.Value == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.SmthWentWrong);

        right.Data = result.Value;
        var variable = 
            _interpreter.GetVariable(left.Data.Value)
         ?? _interpreter.SetVariable(left.Data.Value, right.Data.Value, Converter.TokenTypeToVarType(right.Data.Type));

        if (variable == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.SmthWentWrong);
        
        _interpreter.SetVariable(variable.Name, right.Data.Value, Converter.TokenTypeToVarType(right.Data.Type));
        return new Result<BinaryTree<ExpressionToken>?, Error?>(new BinaryTree<ExpressionToken>(right), null);
    }
    
    private static Result<BinaryTree<ExpressionToken>?, Error?> Addition(BinaryTreeNode<ExpressionToken>? left, BinaryTreeNode<ExpressionToken>? right) {
        if (Converter == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.InterpreterNotInitialized);
        if (right == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.IllegalOperation);
        if (left == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(new BinaryTree<ExpressionToken>(right), null);

        var result = Converter.ParseNumbers(left, right);
        if (!result.Ok) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, result.Error);
        if (result.Value == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.SmthWentWrong);

        var lval = result.Value.Left ?? 0;
        var rval = result.Value.Right;
        if (Converter.IsLiteral(left.Data) || Converter.IsLiteral(right.Data)) {
            _sb.Append($"{left.Data.Value}{right.Data.Value}");
            var strToken = new ExpressionToken(ExpressionTokenType.String, _sb.ToString());
            return new Result<BinaryTree<ExpressionToken>?, Error?>(new BinaryTree<ExpressionToken>(strToken), null);
        }
        
        if (rval == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.IllegalOperation);
        var numToken = new ExpressionToken(ExpressionTokenType.Number, (lval + (double)rval).ToString(CultureInfo.InvariantCulture));
        return new Result<BinaryTree<ExpressionToken>?, Error?>(new BinaryTree<ExpressionToken>(numToken), null);
    }
    
    private static Result<BinaryTree<ExpressionToken>?, Error?> Subtraction(BinaryTreeNode<ExpressionToken>? left, BinaryTreeNode<ExpressionToken>? right) {
        if (Converter == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.InterpreterNotInitialized);
        if (right == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.IllegalOperation);
        if (left == null) {
            right.Data.Value = $"-{right.Data.Value}";
            return new Result<BinaryTree<ExpressionToken>?, Error?>(new BinaryTree<ExpressionToken>(right), null);
        }
        
        var result = Converter.ParseNumbers(left, right);
        if (!result.Ok) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, result.Error);
        if (result.Value == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.SmthWentWrong);
        
        var lval = result.Value.Left ?? 0;
        var rval = result.Value.Right;
        if (rval is null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.IllegalOperation);
        
        var token = new ExpressionToken(ExpressionTokenType.Number, (lval - (double)rval).ToString(CultureInfo.InvariantCulture));
        return new Result<BinaryTree<ExpressionToken>?, Error?>(new BinaryTree<ExpressionToken>(token), null);
    }
    
    private static Result<BinaryTree<ExpressionToken>?, Error?> Multiplication(BinaryTreeNode<ExpressionToken>? left, BinaryTreeNode<ExpressionToken>? right) {
        if (Converter == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.InterpreterNotInitialized);
        if (right == null || left == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.IllegalOperation);
        
        var result = Converter.ParseNumbers(left, right);
        if (!result.Ok) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, result.Error);
        if (result.Value == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.SmthWentWrong);
        
        var lval = result.Value.Left;
        var rval = result.Value.Right;
        if (rval is null || lval is null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.IllegalOperation);
        
        var token = new ExpressionToken(ExpressionTokenType.Number, ((double)lval * (double)rval).ToString(CultureInfo.InvariantCulture));
        return new Result<BinaryTree<ExpressionToken>?, Error?>(new BinaryTree<ExpressionToken>(token), null);
    }
    
    private static Result<BinaryTree<ExpressionToken>?, Error?> Division(BinaryTreeNode<ExpressionToken>? left, BinaryTreeNode<ExpressionToken>? right) {
        if (Converter == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.InterpreterNotInitialized);
        if (right == null || left == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.IllegalOperation);
        
        var result = Converter.ParseNumbers(left, right);
        if (!result.Ok) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, result.Error);
        if (result.Value == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.SmthWentWrong);
        
        var lval = result.Value.Left;
        var rval = result.Value.Right;
        if (rval is null || lval is null || rval == 0.0) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.IllegalOperation);
        
        var token = new ExpressionToken(ExpressionTokenType.Number, ((double)lval / (double)rval).ToString(CultureInfo.InvariantCulture));
        return new Result<BinaryTree<ExpressionToken>?, Error?>(new BinaryTree<ExpressionToken>(token), null);
    }
    
    private static Result<BinaryTree<ExpressionToken>?, Error?> Modulo(BinaryTreeNode<ExpressionToken>? left, BinaryTreeNode<ExpressionToken>? right) {
        if (Converter == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.InterpreterNotInitialized);
        if (left == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.IllegalOperation);
        
        var result = Converter.ParseNumbers(left, right);
        if (!result.Ok) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, result.Error);
        if (result.Value == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.SmthWentWrong);
        
        var lval = result.Value.Left;
        var rval = result.Value.Right;
        if (lval is null || rval == 0) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.IllegalOperation);
        
        var token = rval switch {
                        null => new ExpressionToken(ExpressionTokenType.Number, ((double)lval / 100).ToString(CultureInfo.InvariantCulture)),
                        _ => new ExpressionToken(ExpressionTokenType.Number, ((double)lval % (double)rval).ToString(CultureInfo.InvariantCulture)),
                    };
        return new Result<BinaryTree<ExpressionToken>?, Error?>(new BinaryTree<ExpressionToken>(token), null);
    }
    
    private static Result<BinaryTree<ExpressionToken>?, Error?> Exponentiation(BinaryTreeNode<ExpressionToken>? left, BinaryTreeNode<ExpressionToken>? right) {
        if (Converter == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.InterpreterNotInitialized);
        if (right == null || left == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.IllegalOperation);
        
        var result = Converter.ParseNumbers(left, right);
        if (!result.Ok) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, result.Error);
        if (result.Value == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.SmthWentWrong);
        
        var lval = result.Value.Left;
        var rval = result.Value.Right;
        if (rval is null || lval is null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.IllegalOperation);

        var token = new ExpressionToken(ExpressionTokenType.Number, (Math.Pow((double)lval, (double)rval)).ToString(CultureInfo.InvariantCulture));
        return new Result<BinaryTree<ExpressionToken>?, Error?>(new BinaryTree<ExpressionToken>(token), null);
    }
    
    private static Result<BinaryTree<ExpressionToken>?, Error?> And(BinaryTreeNode<ExpressionToken>? left, BinaryTreeNode<ExpressionToken>? right) {
        if (Converter == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.InterpreterNotInitialized);
        if (right == null || left == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.IllegalOperation);
        
        var result = Converter.ParseNumbers(left, right);
        if (!result.Ok) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, result.Error);
        if (result.Value == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.SmthWentWrong);
        
        var lval = result.Value.Left;
        var rval = result.Value.Right;
        if (rval is null || lval is null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.IllegalOperation);

        var token = new ExpressionToken(ExpressionTokenType.Bool, Converter.LogicalBoolToBool(lval != 0 && rval != 0).ToString(CultureInfo.InvariantCulture));
        return new Result<BinaryTree<ExpressionToken>?, Error?>(new BinaryTree<ExpressionToken>(token), null);
    }
    
    private static Result<BinaryTree<ExpressionToken>?, Error?> Or(BinaryTreeNode<ExpressionToken>? left, BinaryTreeNode<ExpressionToken>? right) {
        if (Converter == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.InterpreterNotInitialized);
        if (right == null || left == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.IllegalOperation);

        var result = Converter.ParseNumbers(left, right);
        if (!result.Ok) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, result.Error);
        if (result.Value == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.SmthWentWrong);
        
        var lval = result.Value.Left;
        var rval = result.Value.Right;
        if (rval is null || lval is null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.IllegalOperation);

        var token = new ExpressionToken(ExpressionTokenType.Bool, Converter.LogicalBoolToBool(lval != 0 || rval != 0).ToString(CultureInfo.InvariantCulture));
        return new Result<BinaryTree<ExpressionToken>?, Error?>(new BinaryTree<ExpressionToken>(token), null);
    }
    
    private static Result<BinaryTree<ExpressionToken>?, Error?> Not(BinaryTreeNode<ExpressionToken>? left, BinaryTreeNode<ExpressionToken>? right) {
        if (Converter == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.InterpreterNotInitialized);
        if (right == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.IllegalOperation);
        
        var result = Converter.ParseNumbers(left, right);
        if (!result.Ok) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, result.Error);
        if (result.Value == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.SmthWentWrong);
        
        var rval = result.Value.Right;
        if (rval is null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.IllegalOperation);
        
        var token = new ExpressionToken(ExpressionTokenType.Bool, Converter.LogicalBoolToBool(rval == 0).ToString(CultureInfo.InvariantCulture));
        return new Result<BinaryTree<ExpressionToken>?, Error?>(new BinaryTree<ExpressionToken>(token), null);
    }
    
    private static Result<BinaryTree<ExpressionToken>?, Error?> Equal(BinaryTreeNode<ExpressionToken>? left, BinaryTreeNode<ExpressionToken>? right) {
        if (Converter == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.InterpreterNotInitialized);
        if (right == null || left == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.IllegalOperation);
        
        var result = Converter.ParseNumbers(left, right);
        if (!result.Ok) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, result.Error);
        if (result.Value == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.SmthWentWrong);
        
        var lval = result.Value.Left;
        var rval = result.Value.Right;
        if (rval is null || lval is null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.IllegalOperation);

        var token = new ExpressionToken(ExpressionTokenType.Bool, Converter.LogicalBoolToBool(lval.Equals(rval)).ToString(CultureInfo.InvariantCulture));
        return new Result<BinaryTree<ExpressionToken>?, Error?>(new BinaryTree<ExpressionToken>(token), null);
    }
    
    private static Result<BinaryTree<ExpressionToken>?, Error?> NotEqual(BinaryTreeNode<ExpressionToken>? left, BinaryTreeNode<ExpressionToken>? right) {
        if (Converter == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.InterpreterNotInitialized);
        if (right == null || left == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.IllegalOperation);
        
        var result = Converter.ParseNumbers(left, right);
        if (!result.Ok) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, result.Error);
        if (result.Value == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.SmthWentWrong);
        
        var lval = result.Value.Left;
        var rval = result.Value.Right;
        if (rval is null || lval is null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.IllegalOperation);

        var token = new ExpressionToken(ExpressionTokenType.Bool, Converter.LogicalBoolToBool(!lval.Equals(rval)).ToString(CultureInfo.InvariantCulture));
        return new Result<BinaryTree<ExpressionToken>?, Error?>(new BinaryTree<ExpressionToken>(token), null);
    }
    
    private static Result<BinaryTree<ExpressionToken>?, Error?> Greater(BinaryTreeNode<ExpressionToken>? left, BinaryTreeNode<ExpressionToken>? right) {
        if (Converter == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.InterpreterNotInitialized);
        if (right == null || left == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.IllegalOperation);
        
        var result = Converter.ParseNumbers(left, right);
        if (!result.Ok) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, result.Error);
        if (result.Value == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.SmthWentWrong);
        
        var lval = result.Value.Left;
        var rval = result.Value.Right;
        if (rval is null || lval is null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.IllegalOperation);

        var token = new ExpressionToken(ExpressionTokenType.Bool, Converter.LogicalBoolToBool(lval > rval).ToString(CultureInfo.InvariantCulture));
        return new Result<BinaryTree<ExpressionToken>?, Error?>(new BinaryTree<ExpressionToken>(token), null);
    }
    
    private static Result<BinaryTree<ExpressionToken>?, Error?> Less(BinaryTreeNode<ExpressionToken>? left, BinaryTreeNode<ExpressionToken>? right) {
        if (Converter == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.InterpreterNotInitialized);
        if (right == null || left == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.IllegalOperation);
        
        var result = Converter.ParseNumbers(left, right);
        if (!result.Ok) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, result.Error);
        if (result.Value == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.SmthWentWrong);
        
        var lval = result.Value.Left;
        var rval = result.Value.Right;
        if (rval is null || lval is null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.IllegalOperation);

        var token = new ExpressionToken(ExpressionTokenType.Bool, Converter.LogicalBoolToBool(lval < rval).ToString(CultureInfo.InvariantCulture));
        return new Result<BinaryTree<ExpressionToken>?, Error?>(new BinaryTree<ExpressionToken>(token), null);
    }
    
    private static Result<BinaryTree<ExpressionToken>?, Error?> GreaterOrEqual(BinaryTreeNode<ExpressionToken>? left, BinaryTreeNode<ExpressionToken>? right) {
        if (Converter == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.InterpreterNotInitialized);
        if (right == null || left == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.IllegalOperation);
        
        var result = Converter.ParseNumbers(left, right);
        if (!result.Ok) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, result.Error);
        if (result.Value == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.SmthWentWrong);
        
        var lval = result.Value.Left;
        var rval = result.Value.Right;
        if (rval is null || lval is null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.IllegalOperation);

        var token = new ExpressionToken(ExpressionTokenType.Bool, Converter.LogicalBoolToBool(lval >= rval).ToString(CultureInfo.InvariantCulture));
        return new Result<BinaryTree<ExpressionToken>?, Error?>(new BinaryTree<ExpressionToken>(token), null);
    }
    
    private static Result<BinaryTree<ExpressionToken>?, Error?> LessOrEqual(BinaryTreeNode<ExpressionToken>? left, BinaryTreeNode<ExpressionToken>? right) {
        if (Converter == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.InterpreterNotInitialized);
        if (right == null || left == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.IllegalOperation);
        
        var result = Converter.ParseNumbers(left, right);
        if (!result.Ok) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, result.Error);
        if (result.Value == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.SmthWentWrong);
        
        var lval = result.Value.Left;
        var rval = result.Value.Right;
        if (rval is null || lval is null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.IllegalOperation);

        var token = new ExpressionToken(ExpressionTokenType.Bool, Converter.LogicalBoolToBool(lval <= rval).ToString(CultureInfo.InvariantCulture));
        return new Result<BinaryTree<ExpressionToken>?, Error?>(new BinaryTree<ExpressionToken>(token), null);
    }
}