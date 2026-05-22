using Godot;
using System;
using System.Collections.Generic;

namespace MineSweeper;

public partial class GameScreen : Control
{
    public event Action<bool, double, GameMode>? GameEnded;
    public event Action? BackRequested;

    private Label _mineCounter = null!;
    private Label _timerLabel = null!;
    private Button _newGameButton = null!;

    private GameLogic? _logic;
    private GameTimer _timer = new();
    private GameMode _currentMode;
    private MinefieldView? _minefieldView;

    // Layout gaps — adjustable before first NewGame() call
    public int LabelToGridGap = 75;
    public int ButtonToGridGap = 15;

    public bool FlagsEverUsed { get; private set; }
    public int MaxConsecutiveReveals { get; private set; }
    public int MaxChordRevealCount { get; private set; }
    public int FirstClickRevealCount { get; private set; }
    public GameLogic? CurrentLogic => _logic;

    private int CurrentConsecutiveReveals { get; set; }
    private int _pendingRevealCount;
    private bool _justFirstClicked;

    public override void _Ready() {
        _mineCounter = GetNode<Label>("Label_MineCount");
        _timerLabel = GetNode<Label>("Label_Time");
        _newGameButton = GetNode<Button>("Button_NewGame");

        // Pin mine counter to left edge (aligns with grid left)
        _mineCounter.LayoutMode = 1;
        _mineCounter.AnchorLeft = 0;
        _mineCounter.AnchorRight = 0;
        _mineCounter.OffsetLeft = 50;
        _mineCounter.OffsetRight = 225;

        // Pin timer to right edge (aligns with grid right)
        _timerLabel.LayoutMode = 1;
        _timerLabel.AnchorLeft = 1;
        _timerLabel.AnchorRight = 1;
        _timerLabel.OffsetLeft = -200;
        _timerLabel.OffsetRight = -50;

        // Center button horizontally
        // _newGameButton.LayoutMode = 1;
        // _newGameButton.AnchorLeft = 0.5f;
        // _newGameButton.AnchorRight = 0.5f;
        // _newGameButton.OffsetLeft = -70;
        // _newGameButton.OffsetRight = 70;

        GetNode<Button>("Button_Home").Pressed += () => {
            if (_logic is { Status: GameStatus.Playing })
                _timer.Stop();
            BackRequested?.Invoke();
        };

        _newGameButton.Pressed += RestartGame;
    }

