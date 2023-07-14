using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Board {

	private TileValue[] _values;
    private SolveState[] _solveStates;
	public Board(TileValue[] values, SolveState[] solveStates)
    {
        if (values.Length != 9)
            throw new ArgumentOutOfRangeException("Entered board has a size of " + values.Length + " instead of the required length of 9.");
        _values = values;
        _solveStates = solveStates;
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
}
