using System.Collections.Generic;

namespace MineSweeper;

public class ModeConfig
{
    public List<PresetEntry> Presets { get; set; } = new();
    public CustomModeRange CustomMode { get; set; } = new();
}

public class PresetEntry
{
    public string Name { get; set; } = "";
    public int Width { get; set; }
    public int Height { get; set; }
    public int Mines { get; set; }
}

public class CustomModeRange
{
    public int MinWidth { get; set; } = 5;
    public int MaxWidth { get; set; } = 40;
    public int MinHeight { get; set; } = 5;
    public int MaxHeight { get; set; } = 40;
}

public class AchievementConfig
{
    public List<AchievementEntry> Achievements { get; set; } = new();
}

public class AchievementEntry
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
}
