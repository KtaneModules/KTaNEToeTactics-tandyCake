using System;
using System.Collections.Generic;
using System.Linq;

public struct SolveState {
	public List<int> tileIndices;
    // Converts the given ID into its binary form and generates a solve state consisting of all positions where the binary number has '1's.
	public SolveState(int id)
    {
        List<int> indices = new List<int>(3);
        for (int i = 0; i < 9; i++) {
            if ((id & (1 << i)) != 0)
                indices.Add(i);
        }
        if (indices.Count != 3) throw new ArgumentException("Solve state must have exactly three positions.");
        else tileIndices = indices;
    }
    public TileValue GetWinner(TileValue[] tiles) {
        var pieces = tileIndices.Select(i => tiles[i]);
        if (pieces.All(x => x == pieces.First()))
            return pieces.First();
        else return TileValue.None;
    }
}
