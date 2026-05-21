using Godot;
using System;

namespace MineSweeper;

public partial class MainMenuScreen : Control
{
    public event Action<GameMode>? GameStartRequested;
    public event Action? AchievementsRequested;
    public event Action? CustomModeRequested;

    private VBoxContainer _mainVBox = null!;
    private BestTimesPanel _bestTimesPanel = null!;

    public override void _Ready() {
        SetAnchorsPreset(Control.LayoutPreset.FullRect);

        _mainVBox = new VBoxContainer();
        _mainVBox.SetAnchorsPreset(Control.LayoutPreset.Center);
        _mainVBox.AddThemeConstantOverride("separation", 12);
        AddChild(_mainVBox);

        var title = new Label {
            Text = "扫 雷",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        title.AddThemeFontSizeOverride("font_size", 48);
        _mainVBox.AddChild(title);

        // Mode label
        var modeLabel = new Label {
            Text = "选择模式",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        modeLabel.AddThemeFontSizeOverride("font_size", 18);
        _mainVBox.AddChild(modeLabel);

        // Mode buttons
        foreach (var mode in GameMode.Presets) {
            var btn = CreateModeButton(mode);
            _mainVBox.AddChild(btn);
        }

        var customBtn = new Button { Text = "自定义..." };
        customBtn.Pressed += () => CustomModeRequested?.Invoke();
        _mainVBox.AddChild(customBtn);

        // Separator
        var sep = new HSeparator();
        _mainVBox.AddChild(sep);

        // Best times
        _bestTimesPanel = new BestTimesPanel();
        _mainVBox.AddChild(_bestTimesPanel);

        // Bottom buttons
        var sep2 = new HSeparator();
        _mainVBox.AddChild(sep2);

        var bottomHBox = new HBoxContainer();
        bottomHBox.AddThemeConstantOverride("separation", 16);
        bottomHBox.Alignment = BoxContainer.AlignmentMode.Center;
        _mainVBox.AddChild(bottomHBox);

        var settingsBtn = new Button { Text = "设置" };
        settingsBtn.Pressed += () => GD.Print("Settings not implemented yet");
        bottomHBox.AddChild(settingsBtn);

        var achBtn = new Button { Text = "成就" };
        achBtn.Pressed += () => AchievementsRequested?.Invoke();
        bottomHBox.AddChild(achBtn);

        var exitBtn = new Button { Text = "退出" };
        exitBtn.Pressed += () => GetTree().Quit();
        bottomHBox.AddChild(exitBtn);
    }

    private Button CreateModeButton(GameMode mode) {
        var btn = new Button {
            Text = $"{mode.Name}  ({mode.Width}x{mode.Height}, {mode.MineCount}雷)",
        };
        btn.Pressed += () => GameStartRequested?.Invoke(mode);
        return btn;
    }

    public override void _UnhandledInput(InputEvent @event) {
        if (@event is InputEventKey { Pressed: true, Keycode: Key.Escape }) {
            GetTree().Quit();
        }
    }

    public void Refresh(BestTimes bestTimes) {
        _bestTimesPanel.Refresh(bestTimes);
    }
}