using Godot;
using System.Collections.Generic;

namespace MineSweeper;

public partial class MinefieldView : Control
{
    private ScrollContainer _scrollContainer = null!;
    private CenterContainer _centerContainer = null!;
    private GridContainer _gridContainer = null!;
    private ColorRect _mask;
    private Cell[,] _cells = null!;
    private int _width;
    private int _height;

    public MinefieldView() {
        _scrollContainer = new ScrollContainer();
        _scrollContainer.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_scrollContainer);

        _centerContainer = new CenterContainer();
        _scrollContainer.AddChild(_centerContainer);

        _gridContainer = new GridContainer();
        _gridContainer.AddThemeConstantOverride("h_separation", 0);
        _gridContainer.AddThemeConstantOverride("v_separation", 0);
        _centerContainer.AddChild(_gridContainer);
        _mask = new ColorRect();
        _mask.Color = new Color(0, 0, 0, 0);
        _mask.LayoutMode = 1;
        _mask.SetAnchorsPreset(LayoutPreset.FullRect);
        _mask.Visible = false;
        _mask.MouseFilter = MouseFilterEnum.Stop;
        _centerContainer.AddChild(_mask);
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

        // Let layout update, then ensure the CenterContainer fills the scroll area when grid is small
        CallDeferred(nameof(AdjustCenterContainer));
    }

    private void AdjustCenterContainer() {
        var scrollSize = _scrollContainer.Size;
        var gridSize = new Vector2(_width * Cell.PixelSize, _height * Cell.PixelSize);
        _centerContainer.CustomMinimumSize = new Vector2(
            Mathf.Max(gridSize.X, scrollSize.X),
            Mathf.Max(gridSize.Y, scrollSize.Y)
        );
        _mask.CustomMinimumSize = _centerContainer.CustomMinimumSize;
    }

    public override void _Notification(int what) {
        if (what == NotificationResized) {
            AdjustCenterContainer();
        }
    }
    
    public void SetPause(bool paused) => _mask.Visible = paused; 

    public Cell? GetCell(int x, int y) {
        if (x >= 0 && x < _width && y >= 0 && y < _height) {
            return _cells[x, y];
        }
        return null;
    }

    public void UpdateCell(int x, int y, CellState display, int adjacentMines) {
        GetCell(x, y)?.SetDisplay(display, adjacentMines);
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
