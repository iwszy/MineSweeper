using Godot;
using System.Collections.Generic;

namespace MineSweeper;

public partial class GameManager : Control
{
    private Control _mainMenu = null!;
    private GameScreen _gameScreen = null!;
    private Panel _achievementPanel = null!;
    private VBoxContainer _achievementContainer = null!;
    private Panel _achievementTemplate = null!;
    private BestTimes _bestTimes = null!;
    private AchievementData _achievementData = null!;

    private Panel _customPanel = null!;
    private SpinBox _spinWidth = null!;
    private SpinBox _spinHeight = null!;
    private SpinBox _spinMines = null!;
    private HSlider _sliderWidth = null!;
    private HSlider _sliderHeight = null!;
    private HSlider _sliderMines = null!;
    private Button _btnCustomConfirm = null!;

    private GameMode _lastMode;
    private Button[] _modeButtons = new Button[6];
    private Label[] _modeLabels = new Label[6];

    // Achievement notification
    private Panel _newAchvPanel = null!;
    private Label _newAchvName = null!;
    private Queue<AchievementDef.Achievement> _achvQueue = new();
    private bool _achvAnimating;
    private float _achvAnimProgress;
    private float _achvTargetY;

    public override void _Ready() {
        _bestTimes = new BestTimes();
        _bestTimes.Load();

        _achievementData = new AchievementData();
        _achievementData.Load();

        _mainMenu = GetNode<Control>("MainMenu");
        _gameScreen = GetNode<GameScreen>("GameScreen");
        _gameScreen.GameEnded += OnGameEnded;
        _gameScreen.BackRequested += OnBackFromGame;
        _gameScreen.Visible = false;

        GetNode<Button>("MainMenu/Button_Setting").Pressed += () =>
            GD.Print("Settings not implemented");

        var presets = GameMode.Presets;
        for (int i = 0; i < 6; i++) {
            var row = GetNode<Control>($"MainMenu/VBoxContainer_ModeButtons/Control_Mode{i}Button");
            _modeButtons[i] = row.GetNode<Button>("Button");
            _modeLabels[i] = row.GetNode<Label>("Label");

            if (i < presets.Length) {
                var mode = presets[i];
                _modeButtons[i].Text = mode.Name;
                _modeButtons[i].Pressed += () => StartGame(mode);
            } else {
                _modeButtons[i].Pressed += ShowCustomModeDialog;
            }
        }

        RefreshBestTimes();

        _achievementPanel = GetNode<Panel>("MainMenu/Panel_Achievements");
        _achievementPanel.GetNode<Button>("Button_Home").Pressed += HideAchievements;
        GetNode<Button>("MainMenu/Button_Achievement").Pressed += ShowAchievements;

        var scroll = _achievementPanel.GetNode<ScrollContainer>("ScrollContainer");
        _achievementContainer = scroll.GetNode<VBoxContainer>("VBoxContainer_Achievements");
        _achievementTemplate = _achievementContainer.GetNode<Panel>("Panel_Achievement");

        _customPanel = GetNode<Panel>("MainMenu/Panel_Custom");
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
        _customPanel.GetNode<Button>("Button_Cancel").Pressed += () =>
            _customPanel.Visible = false;

        _newAchvPanel = GetNode<Panel>("Panel_NewAchievement");
        _newAchvName = _newAchvPanel.GetNode<Label>("Label_AchievementName");
        _newAchvPanel.Visible = false;
        _achvTargetY = _newAchvPanel.Position.Y;
    }

    private void StartGame(GameMode mode) {
        _lastMode = mode;
        _achievementPanel.Visible = false;
        _mainMenu.Visible = false;
        _gameScreen.Visible = true;
        _gameScreen.NewGame(mode);
    }

    private void ShowCustomModeDialog() {
        _spinWidth.Value = 9;
        _spinHeight.Value = 9;
        _spinMines.Value = 10;
        UpdateCustomMineMax();
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
        int maxMines = w * h - 9; // must leave room for first-click safe zone
        if (maxMines < 1) maxMines = 1;
        _spinMines.MaxValue = maxMines;
        _spinMines.Suffix = $" / {maxMines}";
        _sliderMines.MaxValue = maxMines;
        if (_spinMines.Value > maxMines) {
            _spinMines.Value = maxMines;
        }
    }

