using Godot;
using System;
using System.Collections.Generic;

namespace MineSweeper;

public partial class GameScreen : Control
{
    public event Action<bool, double, GameMode>? GameEnded;
    public event Action? BackRequested;

    private VBoxContainer _vboxContainer = null!;
    private HBoxContainer _topBar = null!;
    private Label _mineCounter = null!;
    private Button _smileyButton = null!;
    private Label _timerLabel = null!;

    private GameLogic? _logic;
    private GameTimer _timer = new();
    private GameMode _currentMode;
    private MinefieldView? _minefieldView;

    // Stats tracking for achievements
    public bool FlagsEverUsed { get; private set; }
    private int CurrentConsecutiveReveals { get; set; }
    public int MaxConsecutiveReveals { get; private set; }
    public int MaxChordRevealCount { get; private set; }
    public int FirstClickRevealCount { get; private set; }
    public GameLogic? CurrentLogic => _logic;

    public override void _Ready()
    {
        SetAnchorsPreset(Control.LayoutPreset.FullRect);

        _vboxContainer = new VBoxContainer();
        _vboxContainer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _vboxContainer.AddThemeConstantOverride("separation", 0);
        AddChild(_vboxContainer);

        _topBar = new HBoxContainer();
        _topBar.CustomMinimumSize = new Vector2(0, 44);
        _vboxContainer.AddChild(_topBar);

        _mineCounter = new Label
        {
            Text = "000",
            CustomMinimumSize = new Vector2(70, 0),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        _topBar.AddChild(_mineCounter);

        _smileyButton = new Button
        {
            Text = ":)",
            CustomMinimumSize = new Vector2(40, 40),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
        };
        _topBar.AddChild(_smileyButton);

        _timerLabel = new Label
        {
            Text = "00:00.00",
            CustomMinimumSize = new Vector2(80, 0),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        _topBar.AddChild(_timerLabel);

        _smileyButton.Pressed += OnSmileyPressed;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            if (keyEvent.Keycode == Key.F2)
                OnSmileyPressed();
            else if (keyEvent.Keycode == Key.Escape)
                ReturnToMenu();
        }
    }

    private void ReturnToMenu()
    {
        if (_logic is { Status: GameStatus.Playing })
            _timer.Stop();
        BackRequested?.Invoke();
    }

    public void NewGame(GameMode mode)
    {
        _currentMode = mode;
        _timer.Reset();
        UpdateTimerLabel();
        UpdateMineCounter(mode.MineCount);
        _smileyButton.Text = ":)";

        // Reset stats
        FlagsEverUsed = false;
        CurrentConsecutiveReveals = 0;
        MaxConsecutiveReveals = 0;
        MaxChordRevealCount = 0;
        FirstClickRevealCount = 0;

        if (_logic != null)
        {
            _logic.CellRevealed -= OnCellRevealed;
            _logic.CellsRevealedBatch -= OnCellsRevealedBatch;
            _logic.MineHit -= OnMineHit;
            _logic.GameWon -= OnGameWon;
            _logic.FlagChanged -= OnFlagChanged;
        }

        _pendingRevealCount = 0;
        _justFirstClicked = false;

        _logic = new GameLogic(mode);
        _logic.CellRevealed += OnCellRevealed;
        _logic.CellsRevealedBatch += OnCellsRevealedBatch;
        _logic.MineHit += OnMineHit;
        _logic.GameWon += OnGameWon;
        _logic.FlagChanged += OnFlagChanged;

        if (_minefieldView != null)
        {
            _vboxContainer.RemoveChild(_minefieldView);
            _minefieldView.QueueFree();
        }

        _minefieldView = new MinefieldView();
        _minefieldView.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        _vboxContainer.AddChild(_minefieldView);
        _minefieldView.Build(mode.Width, mode.Height);

        for (int x = 0; x < mode.Width; x++)
        {
            for (int y = 0; y < mode.Height; y++)
            {
                var cell = _minefieldView.GetCell(x, y);
                if (cell == null) continue;
                int cx = x, cy = y;
                cell.LeftClicked += () => OnCellLeftClicked(cx, cy);
                cell.RightClicked += () => OnCellRightClicked(cx, cy);
                cell.ChordClicked += () => OnCellChordClicked(cx, cy);
            }
        }
    }

    public override void _Process(double delta)
    {
        _timer.Update(delta);
        if (_timer.IsRunning)
            UpdateTimerLabel();
    }

    private void UpdateTimerLabel()
    {
        _timerLabel.Text = _timer.Formatted;
    }

    private void UpdateMineCounter(int count)
    {
        _mineCounter.Text = count.ToString("D3");
    }

    private int _pendingRevealCount;
    private bool _justFirstClicked;

    private void OnCellLeftClicked(int x, int y)
    {
        if (_logic == null) return;
        _justFirstClicked = !_logic.Model.MinesPlaced;
        _pendingRevealCount = 0;
        _logic.RevealCell(x, y);
        if (_justFirstClicked && _logic.Status == GameStatus.Playing)
            _timer.Start();
        _justFirstClicked = false;
    }

    private void OnCellRightClicked(int x, int y)
    {
        FlagsEverUsed = true;
        CurrentConsecutiveReveals = 0;
        _logic?.ToggleFlag(x, y);
    }

    private void OnCellChordClicked(int x, int y)
    {
        if (_logic == null) return;
        _pendingRevealCount = 0;
        _logic.ChordReveal(x, y);
        if (_pendingRevealCount > MaxChordRevealCount)
            MaxChordRevealCount = _pendingRevealCount;
        // Chord also resets consecutive reveals (since it involves flag placement)
        // Actually no, chord doesn't place flags. The consecutive reveal chain continues.
    }

    private void OnSmileyPressed()
    {
        NewGame(_currentMode);
    }

    private void OnCellRevealed(Vector2I pos, int adjacentMines)
    {
        _minefieldView?.UpdateCell(pos.X, pos.Y, CellState.Revealed, adjacentMines);
        _pendingRevealCount++;
        CurrentConsecutiveReveals++;
        if (CurrentConsecutiveReveals > MaxConsecutiveReveals)
            MaxConsecutiveReveals = CurrentConsecutiveReveals;
    }

    private void OnCellsRevealedBatch(List<Vector2I> cells)
    {
        if (_logic == null || _minefieldView == null) return;
        foreach (var pos in cells)
        {
            int adj = _logic.Model.AdjacentMineCounts[pos.X, pos.Y];
            _minefieldView.UpdateCell(pos.X, pos.Y, CellState.Revealed, adj);
        }
        _pendingRevealCount += cells.Count;
        CurrentConsecutiveReveals += cells.Count;
        if (CurrentConsecutiveReveals > MaxConsecutiveReveals)
            MaxConsecutiveReveals = CurrentConsecutiveReveals;

        if (_justFirstClicked)
        {
            FirstClickRevealCount = cells.Count;
            _justFirstClicked = false;
        }
    }

    private void OnMineHit(Vector2I pos)
    {
        if (_logic == null || _minefieldView == null) return;
        _timer.Stop();
        _smileyButton.Text = ":(";
        _minefieldView.RevealAllMines(
            _logic.Model.MinePositions, pos, _logic.DisplayState);
        DisableAllCells();
        GameEnded?.Invoke(false, _timer.Elapsed, _currentMode);
    }

    private void OnGameWon()
    {
        if (_logic == null || _minefieldView == null) return;
        _timer.Stop();
        _smileyButton.Text = "B)";
        for (int x = 0; x < _logic.Model.Width; x++)
        {
            for (int y = 0; y < _logic.Model.Height; y++)
            {
                if (_logic.Model.MineMap[x, y]
                    && _logic.DisplayState[x, y] != CellState.Flagged)
                {
                    _logic.DisplayState[x, y] = CellState.Flagged;
                    _minefieldView.UpdateCell(x, y, CellState.Flagged, -1);
                }
            }
        }
        DisableAllCells();
        GameEnded?.Invoke(true, _timer.Elapsed, _currentMode);
    }

    private void OnFlagChanged(Vector2I pos, CellState newState)
    {
        _minefieldView?.UpdateCell(pos.X, pos.Y, newState, -1);
        if (_logic != null)
            UpdateMineCounter(_currentMode.MineCount - _logic.FlagCount);
    }

    private void DisableAllCells()
    {
        if (_minefieldView == null) return;
        for (int x = 0; x < _currentMode.Width; x++)
        {
            for (int y = 0; y < _currentMode.Height; y++)
            {
                var cell = _minefieldView.GetCell(x, y);
                if (cell != null)
                    cell.MouseFilter = MouseFilterEnum.Ignore;
            }
        }
    }
}
