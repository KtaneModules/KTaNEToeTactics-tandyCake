using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Board {

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
        
        do currentNode = GetBestMove(currentNode);
        while (currentNode.victor == TileValue.None); 
        
        return currentNode.victor == player;
    }
    private IEnumerable<Move> GetAvailableMoves(TileValue whoseTurn)
    {
        for (int i = 0; i < 9; i++)
            if (_values[i] == TileValue.None)
                yield return new Move(whoseTurn, i);
    }
    public int GetBestMove()
    {
       return GetBestMove(_root).howDidWeGetHere.playedPosition;
    }
    private TreeNode GetBestMove(TreeNode node)
    {
        TileValue optimizingPlayer = NextPiece(node.howDidWeGetHere.playedPiece);
        var kavin = node.children.Select(c => c.GetAllEnds()).ToArray();
        Debug.Log(":3");
        return node.children.OrderByDescending(child =>
                   child.GetAllEnds().Count(e => e.victor == optimizingPlayer)).ThenByDescending(child =>
                   child.GetAllEnds().Count())
                   .First();
    }
}
