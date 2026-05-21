using System;
using System.Collections.Generic;
using Godot;

namespace MineSweeper;

public enum GameStatus
{
    Playing,
    Won,
    Lost
}

public class GameLogic
{
    public MinefieldModel Model { get; }
    public CellState[,] DisplayState { get; }
    public GameStatus Status { get; private set; } = GameStatus.Playing;
    public int FlagCount { get; private set; }

    public event Action<Vector2I, int>? CellRevealed;
    public event Action<Vector2I>? MineHit;
    public event Action? GameWon;
    public event Action<Vector2I, CellState>? FlagChanged;
    public event Action<List<Vector2I>>? CellsRevealedBatch;

    public GameLogic(GameMode mode)
    {
        Model = new MinefieldModel(mode.Width, mode.Height, mode.MineCount);
        DisplayState = new CellState[mode.Width, mode.Height];
    }

    public void RevealCell(int x, int y)
    {
        if (Status != GameStatus.Playing) return;
        if (!Model.IsInBounds(x, y)) return;
        if (DisplayState[x, y] == CellState.Revealed) return;
        if (DisplayState[x, y] == CellState.Flagged) return;

        if (!Model.MinesPlaced)
            Model.PlaceMines(new Vector2I(x, y));

        if (Model.MineMap[x, y])
        {
            DisplayState[x, y] = CellState.Revealed;
            Status = GameStatus.Lost;
            MineHit?.Invoke(new Vector2I(x, y));
            return;
        }

        int adjacent = Model.AdjacentMineCounts[x, y];
        if (adjacent > 0)
        {
            DisplayState[x, y] = CellState.Revealed;
            CellRevealed?.Invoke(new Vector2I(x, y), adjacent);
        }
        else
        {
            var revealedCells = new List<Vector2I>();
            var queue = new Queue<Vector2I>();
            queue.Enqueue(new Vector2I(x, y));

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                int cx = current.X;
                int cy = current.Y;

                if (!Model.IsInBounds(cx, cy)) continue;
                if (DisplayState[cx, cy] == CellState.Revealed) continue;
                if (DisplayState[cx, cy] == CellState.Flagged) continue;
                if (Model.MineMap[cx, cy]) continue;

                DisplayState[cx, cy] = CellState.Revealed;
                revealedCells.Add(current);
                int adj = Model.AdjacentMineCounts[cx, cy];

                if (adj == 0)
                {
                    foreach (var n in Model.GetNeighbors(cx, cy))
                        if (DisplayState[n.X, n.Y] != CellState.Revealed
                            && DisplayState[n.X, n.Y] != CellState.Flagged)
                            queue.Enqueue(n);
                }
            }

            CellsRevealedBatch?.Invoke(revealedCells);
        }

        CheckWin();
    }

    public void ToggleFlag(int x, int y)
    {
        if (Status != GameStatus.Playing) return;
        if (!Model.IsInBounds(x, y)) return;
        if (DisplayState[x, y] == CellState.Revealed) return;

        var oldState = DisplayState[x, y];
        var newState = oldState switch
        {
            CellState.Hidden => CellState.Flagged,
            CellState.Flagged => CellState.Question,
            CellState.Question => CellState.Hidden,
            _ => CellState.Hidden
        };

        DisplayState[x, y] = newState;

        if (oldState == CellState.Flagged) FlagCount--;
        if (newState == CellState.Flagged) FlagCount++;

        FlagChanged?.Invoke(new Vector2I(x, y), newState);
    }

    public void ChordReveal(int x, int y)
    {
        if (Status != GameStatus.Playing) return;
        if (!Model.IsInBounds(x, y)) return;
        if (DisplayState[x, y] != CellState.Revealed) return;
        if (Model.MineMap[x, y]) return;

        int adjacentMines = Model.AdjacentMineCounts[x, y];
        if (adjacentMines <= 0) return;

        int flagCount = 0;
        foreach (var n in Model.GetNeighbors(x, y))
            if (DisplayState[n.X, n.Y] == CellState.Flagged)
                flagCount++;

        if (flagCount != adjacentMines) return;

        foreach (var n in Model.GetNeighbors(x, y))
        {
            if (DisplayState[n.X, n.Y] == CellState.Flagged) continue;
            if (DisplayState[n.X, n.Y] == CellState.Revealed) continue;
            RevealCell(n.X, n.Y);
            if (Status == GameStatus.Lost) return;
        }
    }

    private void CheckWin()
    {
        if (Status != GameStatus.Playing) return;

        for (int x = 0; x < Model.Width; x++)
        {
            for (int y = 0; y < Model.Height; y++)
            {
                if (!Model.MineMap[x, y] && DisplayState[x, y] != CellState.Revealed)
                    return;
            }
        }

        Status = GameStatus.Won;
        GameWon?.Invoke();
    }
}
