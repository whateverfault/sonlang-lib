using sonlanglib.interpreter.calculator;
using sonlanglib.interpreter.conversion;
using sonlanglib.interpreter.data.ops;
using sonlanglib.interpreter.error;
using sonlanglib.interpreter.tokenizer;
using sonlanglib.shared;
using sonlanglib.shared.trees;

namespace sonlanglib.interpreter.parser;

public class TokenParser {
    private readonly Interpreter _interpreter;

    private TypeConverter Converter => _interpreter.TypeConverter;
    private Calculator Calculator => _interpreter.Calculator;
    
    
    public TokenParser(Interpreter interpreter) {
        _interpreter = interpreter;
    }
    
    public Result<BinaryTree<ExpressionToken>, Error?> Parse(List<BinaryTreeNode<ExpressionToken>> nodes, int pos = 0) {
        for (var i = pos; i < nodes.Count; ++i) {
            var node = nodes[i];
            var data = node.Data;

            if (data.Type != ExpressionTokenType.LeftParenthesis) continue;
            nodes.RemoveAt(i);
            Parse(nodes, i); break;
        }

        if (nodes.Count <= 0) return new Result<BinaryTree<ExpressionToken>, Error?>(new BinaryTree<ExpressionToken>(ExpressionToken.Empty), null);
        
        var singleNodeLeftResult = IsSingleNodeLeft(nodes);
        if (!singleNodeLeftResult.Ok) return new Result<BinaryTree<ExpressionToken>, Error?>(null, singleNodeLeftResult.Error);
        if (singleNodeLeftResult.Value != null) return singleNodeLeftResult;
        
        var parseIfsResult = ParseIfs(nodes, pos);
        if (!parseIfsResult.Ok) return new Result<BinaryTree<ExpressionToken>, Error?>(null, parseIfsResult.Error);
        if (parseIfsResult.Value == null) return new Result<BinaryTree<ExpressionToken>, Error?>(null, Error.SmthWentWrong);
        
        if (nodes.Count <= 0) return new Result<BinaryTree<ExpressionToken>, Error?>(new BinaryTree<ExpressionToken>(ExpressionToken.Empty), null);
        singleNodeLeftResult = IsSingleNodeLeft(nodes);
        if (!singleNodeLeftResult.Ok) return new Result<BinaryTree<ExpressionToken>, Error?>(null, singleNodeLeftResult.Error);
        if (singleNodeLeftResult.Value != null) return singleNodeLeftResult;
        
        var scopeEndIndex = nodes.Count;
        do {
            var maxPriority = int.MinValue;
            Operation? maxOp = null;
            var maxIndex = 0;

            for (var i = pos; i < scopeEndIndex; ++i) {
                var node = nodes[i];
                var data = node.Data;

                if (data.Type == ExpressionTokenType.RightParenthesis) {
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
                for (var i = 0; i < nodes.Count; i++) {
                    var data = nodes[i].Data;
                    if (!Converter.IsOperation(data)) continue;

                    maxOp = OperationList.GetOperation(data.Value.Val, GetOperationScope(i, nodes));
                    if (maxOp == null) continue;

                    break;
                }

                if (maxOp == null) return new Result<BinaryTree<ExpressionToken>, Error?>(null, Error.IllegalOperation);
            }
            
            if (maxIndex > 0 && maxOp.Scope is OpScope.Left or OpScope.LeftRight) {
                nodes[maxIndex].Left = nodes[maxIndex - 1];
                nodes[maxIndex - 1].Parent = nodes[maxIndex];
                nodes.RemoveAt(--maxIndex); --scopeEndIndex;
            } if (maxIndex < nodes.Count - 1 && maxOp.Scope is OpScope.Right or OpScope.LeftRight) {
                if (nodes[maxIndex + 1].Data.Type == ExpressionTokenType.RightParenthesis) {
                    return new Result<BinaryTree<ExpressionToken>, Error?>(null, Error.InvalidSyntax);
                }
                nodes[maxIndex].Right = nodes[maxIndex + 1];
                nodes[maxIndex + 1].Parent = nodes[maxIndex];
                nodes.RemoveAt(maxIndex + 1); --scopeEndIndex;
            }

            if (!ResolveVariables(nodes[maxIndex], maxOp)) return new Result<BinaryTree<ExpressionToken>, Error?>(null, Error.UnknownIdentifier);
        } while (scopeEndIndex - pos > 1 && nodes.Count > 1);
        
        return new Result<BinaryTree<ExpressionToken>, Error?>(new BinaryTree<ExpressionToken>(nodes[0]), null);
    }

    private Result<List<BinaryTreeNode<ExpressionToken>>, Error?> ParseIfs(List<BinaryTreeNode<ExpressionToken>> nodes, int pos = 0) {
        var wasIf = false;
        
        for (var i = pos; i < nodes.Count;) {
            var data = nodes[i].Data;
            if (!Converter.IsIf(data)) {
                wasIf = false;
                ++i;
                continue;
            } if (data.Type == ExpressionTokenType.If) wasIf = true;
            
            if (Converter.IsIf(data) && wasIf == false) return new Result<List<BinaryTreeNode<ExpressionToken>>, Error?>(null, Error.InvalidSyntax);

            var condition = true;
            if (data.Type != ExpressionTokenType.Else) {
                var left = nodes[i].Left;
                if (left == null) return new Result<List<BinaryTreeNode<ExpressionToken>>, Error?>(null, Error.InvalidSyntax);
                
                var result = Calculator.Calculate(new BinaryTree<ExpressionToken>(left));
                if (!result.Ok) return new Result<List<BinaryTreeNode<ExpressionToken>>, Error?>(null, result.Error);
                if (result.Value == null) return new Result<List<BinaryTreeNode<ExpressionToken>>, Error?>(null, Error.SmthWentWrong);

                var value = result.Value;
                if (!Converter.ToBool(value.Value, out value.Value.Val)) {
                    return new Result<List<BinaryTreeNode<ExpressionToken>>, Error?>(null, Error.IllegalOperation);
                }

                value.Type = ExpressionTokenType.Bool;
                value.Value.Type = ExpressionTokenType.Bool;
                if (!Converter.BoolToLogicalBool(value.Value, out condition)) {
                    return new Result<List<BinaryTreeNode<ExpressionToken>>, Error?>(null, Error.IllegalOperation);
                }
            }

            if (condition) {
                var right = nodes[i].Right;
                if (right == null) return new Result<List<BinaryTreeNode<ExpressionToken>>, Error?>(null, Error.InvalidSyntax);
                
                nodes.RemoveAt(i);
                nodes.Insert(i++, right);

                if (i >= nodes.Count) break;
                data = nodes[i].Data;
                
                while (Converter.IsIf(data)) {
                    nodes.RemoveAt(i);
                    
                    if (i >= nodes.Count) break;
                    data = nodes[i].Data;
                }

                ++i;
                continue;
            }

            nodes.RemoveAt(i);
        }

        return new Result<List<BinaryTreeNode<ExpressionToken>>, Error?>(nodes, null);
    }
    
    private OpScope GetOperationScope(int pos, List<BinaryTreeNode<ExpressionToken>> nodes) {
        var scope = OpScope.None;
        
        if (pos > 0 && (Converter.IsValue(nodes[pos - 1].Data) || IsFilledOp(nodes[pos - 1]))) scope = OpScope.Left;
        if (pos < nodes.Count - 1 && (Converter.IsValue(nodes[pos + 1].Data) || IsFilledOp(nodes[pos + 1]))) {
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

    private Result<BinaryTree<ExpressionToken>, Error?> IsSingleNodeLeft(List<BinaryTreeNode<ExpressionToken>> nodes) {
        if (nodes.Count == 0) return new Result<BinaryTree<ExpressionToken>, Error?>(null, Error.SmthWentWrong);
        if (nodes.Count > 1
         || nodes[0].Left != null 
         || nodes[0].Right != null) return new Result<BinaryTree<ExpressionToken>, Error?>(null, null);

        var node = nodes[0];
        if (node.Data.Type != ExpressionTokenType.Name 
         || Converter.AssignVarToName(node.Data)) return new Result<BinaryTree<ExpressionToken>, Error?>(new BinaryTree<ExpressionToken>(nodes[0]), null);

        return new Result<BinaryTree<ExpressionToken>, Error?>(null, Error.UnknownIdentifier);
    }
    
    private bool IsFilledOp(BinaryTreeNode<ExpressionToken> op) {
        return op.Left != null || op.Right != null;
    }
}