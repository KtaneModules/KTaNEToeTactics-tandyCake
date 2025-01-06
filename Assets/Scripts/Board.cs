using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
public class Board {

    private TileValue[] values; // Stores what pieces are held in each position.
    private SolveState[] solveStates; // Stores the states which cause a player to win.

    // Creates a board with the given pieces and solve states.
    public Board(TileValue[] values, SolveState[] solveStates)
    {
        if (values.Length != 9)
            throw new ArgumentException("Entered board has a size of " + values.Length + " instead of the required length of 9.");
        this.values = values;
        this.solveStates = solveStates;
    }
    // Returns the winner in the current board state based on the solve states, or TileValue.None if there is no winner. (Unfinished or Tie)
    public TileValue GetWinner() {
        foreach (SolveState solve in solveStates) {
            if (solve.GetWinner(values) != TileValue.None)
                return solve.GetWinner(values);
        }
        return TileValue.None;
    }
    // Returns a new board with the given piece added to the given position.
    public Board Add(int ix, TileValue piece) {
        if (values[ix] != TileValue.None) throw new ArgumentException("Cannot place piece in occupied spot.");
        Board copy = Copy();
        copy.values[ix] = piece;
        return copy;
    }
    // Returns the positions of the board which contain no pieces.
    public IEnumerable<int> GetEmptySlots()
    {
        for (int i = 0; i < 9; i++)
            if (values[i] == TileValue.None)
                yield return i;
    }
    // Creates a copy of the board with a deep-cloned TileValue array.
    private Board Copy()
    {
        return new Board(values.Clone() as TileValue[], solveStates);
    }
}
