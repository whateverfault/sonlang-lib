using System.Globalization;
using System.Text;
using sonlanglib.interpreter.conversion;
using sonlanglib.interpreter.data.vars;
using sonlanglib.interpreter.error;
using sonlanglib.interpreter.tokenizer;
using sonlanglib.shared;
using sonlanglib.shared.trees;

namespace sonlanglib.interpreter.data.ops;

public delegate Result<Value?, Error?> ActionOnValues(Value left, Value right); 

public class ValueOperations {
    private readonly StringBuilder _sb = new StringBuilder(); 
    private readonly TypeConverter _converter;
    

    public ValueOperations(TypeConverter converter) {
        _converter = converter;
    }
    
    public Result<BinaryTree<ExpressionToken>?, Error?> ApplyOperationOnArrayLeft(BinaryTreeNode<ExpressionToken>? left, BinaryTreeNode<ExpressionToken>? right, ActionOnValues action) {
        if (right == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.IllegalOperation);
        if (left == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(new BinaryTree<ExpressionToken>(right), null);

        var isArrayLeft = left.Data.Type == ExpressionTokenType.Array;
        var array = left.Data.Type == ExpressionTokenType.Array?
                        left.Data :
                        right.Data;
        if (array.Type != ExpressionTokenType.Array) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.IllegalOperation);

        var value = _converter.IsNotArrayValue(left.Data)? 
                        left.Data :
                        right.Data;
        if (!_converter.IsNotArrayValue(value)) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.IllegalOperation);

