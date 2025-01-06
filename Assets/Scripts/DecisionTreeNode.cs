using System;
using System.Collections.Generic;
public class DecisionTreeNode {

    public TileValue CurrentPlayer { get; private set; } // Player to make the next move. 
	public int BestMove { get; private set; } // Optimal move for the current player.
    public TileValue Winner { get; private set; } // Stores the winner of the board state that this node represents.
    
    private Dictionary<int, DecisionTreeNode> children; // Indexed by the position in which the piece was last played.
    private int depth; // How many nodes down from the root the node is.
    // positive if O wins, negative if X wins, 0 if nobody wins in minmax algorithm.
    // Values farther from zero indicate faster paths to victory.
    // O wants to maximize this, X wants to minimize this.
    private int oScore;

    // Creates a root node with the given starting board and starting player.
    public DecisionTreeNode(Board board, TileValue currentPlayer) : this(board, currentPlayer, 0) { }
    // Creates a decision node with the given board, player, and depth.
	private DecisionTreeNode(Board board, TileValue currentPlayer, int depth)
	{
        this.CurrentPlayer = currentPlayer;
        this.depth = depth;
        Winner = board.GetWinner();
        children = new Dictionary<int, DecisionTreeNode>();

        if (Winner == TileValue.None)
            GetChildren(board);
        GetBestMove();
    }
    // Returns the tree node obtained by the current player making the given move on this state.
	public DecisionTreeNode Move(int m)
	{
        if (isLeaf()) throw new ArgumentException("Cannot move anywhere from a leaf node.");
        if (!children.ContainsKey(m)) throw new ArgumentException("Cannot make the given move.");
		return children[m];
	}
    // Returns who will win if both players play optimally.
    public TileValue GetUltimateWinner() {
        if (BestMove == -1) 
            return Winner;
        // Recurse down the path by following optimal play until a winner is hit.
        else return Move(BestMove).GetUltimateWinner();
    }
    // Returns the nodes immediately below this one.
    public IEnumerable<DecisionTreeNode> GetImmediateChildren()
    {
        foreach (DecisionTreeNode child in children.Values)
            yield return child;
    }
    // Adds child nodes to the dictionary representing which moves are possible from the current state.
    private void GetChildren(Board board) {

        TileValue otherPiece = CurrentPlayer == TileValue.X ? TileValue.O : TileValue.X;
        foreach (int pos in board.GetEmptySlots())
        {
            Board newState = board.Add(pos, CurrentPlayer);
            var child = new DecisionTreeNode(newState, otherPiece, depth + 1);
            children.Add(pos, child);
        }
    }
    // Gets the best move from the current board position, using a minmax algorithm.
    private void GetBestMove()
    {
        // O-Score for leaf nodes is dependent on who wins and how far down the tree the leaf is.
        if (isLeaf())
        {
            BestMove = -1;
            // Subtracting depth makes win-states closer to the root more favorable.
            switch (Winner)
            {
                case TileValue.O:    oScore = 10 - depth; break;
                case TileValue.X:    oScore = depth - 10; break;
                case TileValue.None: oScore = 0; break;
            }
        }
        else
        {
            // O picks a move to maximize O-score, X picks a move to minimize it.
            if (CurrentPlayer == TileValue.O)
                BestMove = children.MaxBy(move => move.Value.oScore).Key;
            else BestMove = children.MinBy(move => move.Value.oScore).Key;
            oScore = Move(BestMove).oScore;
        }
    }
    // Returns whether this node is a leaf node (no children)
	private bool isLeaf() {
		return children.Count == 0;
	}
}