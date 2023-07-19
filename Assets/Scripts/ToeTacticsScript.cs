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
    //Ordered from the bottom right. (reverse reading order)
    public Tile[] tiles;

    private static string[] tileNames = new[] { "bottom-right", "bottom-middle", "bottom-left", "middle-right", "center", "middle-left", "top-right", "top-middle", "top-left" };

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    private bool moduleInteractable = false;
    private bool cbOn;

    private Board startingBoard;
    private Board board;
    private ShapeColor[] shapeColors;
    private SolveState[,] solveStateTable = new SolveState[9, 6];
    private List<SolveState> usedSolveStates = new List<SolveState>();
    SolveState[] allSolveStates = new int[] { 7, 11, 13, 14, 19, 21, 22, 25, 26, 28, 35, 37, 38, 41, 42, 44, 49, 50, 52, 56, 67, 69, 70, 73, 74, 76, 81, 82, 84, 88, 97, 98, 100, 104, 112, 131, 133, 134, 137, 138, 140, 145, 146, 148, 152, 161, 162, 164, 168, 176, 193, 194, 196, 200, 208, 224 }
                   .Select(ix => DecompressState(ix)).ToArray();
    private TileValue playerPiece;
    private bool canStrike = true;

    private static SolveState DecompressState(int ix) {
        List<int> indices = new List<int>(3);
        for (int i = 0; i < 9; i++)
            if ((ix & (1 << i)) != 0)
                indices.Add(i);
        return new SolveState(indices.ToArray());
    }
    void Awake () {
        moduleId = moduleIdCounter++;
        GenRuleseed();
        foreach (Tile tile in tiles)
            tile.selectable.OnInteract += () => { TilePress(tile); return false; };
        Module.OnActivate += () => Activate();
    }
    void Start()
    {
        if (Colorblind.ColorblindModeActive)
            ToggleCB();
        playerPiece = Bomb.GetSerialNumberNumbers().Last() % 2 == 0 ? TileValue.O : TileValue.X;
        Log("You are playing as {0}.", playerPiece);
    }
    void Activate ()
    {
        GeneratePuzzle();
        moduleInteractable = true;
    }
    void ToggleCB()
    {
        cbOn = !cbOn;
        for (int i = 0; i < 9; i++)
            tiles[i].SetColorblind(cbOn);
    }
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
            if (!moduleSolved && !board.IsFull())
                StartCoroutine(PlaceOpponentPiece());
        }
    }
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
        do
        {
            int[] prefilled = Enumerable.Range(0, 9).ToArray().Shuffle().Take(4).ToArray();
            TileValue[] pieces = new[] { TileValue.X, TileValue.X, TileValue.O, TileValue.O };
            TileValue[] boardState = Enumerable.Repeat(TileValue.None, 9).ToArray();
            usedSolveStates.Clear();
            for (int i = 0; i < 9; i++)
                tiles[i].SetTile(TileValue.None, ShapeColor.Gray);
            for (int i = 0; i < 4; i++)
            {
                int pos = prefilled[i];
                tiles[pos].SetTile(pieces[i], (ShapeColor)Rnd.Range(1, 4));
                boardState[pos] = pieces[i];
                usedSolveStates.Add(IndexTable(tiles[pos]));
                board = new Board(boardState, usedSolveStates.ToArray());
            }
            board.GenerateTree(playerPiece);
        } while (!board.IsWinnableBy(playerPiece));

        startingBoard = board.Clone() as Board;
        shapeColors = new ShapeColor[9];
        for (int i = 0; i < 9; i++)
            shapeColors[i] = tiles[i].Color;
        LogStart();
    }
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
        board[position] = piece;
        CheckCurrentBoard();
    }
    void CheckCurrentBoard()
    {
        TileValue victor = board.GetVictor();
        moduleInteractable = false;
        if (victor == playerPiece)
            Solve();
        else if (victor == Board.NextPiece(playerPiece))
            StrikeWithOppWin();
        else if (board.IsFull())
            StrikeWithTie();
        else moduleInteractable = true;
    }
    IEnumerator PlaceOpponentPiece()
    {
        moduleInteractable = false;
        yield return new WaitForSeconds(1.25f);
        TileValue opponentPiece = Board.NextPiece(playerPiece);
        board.GenerateTree(opponentPiece);
        int position = board.GetBestMove();
        if (position == -1)
        {
            yield break;
        }
        PlaceTile(position, opponentPiece);
        Log("Your opponent placed an {0} in the {1} position.", opponentPiece, tileNames[position]);
        CheckCurrentBoard();
        SuggestBestMove();
        moduleInteractable = true;
    }
    void SuggestBestMove()
    {
        board.GenerateTree(playerPiece);
        int bestMove = board.GetBestMove();
        if (bestMove == -1)
            return;
        Log("You should place an {0} in the {1} position.", playerPiece, tileNames[bestMove]);
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
        if (canStrike)
            Module.HandleStrike();
        for (int i = 0; i < 9; i++)
            tiles[i].SetTile(startingBoard[i], shapeColors[i]);
        board = startingBoard.Clone() as Board;


        board.GenerateTree(playerPiece);
        moduleInteractable = true;
        yield return null;
        SuggestBestMove();
        canStrike = false;
        yield return new WaitForSeconds(.5f);
        canStrike = true;
    }
    SolveState IndexTable(Tile tile)
    {
        return solveStateTable[tile.position,
                    3 * ((int)tile.Shape - 1) + ((int)tile.Color - 1)];
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
        if (Regex.IsMatch(command, "[TMB][LMR]|[ABC][123]"))
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
        while (!moduleSolved)
        {
            while (!moduleInteractable)
                yield return true;
            board.GenerateTree(playerPiece);
            int btnIx = board.GetBestMove();
            tiles[btnIx].selectable.OnInteract();
            yield return null;
        }
    }
}
