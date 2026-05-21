using Godot;
using System.Collections.Generic;

namespace MineSweeper;

public partial class MinefieldView : Control
{
    private ScrollContainer _scrollContainer = null!;
    private GridContainer _gridContainer = null!;
    private Cell[,] _cells = null!;
    private int _width;
    private int _height;

    public MinefieldView() {
        _scrollContainer = new ScrollContainer();
        _scrollContainer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(_scrollContainer);

        _gridContainer = new GridContainer();
        _gridContainer.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        _scrollContainer.AddChild(_gridContainer);
    }

    public void Build(int width, int height) {
        _width = width;
        _height = height;

        foreach (var child in _gridContainer.GetChildren()) {
            child.QueueFree();
        }

        _cells = new Cell[width, height];
        _gridContainer.Columns = width;

        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                var cell = new Cell();
                cell.GridX = x;
                cell.GridY = y;
                _cells[x, y] = cell;
                _gridContainer.AddChild(cell);
            }
        }
    }

    public Cell? GetCell(int x, int y) {
        if (x >= 0 && x < _width && y >= 0 && y < _height) {
            return _cells[x, y];
        }
        return null;
    }

    public void UpdateCell(int x, int y, CellState state, int adjacentMines) {
        GetCell(x, y)?.SetDisplay(state, adjacentMines);
    }

    public void RevealAllMines(HashSet<Vector2I> minePositions, Vector2I triggeredMine,
        CellState[,] displayState) {
        for (int x = 0; x < _width; x++) {
            for (int y = 0; y < _height; y++) {
                var cell = GetCell(x, y);
                if (cell == null) continue;

                var pos = new Vector2I(x, y);
                bool isMine = minePositions.Contains(pos);
                bool isTriggered = pos == triggeredMine;
                var display = displayState[x, y];

                if (isTriggered) {
                    cell.SetMineTriggered();
                } else if (isMine && display != CellState.Flagged) {
                    cell.SetDisplay(CellState.Revealed, -1);
                } else if (!isMine && display == CellState.Flagged) {
                    cell.SetWrongFlag();
                }
            }
        }
    }
}