        var result = ApplyOperationOnArray(array, value, action, isArrayLeft);
        if (!result.Ok) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, result.Error);
        if (result.Value == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.SmthWentWrong);
        
        return new Result<BinaryTree<ExpressionToken>?, Error?>(new BinaryTree<ExpressionToken>(result.Value), null);
    }
    
    public Result<BinaryTree<ExpressionToken>?, Error?> ApplyOperationOnArrayStrict(BinaryTreeNode<ExpressionToken>? left, BinaryTreeNode<ExpressionToken>? right, ActionOnValues action) {
        if (right == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.IllegalOperation);
        if (left == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(new BinaryTree<ExpressionToken>(right), null);

        var isArrayLeft = left.Data.Type == ExpressionTokenType.Array;
        var array = left.Data.Type == ExpressionTokenType.Array?
                        left.Data :
                        right.Data;
        if (array.Type != ExpressionTokenType.Array) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.IllegalOperation);

        var value = _converter.IsNotArrayValue(left.Data)? 
                        left.Data :
                        right.Data;
        if (!_converter.IsNotArrayValue(value)) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.IllegalOperation);

        var result = ApplyOperationOnArray(array, value, action, isArrayLeft);
        if (!result.Ok) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, result.Error);
        if (result.Value == null) return new Result<BinaryTree<ExpressionToken>?, Error?>(null, Error.SmthWentWrong);
        
        return new Result<BinaryTree<ExpressionToken>?, Error?>(new BinaryTree<ExpressionToken>(result.Value), null);
    }
    
    public Result<Value?, Error?> Add(Value left, Value right) {
        if (_converter.IsLiteral(left) || _converter.IsLiteral(right)) {
            _sb.Append($"{left.Val}{right.Val}");
            var val = new Value(_sb.ToString(), ExpressionTokenType.String);
            _sb.Clear();
            return new Result<Value?, Error?>(val, null);
        }
        
        var result = _converter.ParseNumbers(left, right);
        if (!result.Ok) return new Result<Value?, Error?>(null, result.Error);
        if (result.Value == null) return new Result<Value?, Error?>(null, Error.SmthWentWrong);

        var lval = result.Value.Left ?? 0;
        var rval = result.Value.Right;
        
        if (rval == null) return new Result<Value?, Error?>(null, Error.IllegalOperation);
        var numVal = new Value((lval + (double)rval).ToString(CultureInfo.InvariantCulture), ExpressionTokenType.Number);
        return new Result<Value?, Error?>(numVal, null);
    }
    
    public Result<Value?, Error?> Subtract(Value left, Value right) {
        var result = _converter.ParseNumbers(left, right);
        if (!result.Ok) return new Result<Value?, Error?>(null, result.Error);
        if (result.Value == null) return new Result<Value?, Error?>(null, Error.SmthWentWrong);

        var lval = result.Value.Left ?? 0;
        var rval = result.Value.Right;
        
        if (rval == null) return new Result<Value?, Error?>(null, Error.IllegalOperation);
        var numVal = new Value((lval - (double)rval).ToString(CultureInfo.InvariantCulture), ExpressionTokenType.Number);
        return new Result<Value?, Error?>(numVal, null);
    }
    
    public Result<Value?, Error?> Multiply(Value left, Value right) {
        if (_converter.IsLiteral(left) || _converter.IsLiteral(right)) {
            var parseResult = _converter.ParseNumbers(left, right);
            if (!parseResult.Ok) return new Result<Value?, Error?>(null, parseResult.Error);
            if (parseResult.Value == null) return new Result<Value?, Error?>(null, Error.SmthWentWrong);

            var number = parseResult.Value.Left ?? parseResult.Value.Right;
            if (number < 0) return new Result<Value?, Error?>(null, Error.IllegalOperation);

            var lit = _converter.IsLiteral(left)?
                          left.Val :
                          right.Val;
            
            for (var i = 0d; i < number; ++i) {
                _sb.Append($"{lit}");
            }
            var val = new Value(_sb.ToString(), ExpressionTokenType.String);
            _sb.Clear();
            return new Result<Value?, Error?>(val, null);
        }
        
        var result = _converter.ParseNumbers(left, right);
        if (!result.Ok) return new Result<Value?, Error?>(null, result.Error);
        if (result.Value == null) return new Result<Value?, Error?>(null, Error.SmthWentWrong);

        var lval = result.Value.Left;
        var rval = result.Value.Right;
        
        if (rval == null || lval == null) return new Result<Value?, Error?>(null, Error.IllegalOperation);
        var numVal = new Value(((double)lval * (double)rval).ToString(CultureInfo.InvariantCulture), ExpressionTokenType.Number);
        return new Result<Value?, Error?>(numVal, null);
    }

    public Result<Value?, Error?> Divide(Value left, Value right) {
        var result = _converter.ParseNumbers(left, right);
        if (!result.Ok) return new Result<Value?, Error?>(null, result.Error);
        if (result.Value == null) return new Result<Value?, Error?>(null, Error.SmthWentWrong);

        var lval = result.Value.Left;
        var rval = result.Value.Right;
        
        if (rval == null || lval == null || rval == 0.0) return new Result<Value?, Error?>(null, Error.IllegalOperation);
        var numVal = new Value(((double)lval / (double)rval).ToString(CultureInfo.InvariantCulture), ExpressionTokenType.Number);
        return new Result<Value?, Error?>(numVal, null);
    }
    
    private Result<ExpressionToken?, Error?> ApplyOperationOnArray(ExpressionToken array, ExpressionToken value, ActionOnValues action, bool isArrayLeft) {
        foreach (var val in array.Next) {
            if (val.Type == ExpressionTokenType.Array) {
                ApplyOperationOnArray(val, value, action, isArrayLeft);
                continue;
            }
            
            var result = isArrayLeft?
                             action.Invoke(val.Value, value.Value) : 
                             action.Invoke(value.Value, val.Value);

            if (!result.Ok) return new Result<ExpressionToken?, Error?>(null, result.Error);
            if (result.Value == null) return new Result<ExpressionToken?, Error?>(null, Error.SmthWentWrong);

            val.Value = result.Value;
            val.Type = result.Value.Type;
        }

        return new Result<ExpressionToken?, Error?>(array, null);
    }
}