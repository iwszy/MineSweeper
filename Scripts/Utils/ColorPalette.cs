using Godot;

namespace MineSweeper;

public static class ColorPalette
{
    public static readonly Color[] NumberColors =
    {
        Colors.Transparent,
        new("#0000FF"),   // 1 - Blue
        new("#008000"),   // 2 - Green
        new("#FF0000"),   // 3 - Red
        new("#000080"),   // 4 - Dark Blue
        new("#800000"),   // 5 - Maroon
        new("#008080"),   // 6 - Teal
        new("#000000"),   // 7 - Black
        new("#808080"),   // 8 - Gray
    };

    public static readonly Color PanelHidden = new(0.753f, 0.753f, 0.753f);
    public static readonly Color PanelRevealed = new(0.663f, 0.663f, 0.663f);
    public static readonly Color MineTriggered = new(1.0f, 0.0f, 0.0f);
    public static readonly Color MineRevealed = new(0.85f, 0.75f, 0.75f);

    public const string FlagSymbol = "F";
    public const string MineSymbol = "*";
    public const string QuestionSymbol = "?";
}
