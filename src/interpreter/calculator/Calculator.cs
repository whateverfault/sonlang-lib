using sonlanglib.interpreter.conversion;
using sonlanglib.interpreter.data.ops;
using sonlanglib.interpreter.error;
using sonlanglib.interpreter.tokenizer;
using sonlanglib.shared;
using sonlanglib.shared.trees;

namespace sonlanglib.interpreter.calculator;

public class Calculator {
    private readonly TypeConverter _converter;
    
    
    public Calculator(TypeConverter converter) {
        _converter = converter;
    }
    
    public Result<ExpressionToken?, Error?> Calculate(BinaryTree<ExpressionToken> expression) {
        var ast = expression;
        if (ast.Root.Parent != null) {
            ast = new BinaryTree<ExpressionToken>(expression) {
                                                                   Root = {
                                                                              Parent = null,
                                                                          },
                                                               };
        }
        
        var root = ast.Root;
        var current = root;
        
        while (true) {
            var data = current.Data;
            if (!_converter.IsOperation(current.Data)) current = current.Parent;
            if (current == null) {
                current = root;
                break;
            }

            var left = current.Left?.Data;
            var right = current.Right?.Data;
            if (data.Type == ExpressionTokenType.Operation
             && left == null
             && right == null) {
                var susOp = GetOperation(data.Value.Val);
                if (susOp is not { Scope: OpScope.None, }) return new Result<ExpressionToken?, Error?>(null, Error.InvalidSyntax);
            }
            
            if (current.Left != null && left?.Type is ExpressionTokenType.Operation) {
                current = current.Left;
                continue;
            } if (current.Right != null && right?.Type is ExpressionTokenType.Operation) {
                current = current.Right;
                continue;
            }
            
            var scope = OpScope.None;
            if (left != null) scope = OpScope.Left;
            if (right != null) scope = scope == OpScope.Left ? OpScope.LeftRight : OpScope.Right;
            
            var op = GetOperation(current.Data.Value.Val, scope);
            if (op == null) return new Result<ExpressionToken?, Error?>(null, Error.IllegalOperation);
            
            var result = op.Evaluation.Invoke(current.Left, current.Right);
            if (!result.Ok) return new Result<ExpressionToken?, Error?>(null, result.Error);
            if (result.Value == null) return new Result<ExpressionToken?, Error?>(null, Error.SmthWentWrong);

            current.Left = null;
            current.Right = null;
            current.Data = result.Value.Root.Data;
        }

        if (current.Data.Type == ExpressionTokenType.Name) {
            if (!_converter.AssignVarToName(current.Data)) return new Result<ExpressionToken?, Error?>(null, Error.UnknownIdentifier);
        }
        return new Result<ExpressionToken?, Error?>(current.Data, null);
    }
    
    private Operation? GetOperation(string name, OpScope scope = OpScope.None) {
        var operation = OperationList.GetOperation(name, scope);
        return operation;
    }
}