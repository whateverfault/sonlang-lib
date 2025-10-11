namespace sonlanglib.interpreter.conversion;

public class Pair<T> {
    public T? Left { get; set; }
    public T? Right { get; set; }


    public Pair(T? left, T? right) {
        Left = left;
        Right = right;
    }
}