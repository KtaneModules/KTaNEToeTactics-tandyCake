using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Board : ICloneable {

    class TreeNode
    {
        public Board board;
        public TreeNode parent;
        public Move howDidWeGetHere;
        public List<TreeNode> children;
        public TileValue victor;
        public TreeNode(Board board, TreeNode parent, Move howDidWeGetHere)
        {
            this.board = board;
            this.parent = parent;
            this.howDidWeGetHere = howDidWeGetHere;
            victor = board.GetVictor();

            if (victor == TileValue.None)
            {
                children = new List<TreeNode>();
                foreach (Move move in board.GetAvailableMoves(NextPiece(howDidWeGetHere.playedPiece)))
                    AddChild(move);
            }
        }
        private void AddChild(Move move)
        {
            TileValue[] newValues = board._values.Clone() as TileValue[];
            newValues[move.playedPosition] = move.playedPiece;
            children.Add(new TreeNode(new Board(newValues, board._solveStates), this, move));
        }
        public List<TreeNode> GetAllEnds()
        {
            List<TreeNode> ends = new List<TreeNode>();
            if (children != null)
                foreach (TreeNode child in children)
                    ends.AddRange(child.GetAllEnds());
            else ends.Add(this);
            return ends;
        }
    }
    class Move
    {
        public TileValue playedPiece;
        public int playedPosition;
        public Move(TileValue playedPiece, int playedPosition)
        {
            this.playedPiece = playedPiece;
            this.playedPosition = playedPosition;
        }
    }

	private TileValue[] _values;
    private SolveState[] _solveStates;
    private TreeNode _root;
	public Board(TileValue[] values, SolveState[] solveStates)
    {
        if (values.Length != 9)
            throw new ArgumentOutOfRangeException("Entered board has a size of " + values.Length + " instead of the required length of 9.");
        _values = values;
        _solveStates = solveStates;
    }
    public static TileValue NextPiece(TileValue piece)
    {
        switch (piece)
        {
            case TileValue.X: return TileValue.O;
            case TileValue.O: return TileValue.X;
            default: return TileValue.None;
        }
    }
    public TileValue this[int pos]
    {
        get { return _values[pos]; }
        set { _values[pos] = value; }
    }
    public TileValue GetVictor()
    {
        List<TileValue> outcomes = new List<TileValue>();
        foreach (SolveState state in _solveStates)
        {
            if (state.tileIndices.All(ix => _values[ix] == TileValue.X))
                outcomes.Add(TileValue.X);
            else if (state.tileIndices.All(ix => _values[ix] == TileValue.O))
                outcomes.Add(TileValue.O);
            else outcomes.Add(TileValue.None);
        }

        bool xWin = outcomes.Contains(TileValue.X);
        bool oWin = outcomes.Contains(TileValue.O);
        if (xWin && !oWin)
            return TileValue.X;
        if (oWin && !xWin)
            return TileValue.O;
        return TileValue.None;
    }
    public bool IsFull()
    {
        return !_values.Any(x => x == TileValue.None);
    }
    public bool IsWin(TileValue player)
    {
        return GetVictor() == player;
    }
    public void GenerateTree(TileValue player)
    {
        _root = new TreeNode(this, null, new Move(NextPiece(player), -1));
    }
    public bool IsWinnableBy(TileValue player)
    {
        // No one-move games allowed.
        if (_root.children.Any(x => x.board.IsWin(player)))
            return false;

        TreeNode currentNode = _root;

        while (GetBestMove(currentNode) != null)
            currentNode = GetBestMove(currentNode);
        
        return currentNode != null && currentNode.victor == player;
    }
    private IEnumerable<Move> GetAvailableMoves(TileValue whoseTurn)
    {
        for (int i = 0; i < 9; i++)
            if (_values[i] == TileValue.None)
                yield return new Move(whoseTurn, i);
    }
    public int GetBestMove()
    {
        if (GetBestMove(_root) == null)
            return -1;
       return GetBestMove(_root).howDidWeGetHere.playedPosition;
    }
    private TreeNode GetBestMove(TreeNode node)
    {
        if (node.children == null || node.children.Count == 0)
            return null;
        TileValue optimizingPlayer = NextPiece(node.howDidWeGetHere.playedPiece);
        //Debug.Log("Optimizing for " + optimizingPlayer);
        foreach (TreeNode child in node.children)
        {
            if (child.victor == optimizingPlayer) {
                //Debug.LogFormat("Placing in {0} to win!!!", child.howDidWeGetHere.playedPosition);
                return child;
            }
        }
        foreach (TreeNode child in node.children)
        {
            if (child.victor == NextPiece(optimizingPlayer))
            {
                //Debug.LogFormat("Placing in {0} to block the opponent.", child.howDidWeGetHere.playedPosition);
                return child;
            }
        }
        Dictionary<TreeNode, int> scores = new Dictionary<TreeNode, int>();
        foreach (TreeNode child in node.children)
        {
            scores.Add(child, 0);
            if (child.children == null)
                continue;
            foreach (TreeNode subChild in child.children)
            {
                TileValue nextVictor = subChild.victor;
                if (nextVictor == optimizingPlayer)
                    scores[child]++;
                else if (nextVictor == NextPiece(optimizingPlayer))
                    scores[child]--;
            }
        }
        return scores.MaxBy(x => x.Value).Key;
    }

    public object Clone()
    {
        return new Board(_values.Clone() as TileValue[], _solveStates);
    }
}
