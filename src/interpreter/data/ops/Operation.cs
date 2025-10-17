using sonlanglib.interpreter.error;
using sonlanglib.interpreter.tokenizer;
using sonlanglib.shared;
using sonlanglib.shared.trees;

namespace sonlanglib.interpreter.data.ops;

public delegate Result<BinaryTree<ExpressionToken>?, Error?> EvaluationHandler(BinaryTreeNode<ExpressionToken>? left, BinaryTreeNode<ExpressionToken>? right);

public class Operation {
    public string Name { get; }
    public OpScope Scope { get; }
    public Priority Priority { get; }
    
    public EvaluationHandler Evaluation { get; }
    

    public Operation(string name, EvaluationHandler evaluation, OpScope scope, Priority priority) {
        Name = name;
        Evaluation = evaluation;
        Scope = scope;
        Priority = priority;
    }
}