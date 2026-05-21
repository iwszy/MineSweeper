using Godot;
using System;

namespace MineSweeper;

public partial class AchievementGallery : Control
{
    public event Action? BackRequested;

    public override void _Ready() {
        SetAnchorsPreset(Control.LayoutPreset.FullRect);
    }

    public override void _UnhandledInput(InputEvent @event) {
        if (@event is InputEventKey { Pressed: true, Keycode: Key.Escape }) {
            BackRequested?.Invoke();
        }
    }

    public void Refresh(AchievementData data) {
        foreach (var child in GetChildren()) {
            child.QueueFree();
        }

        var margin = new MarginContainer();
        margin.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 24);
        margin.AddThemeConstantOverride("margin_top", 24);
        margin.AddThemeConstantOverride("margin_right", 24);
        margin.AddThemeConstantOverride("margin_bottom", 24);
        AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 12);
        margin.AddChild(vbox);

        var title = new Label {
            Text = "成就",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        title.AddThemeFontSizeOverride("font_size", 36);
        vbox.AddChild(title);

        var scroll = new ScrollContainer();
        scroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        vbox.AddChild(scroll);

        var grid = new GridContainer();
        grid.Columns = 3;
        grid.AddThemeConstantOverride("h_separation", 8);
        grid.AddThemeConstantOverride("v_separation", 8);
        scroll.AddChild(grid);

        foreach (var def in AchievementDef.All) {
            var panel = new Panel();
            panel.CustomMinimumSize = new Vector2(200, 80);
            var unlocked = data.IsUnlocked(def.Id);
            var styleBox = new StyleBoxFlat {
                BgColor = unlocked
                    ? new Color(0.2f, 0.5f, 0.2f)
                    : new Color(0.3f, 0.3f, 0.3f),
                BorderWidthLeft = 2,
                BorderWidthTop = 2,
                BorderWidthRight = 2,
                BorderWidthBottom = 2,
                BorderColor = unlocked
                    ? new Color(0.3f, 0.7f, 0.3f)
                    : new Color(0.5f, 0.5f, 0.5f),
            };
            panel.AddThemeStyleboxOverride("panel", styleBox);

            var innerVbox = new VBoxContainer();
            innerVbox.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            innerVbox.OffsetLeft = 8;
            innerVbox.OffsetTop = 6;
            innerVbox.OffsetRight = -8;
            innerVbox.OffsetBottom = -6;
            panel.AddChild(innerVbox);

            var nameLabel = new Label {
                Text = unlocked ? def.Name : "???",
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            nameLabel.AddThemeFontSizeOverride("font_size", 16);
            innerVbox.AddChild(nameLabel);

            var descLabel = new Label {
                Text = unlocked ? def.Description : "达成条件隐藏",
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            descLabel.AddThemeFontSizeOverride("font_size", 11);
            innerVbox.AddChild(descLabel);

            grid.AddChild(panel);
        }

        var backBtn = new Button { Text = "返回" };
        backBtn.Pressed += () => BackRequested?.Invoke();
        vbox.AddChild(backBtn);
    }
}