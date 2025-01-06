using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class ToeTacticsScript : MonoBehaviour {

    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMBombModule Module;
    public KMColorblindMode Colorblind;
    public KMRuleSeedable Ruleseed;
    public Tile[] tiles; //Ordered from the bottom right. (reverse reading order)
    private static string[] tileNames = new[] { "bottom-right", "bottom-middle", "bottom-left", "middle-right", "center", "middle-left", "top-right", "top-middle", "top-left" };

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    private bool moduleInteractable = false; 
    private bool cbOn; // Stores if colorblind mode is enabled.

    private TileValue[] boardSpaces; // Stores initial piece placements on the board.
    private ShapeColor[] shapeColors; // Stores colors of the initial placements on the board.
    private SolveState[,] solveStateTable = new SolveState[9, 6]; // Table stored in manual (varies by ruleseed)
    private SolveState[] usedSolveStates;
    // These numbers are the nummbers from 0 to 2^9 which contain exactly three '1's in their binary representation.
    SolveState[] allSolveStates = new int[] { 7, 11, 13, 14, 19, 21, 22, 25, 26, 28, 35, 37, 38, 41, 42, 44, 49, 50, 52, 56, 67, 69, 70, 73, 74, 76, 81, 82, 84, 88, 97, 98, 100, 104, 112, 131, 133, 134, 137, 138, 140, 145, 146, 148, 152, 161, 162, 164, 168, 176, 193, 194, 196, 200, 208, 224 }
                   .Select(id => new SolveState(id)).ToArray();
    
    private TileValue playerPiece; // Stores what piece the defuser is playing as.
    private TileValue oppPiece; // Stores what piece the bomb is playing as.

    private Board startBoard; // Stores the initial positions of pieces on the board.
    private DecisionTreeNode startState; // Stores the root of the decision tree representing the board state, rooted at the initial piece positions.
    private DecisionTreeNode currentState; // Stores the node of startState represented by the current state of the module.

    private bool unwinnable; // Stores whether the current board state is in an unwinnable position.

    void Awake () { // Before bomb initialization.
        moduleId = moduleIdCounter++;
        GenRuleseed();
        foreach (Tile tile in tiles)
            tile.selectable.OnInteract += () => { TilePress(tile); return false; };
        Module.OnActivate += () => Activate();
    }
    void Start() { // Immediately after bomb initialization
        if (Colorblind.ColorblindModeActive)
            ToggleCB();
        if (Bomb.GetSerialNumberNumbers().Last() % 2 == 0)
        {
            playerPiece = TileValue.O;
            oppPiece = TileValue.X;
        }
        else
        {
            playerPiece = TileValue.X;
            oppPiece = TileValue.O;
        }
        Log("You are playing as {0}.", playerPiece);
    }
    void Activate () { // When lights turn on.
        GeneratePuzzle();
        moduleInteractable = true;
    }
    void ToggleCB() { // Toggles colorblind support.
        cbOn = !cbOn;
        for (int i = 0; i < 9; i++)
            tiles[i].SetColorblind(cbOn);
    }
    // Handles tile interaction.
    void TilePress(Tile tile)
    {
        if (!tile.IsInteractable)
            return;
        tile.selectable.AddInteractionPunch(.25f);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, tile.transform);
        if (moduleInteractable && !moduleSolved)
        {
            Log("You placed an {0} in the {1} position.", playerPiece, tileNames[tile.position]);
            PlaceTile(tile.position, playerPiece);
            if (!moduleSolved && !FullBoard())
                StartCoroutine(PlaceOpponentPiece());
        }
    }
    // Shuffles the potential solve states and takes the first 6*9=54 of them.
    void GenRuleseed()
    {
        var rng = Ruleseed.GetRNG();
        rng.ShuffleFisherYates(allSolveStates);
        for (int rowFromBottom = 0; rowFromBottom < 9; rowFromBottom++)
            for (int col = 0; col < 6; col++)
                solveStateTable[8 - rowFromBottom, col] = allSolveStates[6 * rowFromBottom + col];
        Log("Generated table with rule-seed {0}.", rng.Seed);
    }
    void GeneratePuzzle()
    {
        TileValue[] pieceOrder = new[] { oppPiece, playerPiece, oppPiece, playerPiece };
        int[] positions;
        ShapeColor[] colors;
        SolveState[] states;
        bool unwinnableByPlayer;
        bool unwinnableByOpponent;
        bool canSolveInOneMove;
        do
        {
            positions = new int[4];
            colors = new ShapeColor[4];
            states = new SolveState[4];
            boardSpaces = Enumerable.Repeat(TileValue.None, 9).ToArray();
            shapeColors = Enumerable.Repeat(ShapeColor.Gray, 9).ToArray();

            int[] positionOrder = Enumerable.Range(0, 9).ToArray().Shuffle();
            for (int i = 0; i < 4; i++)
            {
                positions[i] = positionOrder[i];
                colors[i] = (ShapeColor)Rnd.Range(1, 4);
                states[i] = IndexTable(positions[i], pieceOrder[i], colors[i]);

                boardSpaces[positions[i]] = pieceOrder[i];
                shapeColors[positions[i]] = colors[i];
            }

            startBoard = new Board(boardSpaces, states);
            startState = new DecisionTreeNode(startBoard, playerPiece);

            unwinnableByPlayer = startState.GetUltimateWinner() != playerPiece;
            unwinnableByOpponent = !startState.GetImmediateChildren().Any(x => x.GetUltimateWinner() == oppPiece);
            canSolveInOneMove = startState.Move(startState.BestMove).Winner == playerPiece;
        } while (unwinnableByPlayer || unwinnableByOpponent || canSolveInOneMove);

        currentState = startState;
        for (int i = 0; i < positions.Length; i++)
            tiles[positions[i]].SetTile(pieceOrder[i], colors[i]);
        LogStart();

    }
    // Outputs starting board state to logging in a way that is palatable to custom LFA support.
    void LogStart()
    {
        string colors = "";
        string shapes = "";
        for (int i = 8; i >= 0; i--)
        {
            colors += tiles[i].Color == ShapeColor.Gray ? '.' : tiles[i].Color.ToString()[0];
            shapes += tiles[i].Shape == TileValue.None ? '.' : tiles[i].Shape.ToString()[0];
        }
        Log("colors: {0}", colors);
        Log("shapes: {0}", shapes);
        SuggestBestMove();
    }
    void PlaceTile(int position, TileValue piece)
    {
        Audio.PlaySoundAtTransform("place piece", tiles[position].transform);
        tiles[position].SetTile(piece, ShapeColor.Gray);
        currentState = currentState.Move(position);
        CheckCurrentBoard();
    }
    void CheckCurrentBoard()
    {
        moduleInteractable = false;
        if (currentState.Winner == playerPiece)
            Solve();
        else if (currentState.Winner == oppPiece)
            StrikeWithOppWin();
        else if (FullBoard())
            StrikeWithTie();
        else moduleInteractable = true;
    }
    bool FullBoard()
    {
        for (int i = 0; i < 9; i++)
            if (tiles[i].Shape == TileValue.None)
                return false;
        return true;
    }
    IEnumerator PlaceOpponentPiece()
    {
        moduleInteractable = false;
        yield return new WaitForSeconds(1.25f);
        int pos = currentState.BestMove;
        if (pos == -1)
            yield break;
        moduleInteractable = true;
        PlaceTile(pos, oppPiece);
        Log("Your opponent placed an {0} in the {1} position.", oppPiece, tileNames[pos]);
        SuggestBestMove();
    }
    void SuggestBestMove()
    {
        if (currentState.BestMove == -1)
            return;
        if (!unwinnable)
        {
            if (currentState.GetUltimateWinner() != playerPiece)
            {
                unwinnable = true;
                Log("Uh oh. The board has been put into an unwinnable position.");
            }            
            else Log("You should place an {0} in the {1} position.", playerPiece, tileNames[currentState.BestMove]);
        }
    }
    void Solve()
    {
        moduleSolved = true;
        Log("You put three {0}s in a solving pattern. You win!", playerPiece);
        Module.HandlePass();
        Audio.PlaySoundAtTransform("solve", transform);
    }
    void StrikeWithOppWin()
    {
        Log("Your opponent put three {0}s in a solving pattern. Strike!", playerPiece == TileValue.X ? 'O' : 'X');
        StartCoroutine(StrikeAndReset());
    }
    void StrikeWithTie()
    {
        Log("You've filled the board with no winner. Strike!");
        StartCoroutine(StrikeAndReset());
    }
    IEnumerator StrikeAndReset()
    {
        moduleInteractable = false;
        yield return new WaitForSeconds(1.5f);
        Module.HandleStrike();
        for (int i = 0; i < 9; i++)
            tiles[i].SetTile(boardSpaces[i], shapeColors[i]);
        currentState = startState;
        unwinnable = false;
        moduleInteractable = true;
        yield return null;
        SuggestBestMove();
    }
    SolveState IndexTable(int position, TileValue shape, ShapeColor color)
    {
        int column = 3 * ((int)shape - 1) + ((int)color - 1);
        return solveStateTable[position, column];
    }
    void Log(string str, params object[] args)
    {
        Debug.LogFormat("[Toe Tactics #{0}] {1}", moduleId, string.Format(str, args));
    }

    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use <!{0} ML> or <!{0} A2> to place your piece in the middle-left position. Use <!{0} colorblind> to toggle colorblind mode.";
    #pragma warning restore 414

    IEnumerator ProcessTwitchCommand (string command)
    {
        string[] buttonNames = new[] { "BR", "BM", "BL", "MR", "MM", "ML", "TR", "TM", "TL", "C3", "B3", "A3", "C2", "B2", "A2", "C1", "B1", "A1" };
        command = command.Trim().ToUpperInvariant();
        if (Regex.IsMatch(command, "^([TMB][LMR]|[ABC][123])$"))
        {
            yield return null;
            yield return new WaitUntil(() => moduleInteractable);
            int btnIx = Array.IndexOf(buttonNames, command) % 9;
            tiles[btnIx].selectable.OnInteract();
        }
        else if (command.EqualsAny(new[] { "COLORBLIND", "COLOURBLIND", "COLOR-BLIND", "COLOUR-BLIND", "CB" }))
        {
            yield return null;
            ToggleCB();
        }
    }

    IEnumerator TwitchHandleForcedSolve ()
    {
        if (unwinnable)
            Module.HandlePass();
        else
        {
            while (!moduleSolved)
            {
                while (!moduleInteractable)
                    yield return true;
                yield return new WaitForSeconds(0.1f);
                tiles[currentState.BestMove].selectable.OnInteract();
                yield return null;
            }
        }
    }
}
