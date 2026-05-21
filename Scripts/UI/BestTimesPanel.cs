using Godot;

namespace MineSweeper;

public partial class BestTimesPanel : VBoxContainer
{
    public void Refresh(BestTimes bestTimes)
    {
        foreach (var child in GetChildren())
            child.QueueFree();

        foreach (var mode in GameMode.Presets)
        {
            var time = bestTimes.Get(mode.Name);
            var label = new Label
            {
                Text = time.HasValue
                    ? $"{mode.Name}: {GameTimer.FormatTime(time.Value)}"
                    : $"{mode.Name}: 暂无记录",
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            AddChild(label);
        }
    }
}