    public override void _UnhandledInput(InputEvent @event) {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed) {
            if (keyEvent.Keycode == Key.F2)
                RestartGame();
            else if (keyEvent.Keycode == Key.Escape)
                GetNode<Button>("Button_Home").EmitSignal("pressed");
        }
    }

    public void NewGame(GameMode mode) {
        _currentMode = mode;
        _timer.Reset();
        UpdateTimerLabel();
        UpdateMineCounter(mode.MineCount);

        FlagsEverUsed = false;
        CurrentConsecutiveReveals = 0;
        MaxConsecutiveReveals = 0;
        MaxChordRevealCount = 0;
        FirstClickRevealCount = 0;
        _pendingRevealCount = 0;
        _justFirstClicked = false;

        if (_logic != null) {
            _logic.CellRevealed -= OnCellRevealed;
            _logic.CellsRevealedBatch -= OnCellsRevealedBatch;
            _logic.MineHit -= OnMineHit;
            _logic.GameWon -= OnGameWon;
            _logic.FlagChanged -= OnFlagChanged;
        }

        _logic = new GameLogic(mode);
        _logic.CellRevealed += OnCellRevealed;
        _logic.CellsRevealedBatch += OnCellsRevealedBatch;
        _logic.MineHit += OnMineHit;
        _logic.GameWon += OnGameWon;
        _logic.FlagChanged += OnFlagChanged;

        if (_minefieldView != null) {
            RemoveChild(_minefieldView);
            _minefieldView.QueueFree();
        }

        _minefieldView = new MinefieldView();
        _minefieldView.LayoutMode = 1;
        _minefieldView.AnchorLeft = 0;
        _minefieldView.AnchorTop = 0;
        _minefieldView.AnchorRight = 1;
        _minefieldView.AnchorBottom = 1;
        _minefieldView.OffsetTop = 150;
        AddChild(_minefieldView);
        _minefieldView.Build(mode.Width, mode.Height);

        CallDeferred(nameof(PositionControls));

        for (int x = 0; x < mode.Width; x++) {
            for (int y = 0; y < mode.Height; y++) {
                var cell = _minefieldView.GetCell(x, y);
                if (cell == null) continue;
                int cx = x, cy = y;
                cell.LeftClicked += () => OnCellLeftClicked(cx, cy);
                cell.RightClicked += () => OnCellRightClicked(cx, cy);
                cell.ChordClicked += () => OnCellChordClicked(cx, cy);
            }
        }
    }

    public override void _Process(double delta) {
        _timer.Update(delta);
        if (_timer.IsRunning)
            UpdateTimerLabel();
    }

    public override void _Notification(int what) {
        if (what == NotificationResized)
            CallDeferred(nameof(PositionControls));
    }

    private void PositionControls() {
        if (_minefieldView == null) return;

        var viewSize = _minefieldView.Size;
        var gridPixelW = _currentMode.Width * Cell.PixelSize;
        var gridPixelH = _currentMode.Height * Cell.PixelSize;

        var gridLeft = (viewSize.X - gridPixelW) / 2f;
        if (gridLeft < 0) gridLeft = 0;
        var gridTop = (viewSize.Y - gridPixelH) / 2f;
        if (gridTop < 0) gridTop = 0;

        var labelY = _minefieldView.OffsetTop + gridTop - LabelToGridGap - 50;

        var mineCounterX = _minefieldView.OffsetLeft + gridLeft;
        var mineCounterY = Mathf.Max(70, labelY);
        _mineCounter.Position = new Vector2(mineCounterX, mineCounterY);

        var gridRight = _minefieldView.OffsetLeft + gridLeft
            + Mathf.Min(gridPixelW, viewSize.X);
        var timeLabelX = gridRight - _timerLabel.Size.X;
        _timerLabel.Position = new Vector2(timeLabelX, mineCounterY);

        var btnY = Mathf.Max(_minefieldView.OffsetTop + gridTop - ButtonToGridGap - 45, 70);
        var buttonSizeY = _newGameButton.Size.Y;
        _newGameButton.OffsetTop = btnY;
        _newGameButton.OffsetBottom = btnY + buttonSizeY;
    }

    private void UpdateTimerLabel() {
        _timerLabel.Text = _timer.Formatted;
    }

    private void UpdateMineCounter(int count) {
        _mineCounter.Text = $"剩余：{count}";
    }

    private void RestartGame() {
        NewGame(_currentMode);
    }

    private void OnCellLeftClicked(int x, int y) {
        if (_logic == null) return;
        _justFirstClicked = !_logic.Model.MinesPlaced;
        _pendingRevealCount = 0;
        _logic.RevealCell(x, y);
        if (_justFirstClicked && _logic.Status == GameStatus.Playing)
            _timer.Start();
        _justFirstClicked = false;
    }

    private void OnCellRightClicked(int x, int y) {
        FlagsEverUsed = true;
        CurrentConsecutiveReveals = 0;
        _logic?.ToggleFlag(x, y);
    }

    private void OnCellChordClicked(int x, int y) {
        if (_logic == null) return;
        _pendingRevealCount = 0;
        _logic.ChordReveal(x, y);
        if (_pendingRevealCount > MaxChordRevealCount)
            MaxChordRevealCount = _pendingRevealCount;
    }

    private void OnCellRevealed(Vector2I pos, int adjacentMines) {
        _minefieldView?.UpdateCell(pos.X, pos.Y, CellState.Revealed, adjacentMines);
        _pendingRevealCount++;
        CurrentConsecutiveReveals++;
        if (CurrentConsecutiveReveals > MaxConsecutiveReveals)
            MaxConsecutiveReveals = CurrentConsecutiveReveals;
    }

    private void OnCellsRevealedBatch(List<Vector2I> cells) {
        if (_logic == null || _minefieldView == null) return;
        foreach (var pos in cells) {
            int adj = _logic.Model.AdjacentMineCounts[pos.X, pos.Y];
            _minefieldView.UpdateCell(pos.X, pos.Y, CellState.Revealed, adj);
        }
        _pendingRevealCount += cells.Count;
        CurrentConsecutiveReveals += cells.Count;
        if (CurrentConsecutiveReveals > MaxConsecutiveReveals)
            MaxConsecutiveReveals = CurrentConsecutiveReveals;
        if (_justFirstClicked) {
            FirstClickRevealCount = cells.Count;
            _justFirstClicked = false;
        }
    }

    private void OnMineHit(Vector2I pos) {
        if (_logic == null || _minefieldView == null) return;
        _timer.Stop();
        _minefieldView.RevealAllMines(_logic.Model.MinePositions, pos, _logic.DisplayState);
        DisableAllCells();
        GameEnded?.Invoke(false, _timer.Elapsed, _currentMode);
    }

    private void OnGameWon() {
        if (_logic == null || _minefieldView == null) return;
        _timer.Stop();
        for (int x = 0; x < _logic.Model.Width; x++) {
            for (int y = 0; y < _logic.Model.Height; y++) {
                if (_logic.Model.MineMap[x, y]
                    && _logic.DisplayState[x, y] != CellState.Flagged) {
                    _logic.DisplayState[x, y] = CellState.Flagged;
                    _minefieldView.UpdateCell(x, y, CellState.Flagged, -1);
                }
            }
        }
        DisableAllCells();
        GameEnded?.Invoke(true, _timer.Elapsed, _currentMode);
    }

    private void OnFlagChanged(Vector2I pos, CellState newState) {
        _minefieldView?.UpdateCell(pos.X, pos.Y, newState, -1);
        if (_logic != null)
            UpdateMineCounter(_currentMode.MineCount - _logic.FlagCount);
    }

    private void DisableAllCells() {
        if (_minefieldView == null) return;
        for (int x = 0; x < _currentMode.Width; x++)
            for (int y = 0; y < _currentMode.Height; y++)
                if (_minefieldView.GetCell(x, y) is { } cell)
                    cell.MouseFilter = MouseFilterEnum.Ignore;
    }
}
