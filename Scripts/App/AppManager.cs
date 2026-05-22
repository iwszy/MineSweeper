using Godot;

namespace MineSweeper;

public partial class AppManager : Control
{
    private GameScreen _gameScreen = null!;
    private Panel _achievementPanel = null!;
    private VBoxContainer _achievementContainer = null!;
    private Panel _achievementTemplate = null!;
    private CustomModeDialog _customModeDialog = null!;
    private ResultDialog _resultDialog = null!;
    private BestTimes _bestTimes = null!;
    private AchievementData _achievementData = null!;

    private GameMode _lastMode;

    // Mode row nodes (0-4 presets, 5 custom)
    private Button[] _modeButtons = new Button[6];
    private Label[] _modeLabels = new Label[6];

    public override void _Ready() {
        _bestTimes = new BestTimes();
        _bestTimes.Load();

        _achievementData = new AchievementData();
        _achievementData.Load();

        // Load GameScreen from scene
        var gameScene = ResourceLoader.Load<PackedScene>("res://scenes/game_screen.tscn");
        _gameScreen = gameScene.Instantiate<GameScreen>();
        _gameScreen.GameEnded += OnGameEnded;
        _gameScreen.BackRequested += OnBackFromGame;
        _gameScreen.Visible = false;
        AddChild(_gameScreen);

        // Wire settings button
        GetNode<Button>("Button_Setting").Pressed += () =>
            GD.Print("Settings not implemented");

        // Wire mode buttons
        var presets = GameMode.Presets;
        for (int i = 0; i < 6; i++) {
            var row = GetNode<Control>($"VBoxContainer_ModeButtons/Control_Mode{i}Button");
            _modeButtons[i] = row.GetNode<Button>("Button");
            _modeLabels[i] = row.GetNode<Label>("Label");

            if (i < presets.Length) {
                var mode = presets[i];
                _modeButtons[i].Text = mode.Name;
                var captured = mode;
                _modeButtons[i].Pressed += () => StartGame(captured);
            } else {
                // Custom mode button (index 5)
                _modeButtons[i].Pressed += ShowCustomModeDialog;
            }
        }

        RefreshBestTimes();

        // Wire achievement button
        _achievementPanel = GetNode<Panel>("Panel_Achievements");
        _achievementPanel.GetNode<Button>("Button_Home").Pressed += () => {
            _achievementPanel.Visible = false;
        };
        GetNode<Button>("Button_Achievement").Pressed += ShowAchievements;

        // Capture the template panel (first child of VBoxContainer)
        var scroll = _achievementPanel.GetNode<ScrollContainer>("ScrollContainer");
        _achievementContainer = scroll.GetNode<VBoxContainer>("VBoxContainer_Achievements");
        _achievementTemplate = _achievementContainer.GetNode<Panel>("Panel_Achievement");

        // Custom mode and result dialogs (Window-based popups)
        _customModeDialog = new CustomModeDialog();
        _customModeDialog.CustomModeConfirmed += (w, h, m) =>
            StartGame(GameMode.Custom(w, h, m));
        AddChild(_customModeDialog);

        _resultDialog = new ResultDialog();
        _resultDialog.PlayAgainRequested += () => {
            _resultDialog.Visible = false;
            _gameScreen.NewGame(_lastMode);
        };
        _resultDialog.BackToMenuRequested += () => {
            _resultDialog.Visible = false;
            _gameScreen.Visible = false;
            RefreshBestTimes();
        };
        AddChild(_resultDialog);
    }

    private void StartGame(GameMode mode) {
        _lastMode = mode;
        _achievementPanel.Visible = false;
        _gameScreen.Visible = true;
        _gameScreen.NewGame(mode);
    }

    private void ShowCustomModeDialog() {
        _customModeDialog.ShowDialog();
    }

    private void ShowAchievements() {
        // Clear old panels (keep the template, which is the last child)
        var children = _achievementContainer.GetChildren();
        for (int i = children.Count - 1; i >= 0; i--) {
            if (children[i] != _achievementTemplate)
                children[i].QueueFree();
        }
        _achievementTemplate.Visible = false;

        foreach (var def in AchievementDef.All) {
            var panel = (Panel)_achievementTemplate.Duplicate();
            panel.Visible = true;
            panel.GetNode<Label>("Label_Name").Text = _achievementData.IsUnlocked(def.Id)
                ? def.Name : "???";
            panel.GetNode<Label>("Label_Description").Text = _achievementData.IsUnlocked(def.Id)
                ? def.Description : "达成条件隐藏";
            _achievementContainer.AddChild(panel);
        }

        _achievementPanel.Visible = true;
    }

    private void RefreshBestTimes() {
        for (int i = 0; i < GameMode.Presets.Length && i < _modeLabels.Length; i++) {
            var mode = GameMode.Presets[i];
            var time = _bestTimes.Get(mode.Name);
            _modeLabels[i].Text = time.HasValue
                ? GameTimer.FormatTime(time.Value)
                : "暂无记录";
        }
    }

    private void OnBackFromGame() {
        _gameScreen.Visible = false;
        RefreshBestTimes();
    }

    private void OnGameEnded(bool won, double time, GameMode mode) {
        var logic = _gameScreen.CurrentLogic;

        bool allMinesFlagged = false;
        if (won && logic != null) {
            allMinesFlagged = true;
            for (int x = 0; x < logic.Model.Width; x++)
                for (int y = 0; y < logic.Model.Height; y++)
                    if (logic.Model.MineMap[x, y]
                        && logic.DisplayState[x, y] != CellState.Flagged)
                        allMinesFlagged = false;
        }

        var newAchievements = _achievementData.CheckOnGameEnd(
            won, mode, time,
            logic?.FlagCount ?? 0,
            allMinesFlagged,
            _gameScreen.FlagsEverUsed,
            _gameScreen.MaxChordRevealCount,
            _gameScreen.FirstClickRevealCount,
            _gameScreen.MaxConsecutiveReveals,
            _bestTimes);

        if (won && !mode.IsCustom) {
            bool isNewBest = _bestTimes.TryUpdate(mode.Name, time);
            _resultDialog.ShowResult(won, time, _bestTimes.Get(mode.Name), isNewBest,
                newAchievements);
        } else {
            _resultDialog.ShowResult(won, time, null, false,
                newAchievements.Count > 0 ? newAchievements : null);
        }
    }
}
