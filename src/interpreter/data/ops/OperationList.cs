using System.Globalization;
using sonlanglib.interpreter.calculator;
using sonlanglib.interpreter.conversion;
using sonlanglib.interpreter.error;
using sonlanglib.interpreter.tokenizer;
using sonlanglib.shared;
using sonlanglib.shared.trees;

namespace sonlanglib.interpreter.data.ops;

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
    private static Interpreter? _interpreter;
    private static ValueOperations? _valOps;
    
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
                                                                            Priority.Highest
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
                                                              new Operation(
                                                                            "&",
                                                                            Ref,
                                                                            OpScope.Right,
                                                                            Priority.Highest
                                                                           ),
                                                              new Operation(
                                                                            "*",
                                                                            Deref,
                                                                            OpScope.Right,
                                                                            Priority.Highest
                                                                           ),
                                                          ];
    

    public static void Initialize(Interpreter interpreter) {
        _interpreter = interpreter;

        if (Converter == null) return;
        _valOps = new ValueOperations(Converter);
    }
    
    public static Operation? GetOperation(string name, OpScope scope = OpScope.None) {
        if (string.IsNullOrEmpty(name)) return null;
        
        var candidates = _operations.Where(op => op.Name.Equals(name)).ToList();
        if (candidates.Count <= 0) return null;
        
        var result = candidates[0];
        
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
            _interpreter.GetVariable(left.Data.Value.Val)
         ?? _interpreter.SetVariable(left.Data.Value.Val, right.Data);

        if (variable == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.SmthWentWrong);
        
        _interpreter.SetVariable(variable.Name, right.Data);
        return new Result<BinaryTree<ExpressionToken>?, Error?>(new BinaryTree<ExpressionToken>(right), null);
    }
    
    private static Result<BinaryTree<ExpressionToken>?, Error?> Addition(BinaryTreeNode<ExpressionToken>? left, BinaryTreeNode<ExpressionToken>? right) {
        if (Converter == null 
         || _valOps == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.InterpreterNotInitialized);
        
        if (right == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.IllegalOperation);
        if (left == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(new BinaryTree<ExpressionToken>(right), null);

        if (left.Data.Type == ExpressionTokenType.Array || right.Data.Type == ExpressionTokenType.Array) {
            return _valOps.ApplyOperationOnArrayLeft(left, right, _valOps.Add);
        }
        
        var result = _valOps.Add(left.Data.Value, right.Data.Value);
        if (!result.Ok) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, result.Error);
        if (result.Value == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.SmthWentWrong);

        var token = new ExpressionToken(result.Value);
        return new Result<BinaryTree<ExpressionToken>?, Error?>(new BinaryTree<ExpressionToken>(token), null);
    }
    
    private static Result<BinaryTree<ExpressionToken>?, Error?> Subtraction(BinaryTreeNode<ExpressionToken>? left, BinaryTreeNode<ExpressionToken>? right) {
        if (Converter == null 
         || _valOps == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.InterpreterNotInitialized);
        
        if (right == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.IllegalOperation);
        left ??= new BinaryTreeNode<ExpressionToken>(new ExpressionToken("0", ExpressionTokenType.Number));

        if (left.Data.Type == ExpressionTokenType.Array || right.Data.Type == ExpressionTokenType.Array) {
            return _valOps.ApplyOperationOnArrayLeft(left, right, _valOps.Subtract);
        }
        
        var result = _valOps.Subtract(left.Data.Value, right.Data.Value);
        if (!result.Ok) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, result.Error);
        if (result.Value == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.SmthWentWrong);

        var token = new ExpressionToken(result.Value);
        return new Result<BinaryTree<ExpressionToken>?, Error?>(new BinaryTree<ExpressionToken>(token), null);
    }
    
    private static Result<BinaryTree<ExpressionToken>?, Error?> Multiplication(BinaryTreeNode<ExpressionToken>? left, BinaryTreeNode<ExpressionToken>? right) {
        if (Converter == null 
         || _valOps == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.InterpreterNotInitialized);
        
        if (right == null || left == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.IllegalOperation);
        
        if (left.Data.Type == ExpressionTokenType.Array || right.Data.Type == ExpressionTokenType.Array) {
            return _valOps.ApplyOperationOnArrayStrict(left, right, _valOps.Multiply);
        }
        
        var result = _valOps.Multiply(left.Data.Value, right.Data.Value);
        if (!result.Ok) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, result.Error);
        if (result.Value == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.SmthWentWrong);

        var token = new ExpressionToken(result.Value);
        return new Result<BinaryTree<ExpressionToken>?, Error?>(new BinaryTree<ExpressionToken>(token), null);
    }
    
    private static Result<BinaryTree<ExpressionToken>?, Error?> Division(BinaryTreeNode<ExpressionToken>? left, BinaryTreeNode<ExpressionToken>? right) {
        if (Converter == null 
         || _valOps == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.InterpreterNotInitialized);
        
        if (right == null || left == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.IllegalOperation);
        
        if (left.Data.Type == ExpressionTokenType.Array || right.Data.Type == ExpressionTokenType.Array) {
            return _valOps.ApplyOperationOnArrayStrict(left, right, _valOps.Divide);
        }
        
        var result = _valOps.Divide(left.Data.Value, right.Data.Value);
        if (!result.Ok) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, result.Error);
        if (result.Value == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.SmthWentWrong);

        var token = new ExpressionToken(result.Value);
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
                        null => new ExpressionToken(((double)lval / 100).ToString(CultureInfo.InvariantCulture), ExpressionTokenType.Number),
                        _ => new ExpressionToken(((double)lval % (double)rval).ToString(CultureInfo.InvariantCulture), ExpressionTokenType.Number),
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

        var token = new ExpressionToken((Math.Pow((double)lval, (double)rval)).ToString(CultureInfo.InvariantCulture), ExpressionTokenType.Number);
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

        var token = new ExpressionToken(Converter.LogicalBoolToBool(lval != 0 && rval != 0).ToString(CultureInfo.InvariantCulture), ExpressionTokenType.Bool);
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

        var token = new ExpressionToken(Converter.LogicalBoolToBool(lval != 0 || rval != 0).ToString(CultureInfo.InvariantCulture), ExpressionTokenType.Bool);
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
        
        var token = new ExpressionToken(Converter.LogicalBoolToBool(rval == 0).ToString(CultureInfo.InvariantCulture), ExpressionTokenType.Bool);
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

        var token = new ExpressionToken(Converter.LogicalBoolToBool(lval.Equals(rval)).ToString(CultureInfo.InvariantCulture), ExpressionTokenType.Bool);
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

        var token = new ExpressionToken(Converter.LogicalBoolToBool(!lval.Equals(rval)).ToString(CultureInfo.InvariantCulture), ExpressionTokenType.Bool);
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

        var token = new ExpressionToken(Converter.LogicalBoolToBool(lval > rval).ToString(CultureInfo.InvariantCulture), ExpressionTokenType.Bool);
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

        var token = new ExpressionToken(Converter.LogicalBoolToBool(lval < rval).ToString(CultureInfo.InvariantCulture), ExpressionTokenType.Bool);
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

        var token = new ExpressionToken(Converter.LogicalBoolToBool(lval >= rval).ToString(CultureInfo.InvariantCulture), ExpressionTokenType.Bool);
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

        var token = new ExpressionToken(Converter.LogicalBoolToBool(lval <= rval).ToString(CultureInfo.InvariantCulture), ExpressionTokenType.Bool);
        return new Result<BinaryTree<ExpressionToken>?, Error?>(new BinaryTree<ExpressionToken>(token), null);
    }
    
    private static Result<BinaryTree<ExpressionToken>?, Error?> Ref(BinaryTreeNode<ExpressionToken>? left, BinaryTreeNode<ExpressionToken>? right) {
        if (_interpreter == null || Converter == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.InterpreterNotInitialized);
        if (right == null 
         || !Converter.IsReferencable(right.Data)) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.IllegalOperation);

        var variable = _interpreter.GetVariable(right.Data.Value.Val);
        if (variable == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.IllegalOperation);
        
        var token = new ExpressionToken(variable.Name, ExpressionTokenType.Reference);
        return new Result<BinaryTree<ExpressionToken>?, Error?>(new BinaryTree<ExpressionToken>(token), null);
    }
    
    private static Result<BinaryTree<ExpressionToken>?, Error?> Deref(BinaryTreeNode<ExpressionToken>? left, BinaryTreeNode<ExpressionToken>? right) {
        if (_interpreter == null || Converter == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.InterpreterNotInitialized);
        if (right == null || !Converter.IsReferencable(right.Data)) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.IllegalOperation);
        
        var reference = _interpreter.GetVariable(right.Data.Value.Val);
        if (reference == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.IllegalOperation);
        
        var deref = _interpreter.GetVariable(reference.Value.Value.Val);
        if (deref == null) {
            var derefToken = Converter.VarValueToToken(reference.Value);
            return new Result<BinaryTree<ExpressionToken>?, Error?>(new BinaryTree<ExpressionToken>(derefToken), null);
        }
        
        var token = Converter.VarValueToToken(deref.Value);
        return new Result<BinaryTree<ExpressionToken>?, Error?>(new BinaryTree<ExpressionToken>(token), null);
    }
}