using sonlanglib.interpreter.calculator;
using sonlanglib.interpreter.conversion;
using sonlanglib.interpreter.data;
using sonlanglib.interpreter.error;
using sonlanglib.interpreter.lexer;
using sonlanglib.shared;
using sonlanglib.shared.trees;

namespace sonlanglib.interpreter.parser;

public class TokenParser {
    private readonly Interpreter _interpreter;

    private Calculator Calculator => _interpreter.Calculator;
    private TypeConverter Converter => _interpreter.TypeConverter;
    
    
    public TokenParser(Interpreter interpreter) {
        _interpreter = interpreter;
    }
    
    public Result<BinaryTree<ExpressionToken>, Error?> Parse(List<BinaryTreeNode<ExpressionToken>> nodes, int pos = 0) {
        for (var i = pos; i < nodes.Count; ++i) {
            var node = nodes[i];
            var data = node.Data;

            if (data.Type != ExpressionTokenType.OpeningParenthesis) continue;
            nodes.RemoveAt(i);
            Parse(nodes, i); break;
        }

        if (nodes.Count <= 0) return new Result<BinaryTree<ExpressionToken>, Error?>(null, Error.SmthWentWrong);
        if (nodes.Count == 1 && !Converter.IsOperation(nodes[0].Data)) {
            var node = nodes[0];
            if (node.Data.Type == ExpressionTokenType.Name) {
                if (!Converter.AssignVarToName(node.Data)) {
                    return new Result<BinaryTree<ExpressionToken>, Error?>(null, Error.UnknownIdentifier);
                }
            }
            return new Result<BinaryTree<ExpressionToken>, Error?>(new BinaryTree<ExpressionToken>(nodes[0]), null);
        }
        
        var scopeEndIndex = nodes.Count;
        do {
            var maxPriority = int.MinValue;
            Operation? maxOp = null;
            var maxIndex = 0;

            for (var i = pos; i < scopeEndIndex; ++i) {
                var node = nodes[i];
                var data = node.Data;

                if (data.Type == ExpressionTokenType.ClosingParenthesis) {
                    nodes.RemoveAt(i);
                    scopeEndIndex = i; break;
                }
                
                if (!Converter.IsOperation(data)
                 || node.Left != null 
                 || node.Right != null) continue;
                
                var op = OperationList.GetOperation(data.Value.Val, GetOperationScope(i, nodes));
                if (op == null) continue;

                if ((int)op.Priority <= maxPriority) continue;
                maxPriority = (int)op.Priority;
                maxOp = op;
                maxIndex = i;
            }

            if (maxOp == null) {
                return new Result<BinaryTree<ExpressionToken>, Error?>(null, Error.IllegalOperation);
            }
            
            if (maxIndex > 0 && maxOp.Scope is OpScope.Left or OpScope.LeftRight) {
                nodes[maxIndex].Left = nodes[maxIndex - 1];
                nodes[maxIndex - 1].Parent = nodes[maxIndex];
                nodes.RemoveAt(--maxIndex); --scopeEndIndex;
            } if (maxIndex < nodes.Count - 1 && maxOp.Scope is OpScope.Right or OpScope.LeftRight) {
                if (nodes[maxIndex + 1].Data.Type == ExpressionTokenType.ClosingParenthesis) {
                    return new Result<BinaryTree<ExpressionToken>, Error?>(null, Error.InvalidSyntax);
                }
                nodes[maxIndex].Right = nodes[maxIndex + 1];
                nodes[maxIndex + 1].Parent = nodes[maxIndex];
                nodes.RemoveAt(maxIndex + 1); --scopeEndIndex;
            }

            if (!ResolveVariables(nodes[maxIndex], maxOp)) return new Result<BinaryTree<ExpressionToken>, Error?>(null, Error.UnknownIdentifier);
            
            var result = Calculator.Calculate(new BinaryTree<ExpressionToken>(nodes[maxIndex]));
            if (!result.Ok) return new Result<BinaryTree<ExpressionToken>, Error?>(null, result.Error);
            if (result.Value == null) return new Result<BinaryTree<ExpressionToken>, Error?>(null, Error.SmthWentWrong);
            
            nodes[maxIndex].Data.Type = result.Value.Type;
            nodes[maxIndex].Data.Values = result.Value.Values;
        } while (scopeEndIndex - pos > 1 && nodes.Count > 1);
        
        return new Result<BinaryTree<ExpressionToken>, Error?>(new BinaryTree<ExpressionToken>(nodes[0]), null);
    }

    private OpScope GetOperationScope(int pos, List<BinaryTreeNode<ExpressionToken>> nodes) {
        var scope = OpScope.None;
        if (pos > 0 && Converter.IsValue(nodes[pos - 1].Data)) scope = OpScope.Left;
        if (pos < nodes.Count - 1 && Converter.IsValue(nodes[pos + 1].Data)) {
            scope = scope == OpScope.Left ? OpScope.LeftRight : OpScope.Right;
        }

        return scope;
    }
    
    private bool ResolveVariables(BinaryTreeNode<ExpressionToken> node, Operation op) {
        var data = node.Data;
        if (data.Type != ExpressionTokenType.Operation) return true;
        if (Converter.IsReferenceOperation(op)) return true;
        
        var left = node.Left;
        var right = node.Right;
        
        if (left?.Data.Type == ExpressionTokenType.Name) {
            if (!Converter.AssignVarToName(left.Data)) return false;
        }

        return right?.Data.Type != ExpressionTokenType.Name || Converter.AssignVarToName(right.Data);
    }
}