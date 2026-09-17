namespace Baudr.Core.Models;

public readonly record struct PlotDataPoint(
    string SeriesName,
    double Value,
    DateTimeOffset Timestamp,
    long SampleIndex);

public class PlotSeries
{
    public string Name { get; set; } = string.Empty;
    public string ColorHex { get; set; } = "#00B4D8";
    public bool IsVisible { get; set; } = true;
    public List<double> Values { get; } = new(1000);
    public List<DateTimeOffset> Timestamps { get; } = new(1000);
    public double MinValue { get; private set; } = double.MaxValue;
    public double MaxValue { get; private set; } = double.MinValue;
    public double LatestValue => Values.Count > 0 ? Values[^1] : 0;

    public void Add(double value, DateTimeOffset timestamp, int maxRetention = 1000)
    {
        Values.Add(value);
        Timestamps.Add(timestamp);

        if (value < MinValue) MinValue = value;
        if (value > MaxValue) MaxValue = value;

        if (Values.Count > maxRetention)
        {
            int removeCount = Values.Count - maxRetention;
            Values.RemoveRange(0, removeCount);
            Timestamps.RemoveRange(0, removeCount);

            // Recompute min/max
            MinValue = double.MaxValue;
            MaxValue = double.MinValue;
            for (int i = 0; i < Values.Count; i++)
            {
                double v = Values[i];
                if (v < MinValue) MinValue = v;
                if (v > MaxValue) MaxValue = v;
            }
        }
    }

    public void Clear()
    {
        Values.Clear();
        Timestamps.Clear();
        MinValue = double.MaxValue;
        MaxValue = double.MinValue;
    }
}

