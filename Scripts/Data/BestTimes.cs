using System.Collections.Generic;

namespace MineSweeper;

public class BestTimes
{
    public Dictionary<string, double?> Times { get; set; } = new();

    public void Load()
    {
        Times = DataStore.Load<BestTimes>("best_times.json").Times;
    }

    public void Save()
    {
        DataStore.Save("best_times.json", this);
    }

    public bool TryUpdate(string modeName, double time)
    {
        if (!Times.TryGetValue(modeName, out var current) || current == null || time < current)
        {
            Times[modeName] = time;
            Save();
            return true;
        }
        return false;
    }

    public double? Get(string modeName)
    {
        return Times.TryGetValue(modeName, out var time) ? time : null;
    }
}
