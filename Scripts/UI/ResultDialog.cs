using Godot;
using System;
using System.Collections.Generic;

namespace MineSweeper;

public partial class ResultDialog : Window
{
    public event Action? PlayAgainRequested;
    public event Action? BackToMenuRequested;

    private Label _titleLabel = null!;
    private Label _timeLabel = null!;
    private Label _bestLabel = null!;
    private Label _achievementsLabel = null!;

    public override void _Ready()
    {
        Title = "游戏结束";
        Unresizable = true;
        AlwaysOnTop = true;
        Size = new Vector2I(300, 250);
        Visible = false;

        var vbox = new VBoxContainer();
        vbox.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        vbox.AddThemeConstantOverride("separation", 8);
        vbox.OffsetLeft = 20;
        vbox.OffsetTop = 16;
        vbox.OffsetRight = -20;
        vbox.OffsetBottom = -16;
        AddChild(vbox);

        _titleLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        _titleLabel.AddThemeFontSizeOverride("font_size", 24);
        vbox.AddChild(_titleLabel);

        _timeLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        _timeLabel.AddThemeFontSizeOverride("font_size", 16);
        vbox.AddChild(_timeLabel);

        _bestLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        _bestLabel.AddThemeFontSizeOverride("font_size", 14);
        vbox.AddChild(_bestLabel);

        _achievementsLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Text = "",
        };
        _achievementsLabel.AddThemeFontSizeOverride("font_size", 14);
        vbox.AddChild(_achievementsLabel);

        vbox.AddChild(new HSeparator());

        var btnHBox = new HBoxContainer();
        btnHBox.Alignment = BoxContainer.AlignmentMode.Center;
        btnHBox.AddThemeConstantOverride("separation", 16);
        vbox.AddChild(btnHBox);

        var playAgainBtn = new Button { Text = "再来一局" };
        playAgainBtn.Pressed += () =>
        {
            Hide();
            PlayAgainRequested?.Invoke();
        };
        btnHBox.AddChild(playAgainBtn);

        var backBtn = new Button { Text = "返回主菜单" };
        backBtn.Pressed += () =>
        {
            Hide();
            BackToMenuRequested?.Invoke();
        };
        btnHBox.AddChild(backBtn);

        CloseRequested += () => Hide();
    }

    public void ShowResult(bool won, double time, double? bestTime, bool isNewBest,
        List<AchievementDef.Achievement>? newAchievements)
    {
        _titleLabel.Text = won ? "你赢了!" : "游戏结束";
        _titleLabel.AddThemeColorOverride("font_color", won ? Colors.Green : Colors.Red);
        _timeLabel.Text = $"用时: {GameTimer.FormatTime(time)}";

        if (isNewBest)
            _bestLabel.Text = "新纪录!";
        else if (bestTime.HasValue)
            _bestLabel.Text = $"最佳成绩: {GameTimer.FormatTime(bestTime.Value)}";
        else
            _bestLabel.Text = "";

        if (newAchievements != null && newAchievements.Count > 0)
        {
            var names = new List<string>();
            foreach (var a in newAchievements)
                names.Add(a.Name);
            _achievementsLabel.Text = $"达成成就: {string.Join(", ", names)}";
        }
        else
        {
            _achievementsLabel.Text = "";
        }

        Visible = true;
        PopupCentered();
    }
}
