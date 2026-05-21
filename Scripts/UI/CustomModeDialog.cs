using Godot;
using System;

namespace MineSweeper;

public partial class CustomModeDialog : Window
{
    public event Action<int, int, int>? CustomModeConfirmed;

    private SpinBox _widthSpin = null!;
    private SpinBox _heightSpin = null!;
    private SpinBox _mineSpin = null!;
    private Label _errorLabel = null!;
    private Button _okButton = null!;

    public override void _Ready()
    {
        Title = "自定义模式";
        Unresizable = true;
        AlwaysOnTop = true;
        Size = new Vector2I(300, 240);

        var vbox = new VBoxContainer();
        vbox.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        vbox.AddThemeConstantOverride("separation", 8);
        vbox.OffsetLeft = 16;
        vbox.OffsetTop = 16;
        vbox.OffsetRight = -16;
        vbox.OffsetBottom = -16;
        AddChild(vbox);

        var range = GameMode.CustomRange;
        int minW = range.MinWidth, maxW = range.MaxWidth;
        int minH = range.MinHeight, maxH = range.MaxHeight;

        _widthSpin = CreateSpinRow(vbox, $"宽度 ({minW}-{maxW}):", minW, maxW, 9);
        _heightSpin = CreateSpinRow(vbox, $"高度 ({minH}-{maxH}):", minH, maxH, 9);
        _mineSpin = CreateSpinRow(vbox, "雷数:", 1, maxW * maxH - 1, 10);

        _widthSpin.ValueChanged += OnSizeChanged;
        _heightSpin.ValueChanged += OnSizeChanged;

        _errorLabel = new Label
        {
            Text = "",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        _errorLabel.AddThemeColorOverride("font_color", Colors.Red);
        vbox.AddChild(_errorLabel);

        var btnHBox = new HBoxContainer();
        btnHBox.Alignment = BoxContainer.AlignmentMode.Center;
        btnHBox.AddThemeConstantOverride("separation", 16);
        vbox.AddChild(btnHBox);

        _okButton = new Button { Text = "开始游戏" };
        _okButton.Pressed += OnOkPressed;
        btnHBox.AddChild(_okButton);

        var cancelBtn = new Button { Text = "取消" };
        cancelBtn.Pressed += () => Hide();
        btnHBox.AddChild(cancelBtn);

        CloseRequested += () => Hide();
    }

    private SpinBox CreateSpinRow(VBoxContainer parent, string labelText, int min, int max, int value)
    {
        var hbox = new HBoxContainer();
        var label = new Label { Text = labelText };
        hbox.AddChild(label);
        var spin = new SpinBox { MinValue = min, MaxValue = max, Value = value };
        hbox.AddChild(spin);
        parent.AddChild(hbox);
        return spin;
    }

    private void OnSizeChanged(double _)
    {
        int w = (int)_widthSpin.Value;
        int h = (int)_heightSpin.Value;
        int maxMines = w * h - 1;
        _mineSpin.MaxValue = maxMines;
        if (_mineSpin.Value > maxMines)
            _mineSpin.Value = maxMines;
        ValidateInput();
    }

    private void ValidateInput()
    {
        int w = (int)_widthSpin.Value;
        int h = (int)_heightSpin.Value;
        int m = (int)_mineSpin.Value;

        if (m < 1)
            _errorLabel.Text = "雷数至少为1";
        else if (m >= w * h)
            _errorLabel.Text = "雷数必须小于格子总数";
        else if (w * h - 9 < m)
            _errorLabel.Text = "雷数过多，无法保证首次点击安全";
        else
            _errorLabel.Text = "";

        _okButton.Disabled = _errorLabel.Text != "";
    }

    private void OnOkPressed()
    {
        if (_errorLabel.Text != "") return;
        Hide();
        CustomModeConfirmed?.Invoke(
            (int)_widthSpin.Value, (int)_heightSpin.Value, (int)_mineSpin.Value);
    }

    public void ShowDialog()
    {
        _widthSpin.Value = 9;
        _heightSpin.Value = 9;
        _mineSpin.Value = 10;
        OnSizeChanged(0);
        Visible = true;
        PopupCentered();
    }
}
