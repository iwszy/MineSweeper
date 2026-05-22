using Godot;

namespace MineSweeper;

public partial class AppManager : Control
{
    private GameScreen _gameScreen = null!;
    private Panel _achievementPanel = null!;
    private VBoxContainer _achievementContainer = null!;
    private Panel _achievementTemplate = null!;
    private ResultDialog _resultDialog = null!;
    private BestTimes _bestTimes = null!;
    private AchievementData _achievementData = null!;

    // Custom mode panel nodes
    private Panel _customPanel = null!;
    private SpinBox _spinWidth = null!;
    private SpinBox _spinHeight = null!;
    private SpinBox _spinMines = null!;
    private HSlider _sliderWidth = null!;
    private HSlider _sliderHeight = null!;
    private HSlider _sliderMines = null!;
    private Button _btnCustomConfirm = null!;

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

        // Custom mode panel
        _customPanel = GetNode<Panel>("Panel_Custom");
        _spinWidth = _customPanel.GetNode<SpinBox>("SpinBox_HonGridCount");
        _spinHeight = _customPanel.GetNode<SpinBox>("SpinBox_VerGridCount");
        _spinMines = _customPanel.GetNode<SpinBox>("SpinBox_MineCount");
        _sliderWidth = _customPanel.GetNode<HSlider>("HSlider_HonGridCount");
        _sliderHeight = _customPanel.GetNode<HSlider>("HSlider_VerGridCount");
        _sliderMines = _customPanel.GetNode<HSlider>("HSlider_MineCount");
        _btnCustomConfirm = _customPanel.GetNode<Button>("Button_Confirm");

        var range = GameMode.CustomRange;
        SetupCustomSpinSlider(_spinWidth, _sliderWidth, range.MinWidth, range.MaxWidth, 9);
        SetupCustomSpinSlider(_spinHeight, _sliderHeight, range.MinHeight, range.MaxHeight, 9);
        SetupCustomSpinSlider(_spinMines, _sliderMines, 1, range.MaxWidth * range.MaxHeight - 1, 10);

        _spinWidth.ValueChanged += _ => UpdateCustomMineMax();
        _spinHeight.ValueChanged += _ => UpdateCustomMineMax();

        _btnCustomConfirm.Pressed += () => {
            int w = (int)_spinWidth.Value, h = (int)_spinHeight.Value, m = (int)_spinMines.Value;
            if (m >= 1 && m < w * h && w * h - m >= 9) {
                _customPanel.Visible = false;
                StartGame(GameMode.Custom(w, h, m));
            }
        };

        _customPanel.GetNode<Button>("Button_Cancel").Pressed += () => _customPanel.Visible = false;

        // Result dialog (Window-based popup)
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
        _spinWidth.Value = 9;
        _spinHeight.Value = 9;
        _spinMines.Value = 10;
        _customPanel.Visible = true;
    }

    private static void SetupCustomSpinSlider(SpinBox spin, HSlider slider, int min, int max, int def) {
        spin.MinValue = min;
        spin.MaxValue = max;
        spin.Value = def;
        slider.MinValue = min;
        slider.MaxValue = max;
        slider.Value = def;
        spin.ValueChanged += v => slider.Value = v;
        slider.ValueChanged += v => spin.Value = v;
    }

    private void UpdateCustomMineMax() {
        int w = (int)_spinWidth.Value;
        int h = (int)_spinHeight.Value;
        int maxMines = w * h - 1;
        _spinMines.MaxValue = maxMines;
        _spinMines.Suffix = $" / {maxMines}";
        _sliderMines.MaxValue = maxMines;
        if (_spinMines.Value > maxMines)
            _spinMines.Value = maxMines;
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
        bool allMinesFlagged = won && logic is { AllMinesFlagged: true };

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
