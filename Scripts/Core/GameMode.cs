namespace MineSweeper;

public readonly struct GameMode
{
    public string Name { get; }
    public int Width { get; }
    public int Height { get; }
    public int MineCount { get; }
    public bool IsCustom { get; }
    public int TotalCells => Width * Height;

    private GameMode(string name, int width, int height, int mineCount, bool isCustom)
    {
        Name = name;
        Width = width;
        Height = height;
        MineCount = mineCount;
        IsCustom = isCustom;
    }

    private static GameMode[]? _presets;
    private static CustomModeRange? _customRange;

    public static GameMode[] Presets
    {
        get
        {
            _presets ??= LoadPresets();
            return _presets;
        }
    }

    public static CustomModeRange CustomRange
    {
        get
        {
            _customRange ??= LoadCustomRange();
            return _customRange;
        }
    }

    private static GameMode[] LoadPresets()
    {
        var config = DataStore.LoadRes<ModeConfig>("res://StreamingAssets/mode_config.json");

        if (config.Presets.Count == 0)
        {
            // Fallback defaults
            return new[]
            {
                new GameMode("入门", 9, 9, 10, false),
                new GameMode("普通", 16, 16, 40, false),
                new GameMode("进阶", 30, 16, 99, false),
                new GameMode("大师", 22, 22, 150, false),
                new GameMode("超凡", 28, 24, 240, false),
            };
        }

        var presets = new GameMode[config.Presets.Count];
        for (int i = 0; i < config.Presets.Count; i++)
        {
            var p = config.Presets[i];
            presets[i] = new GameMode(p.Name, p.Width, p.Height, p.Mines, false);
        }
        return presets;
    }

    private static CustomModeRange LoadCustomRange()
    {
        var config = DataStore.LoadRes<ModeConfig>("res://StreamingAssets/mode_config.json");
        return config.CustomMode;
    }

    public static GameMode Custom(int width, int height, int mineCount)
        => new("自定义", width, height, mineCount, true);
}
