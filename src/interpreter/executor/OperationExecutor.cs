using sonlanglib.interpreter.conversion;
using sonlanglib.interpreter.data.ops;
using sonlanglib.interpreter.error;
using sonlanglib.interpreter.tokenizer;
using sonlanglib.shared;
using sonlanglib.shared.trees;

namespace sonlanglib.interpreter.executor;

public class OperationExecutor {
    private readonly TypeConverter _converter;
    
    
    public OperationExecutor(TypeConverter converter) {
        _converter = converter;
    }
    
    public Result<ExpressionToken?, Error?> Execute(BinaryTree<ExpressionToken> expression) {
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
            if (!_converter.IsOperation(data)) current = current.Parent;
            if (current == null) {
                current = root;
                break;
            }

            var left = current.Left?.Data;
            var right = current.Right?.Data;
            
            var operation = OperationList.GetOperation(data.Value.Val); 
            if (data.Type == ExpressionTokenType.Assigment
             && left == null
             && right == null) {
                if (operation is not { Scope: OpScope.None, }) return new Result<ExpressionToken?, Error?>(null, Error.InvalidSyntax);
            }

            if (current.Left != null && left?.Type is ExpressionTokenType.Assigment) {
                current = current.Left;
                continue;
            } if (current.Right != null && right?.Type is ExpressionTokenType.Assigment) {
                current = current.Right;
                continue;
            }
            
            if (operation?.Evaluation == null) return new Result<ExpressionToken?, Error?>(null, Error.SmthWentWrong);
            
            var evalResult = operation.Evaluation.Invoke(current.Left, current.Right);
            if (!evalResult.Ok) return new Result<ExpressionToken?, Error?>(null, evalResult.Error);
            if (evalResult.Value == null) return new Result<ExpressionToken?, Error?>(null, Error.SmthWentWrong);
            
            current.Left = null;
            current.Right = null;
            current.Data = evalResult.Value.Root.Data;
        }
        return new Result<ExpressionToken?, Error?>(current.Data, null);
    }
}