using Godot;

namespace MineSweeper;

public partial class Cell : Control
{
    [Signal]
    public delegate void LeftClickedEventHandler();

    [Signal]
    public delegate void RightClickedEventHandler();

    [Signal]
    public delegate void ChordClickedEventHandler();

    public int GridX { get; set; }
    public int GridY { get; set; }

    private Panel _panel = null!;
    private Label _label = null!;

    private StyleBoxFlat _styleHidden = null!;
    private StyleBoxFlat _styleRevealed = null!;
    private StyleBoxFlat _styleMineTriggered = null!;
    private StyleBoxFlat _styleMineRevealed = null!;
    private StyleBoxFlat _styleWrongFlag = null!;

    public Cell()
    {
        CustomMinimumSize = new Vector2(28, 28);
        MouseFilter = MouseFilterEnum.Stop;

        _panel = new Panel();
        _panel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _panel.OffsetLeft = 1;
        _panel.OffsetTop = 1;
        _panel.OffsetRight = -1;
        _panel.OffsetBottom = -1;
        AddChild(_panel);

        _label = new Label();
        _label.SetAnchorsPreset(Control.LayoutPreset.Center);
        _label.HorizontalAlignment = HorizontalAlignment.Center;
        _label.VerticalAlignment = VerticalAlignment.Center;
        _label.AddThemeFontSizeOverride("font_size", 14);
        AddChild(_label);

        CreateStyles();
        SetDisplay(CellState.Hidden, -1);
    }

    private void CreateStyles()
    {
        _styleHidden = new StyleBoxFlat
        {
            BgColor = ColorPalette.PanelHidden,
            ShadowSize = 3,
            ShadowOffset = new Vector2(1, 1),
            ShadowColor = new Color(0.4f, 0.4f, 0.4f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            BorderColor = new Color(0.5f, 0.5f, 0.5f),
        };

        _styleRevealed = new StyleBoxFlat
        {
            BgColor = ColorPalette.PanelRevealed,
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            BorderColor = new Color(0.55f, 0.55f, 0.55f),
        };

        _styleMineTriggered = new StyleBoxFlat
        {
            BgColor = ColorPalette.MineTriggered,
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            BorderColor = new Color(0.55f, 0.55f, 0.55f),
        };

        _styleMineRevealed = new StyleBoxFlat
        {
            BgColor = ColorPalette.MineRevealed,
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            BorderColor = new Color(0.55f, 0.55f, 0.55f),
        };

        _styleWrongFlag = new StyleBoxFlat
        {
            BgColor = ColorPalette.MineRevealed,
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            BorderColor = new Color(0.55f, 0.55f, 0.55f),
        };
    }

    public void SetDisplay(CellState state, int adjacentMines)
    {
        switch (state)
        {
            case CellState.Hidden:
                _panel.AddThemeStyleboxOverride("panel", _styleHidden);
                _label.Text = "";
                break;

            case CellState.Revealed:
                if (adjacentMines == -1)
                {
                    _panel.AddThemeStyleboxOverride("panel", _styleMineRevealed);
                    _label.Text = ColorPalette.MineSymbol;
                    _label.AddThemeColorOverride("font_color", Colors.Black);
                }
                else if (adjacentMines == 0)
                {
                    _panel.AddThemeStyleboxOverride("panel", _styleRevealed);
                    _label.Text = "";
                }
                else
                {
                    _panel.AddThemeStyleboxOverride("panel", _styleRevealed);
                    _label.Text = adjacentMines.ToString();
                    if (adjacentMines >= 1 && adjacentMines <= 8)
                        _label.AddThemeColorOverride("font_color", ColorPalette.NumberColors[adjacentMines]);
                }
                break;

            case CellState.Flagged:
                _panel.AddThemeStyleboxOverride("panel", _styleHidden);
                _label.Text = ColorPalette.FlagSymbol;
                _label.AddThemeColorOverride("font_color", Colors.Red);
                break;

            case CellState.Question:
                _panel.AddThemeStyleboxOverride("panel", _styleHidden);
                _label.Text = ColorPalette.QuestionSymbol;
                _label.AddThemeColorOverride("font_color", Colors.Black);
                break;
        }
    }

    public void SetMineTriggered()
    {
        _panel.AddThemeStyleboxOverride("panel", _styleMineTriggered);
        _label.Text = ColorPalette.MineSymbol;
        _label.AddThemeColorOverride("font_color", Colors.Black);
    }

    public void SetWrongFlag()
    {
        _panel.AddThemeStyleboxOverride("panel", _styleWrongFlag);
        _label.Text = "X";
        _label.AddThemeColorOverride("font_color", Colors.Red);
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed)
        {
            var mask = mouseButton.ButtonMask;
            bool leftDown = (mask & MouseButtonMask.Left) != 0;
            bool rightDown = (mask & MouseButtonMask.Right) != 0;

            if (leftDown && rightDown)
            {
                EmitSignal(SignalName.ChordClicked);
                AcceptEvent();
            }
            else if (mouseButton.ButtonIndex == MouseButton.Left)
            {
                EmitSignal(SignalName.LeftClicked);
                AcceptEvent();
            }
            else if (mouseButton.ButtonIndex == MouseButton.Right)
            {
                EmitSignal(SignalName.RightClicked);
                AcceptEvent();
            }
            else if (mouseButton.ButtonIndex == MouseButton.Middle)
            {
                EmitSignal(SignalName.ChordClicked);
                AcceptEvent();
            }
        }
    }
}