    private void ShowAchievements() {
        var children = _achievementContainer.GetChildren();
        for (int i = children.Count - 1; i >= 0; i--) {
            if (children[i] != _achievementTemplate) {
                children[i].QueueFree();
            }
        }

        _achievementTemplate.Visible = false;

        foreach (var def in AchievementDef.All) {
            var panel = (Panel)_achievementTemplate.Duplicate();
            panel.Visible = true;

            var unlocked = _achievementData.IsUnlocked(def.Id);
            var nameLabel = panel.GetNode<Label>("Label_Name");
            var descLabel = panel.GetNode<Label>("Label_Description");

            nameLabel.Text = def.Name;
            descLabel.Text = def.Description;

            if (unlocked) {
                nameLabel.AddThemeColorOverride("font_color",
                    new Color(0.25f, 0.18f, 0.05f, 1f));
                descLabel.AddThemeColorOverride("font_color",
                    new Color(0.35f, 0.28f, 0.15f, 1f));
                panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat {
                    BgColor = new Color(0.98f, 0.96f, 0.9f, 1f),
                    BorderWidthLeft = 2, BorderWidthTop = 2,
                    BorderWidthRight = 2, BorderWidthBottom = 2,
                    BorderColor = new Color(0.75f, 0.6f, 0.25f, 1f),
                    CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6,
                    CornerRadiusBottomRight = 6, CornerRadiusBottomLeft = 6,
                    ShadowColor = new Color(0.65f, 0.5f, 0.15f, 0.2f),
                    ShadowSize = 3,
                    ShadowOffset = new Vector2(0, 1),
                });
            } else {
                nameLabel.AddThemeColorOverride("font_color",
                    new Color(0.45f, 0.45f, 0.48f, 1f));
                descLabel.AddThemeColorOverride("font_color",
                    new Color(0.55f, 0.55f, 0.58f, 1f));
                panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat {
                    BgColor = new Color(0.88f, 0.88f, 0.89f, 1f),
                    BorderWidthLeft = 1, BorderWidthTop = 1,
                    BorderWidthRight = 1, BorderWidthBottom = 1,
                    BorderColor = new Color(0.78f, 0.78f, 0.8f, 1f),
                    CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6,
                    CornerRadiusBottomRight = 6, CornerRadiusBottomLeft = 6,
                });
            }

            _achievementContainer.AddChild(panel);
        }

        _achievementPanel.Visible = true;
    }

    private void HideAchievements() {
        _achievementPanel.Visible = false;
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
        _mainMenu.Visible = true;
        RefreshBestTimes();
    }

    public override void _Process(double delta) {
        if (_achvAnimating) {
            AnimateAchievementPopup((float)delta);
        }
    }

    private void ShowNextAchievement() {
        if (_achvQueue.Count == 0) {
            return;
        }

        var achv = _achvQueue.Dequeue();
        _newAchvName.Text = achv.Name;

        // Start from above the screen
        _newAchvPanel.Position = new Vector2(
            _newAchvPanel.Position.X,
            -_newAchvPanel.Size.Y - 20
        );
        _newAchvPanel.Visible = true;

        _achvAnimating = true;
        _achvAnimProgress = 0f;
    }

    private void AnimateAchievementPopup(float delta) {
        _achvAnimProgress = Mathf.Min(_achvAnimProgress + delta / 0.5f, 1f);
        float t = _achvAnimProgress;

        // Bounce curve: slide from above → overshoot down → settle
        float y;
        if (t < 0.55f) {
            y = Mathf.Lerp(-_newAchvPanel.Size.Y - 20, _achvTargetY + 15, t / 0.55f);
        } else if (t < 0.8f) {
            y = Mathf.Lerp(_achvTargetY + 15, _achvTargetY - 5, (t - 0.55f) / 0.25f);
        } else {
            y = Mathf.Lerp(_achvTargetY - 5, _achvTargetY, (t - 0.8f) / 0.2f);
        }

        _newAchvPanel.Position = new Vector2(_newAchvPanel.Position.X, y);

        if (_achvAnimProgress >= 1f) {
            _achvAnimating = false;
            _newAchvPanel.Position = new Vector2(_newAchvPanel.Position.X, _achvTargetY);

            // Display for 2 seconds, then hide and show next
            GetTree().CreateTimer(1.0).Timeout += () => {
                _newAchvPanel.Visible = false;
                if (_achvQueue.Count > 0) {
                    ShowNextAchievement();
                }
            };
        }
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

        if (won && !mode.IsCustom && _bestTimes.TryUpdate(mode.Name, time)) {
            _gameScreen.PlayStampAnimation();
        }

        foreach (var achv in newAchievements)
            _achvQueue.Enqueue(achv);

        if (!_achvAnimating && _achvQueue.Count > 0)
            ShowNextAchievement();

        RefreshBestTimes();
    }
}