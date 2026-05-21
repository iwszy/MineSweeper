namespace MineSweeper;

public class GameTimer
{
    public double Elapsed { get; private set; }
    public bool IsRunning { get; private set; }

    public string Formatted
    {
        get
        {
            int totalSeconds = (int)Elapsed;
            int hours = totalSeconds / 3600;
            int minutes = (totalSeconds % 3600) / 60;
            double seconds = Elapsed % 60;

            if (hours > 0)
                return $"{hours:D2}:{minutes:D2}:{seconds:00.00}";
            return $"{minutes:D2}:{seconds:00.00}";
        }
    }

    public void Start() => IsRunning = true;
    public void Stop() => IsRunning = false;

    public void Reset()
    {
        Elapsed = 0;
        IsRunning = false;
    }

    public void Update(double delta)
    {
        if (IsRunning)
            Elapsed += delta;
    }

    public static string FormatTime(double seconds)
    {
        int totalSeconds = (int)seconds;
        int hours = totalSeconds / 3600;
        int minutes = (totalSeconds % 3600) / 60;
        double secs = seconds % 60;

        if (hours > 0)
            return $"{hours:D2}:{minutes:D2}:{secs:00.00}";
        return $"{minutes:D2}:{secs:00.00}";
    }
}
