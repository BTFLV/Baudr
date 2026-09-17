using System.Collections.ObjectModel;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Baudr.Core.Models;

namespace Baudr.App.Controls;

public class PlotterControl : Control
{
    public static readonly StyledProperty<ObservableCollection<PlotSeries>?> SeriesCollectionProperty =
        AvaloniaProperty.Register<PlotterControl, ObservableCollection<PlotSeries>?>(nameof(SeriesCollection));

    public ObservableCollection<PlotSeries>? SeriesCollection
    {
        get => GetValue(SeriesCollectionProperty);
        set => SetValue(SeriesCollectionProperty, value);
    }

    public static readonly StyledProperty<bool> AutoScaleProperty =
        AvaloniaProperty.Register<PlotterControl, bool>(nameof(AutoScale), true);

    public bool AutoScale
    {
        get => GetValue(AutoScaleProperty);
        set => SetValue(AutoScaleProperty, value);
    }

    public static readonly StyledProperty<double> ManualMinYProperty =
        AvaloniaProperty.Register<PlotterControl, double>(nameof(ManualMinY), 0.0);

    public double ManualMinY
    {
        get => GetValue(ManualMinYProperty);
        set => SetValue(ManualMinYProperty, value);
    }

    public static readonly StyledProperty<double> ManualMaxYProperty =
        AvaloniaProperty.Register<PlotterControl, double>(nameof(ManualMaxY), 100.0);

    public double ManualMaxY
    {
        get => GetValue(ManualMaxYProperty);
        set => SetValue(ManualMaxYProperty, value);
    }

    public PlotterControl()
    {
        ClipToBounds = true;
    }

    public void RequestRedraw()
    {
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bounds = Bounds;
        if (bounds.Width <= 40 || bounds.Height <= 40) return;

        // Background
        var bgBrush = this.FindResource("ThemeSurfaceBrush") as IBrush ?? Brushes.Black;
        context.FillRectangle(bgBrush, bounds);

        double paddingLeft = 50;
        double paddingRight = 15;
        double paddingTop = 15;
        double paddingBottom = 25;

        double plotWidth = bounds.Width - paddingLeft - paddingRight;
        double plotHeight = bounds.Height - paddingTop - paddingBottom;
        var plotRect = new Rect(paddingLeft, paddingTop, plotWidth, plotHeight);

        var borderBrush = this.FindResource("ThemeBorderBrush") as IBrush ?? Brushes.Gray;
        var gridPen = new Pen(this.FindResource("ThemeBorderSubtleBrush") as IBrush ?? Brushes.DarkSlateGray, 1, new DashStyle([4, 4], 0));
        var textBrush = this.FindResource("ThemeMutedBrush") as IBrush ?? Brushes.Gray;
        var typeface = new Typeface("Cascadia Code, Consolas, monospace");

        // Calculate Min and Max Y
        double minY = ManualMinY;
        double maxY = ManualMaxY;

        var seriesList = SeriesCollection;
        if (AutoScale && seriesList != null && seriesList.Count > 0)
        {
            double computedMin = double.MaxValue;
            double computedMax = double.MinValue;
            bool hasData = false;

            foreach (var series in seriesList)
            {
                if (!series.IsVisible || series.Values.Count == 0) continue;
                if (series.MinValue < computedMin) computedMin = series.MinValue;
                if (series.MaxValue > computedMax) computedMax = series.MaxValue;
                hasData = true;
            }

            if (hasData && computedMin < computedMax)
            {
                double margin = (computedMax - computedMin) * 0.1;
                minY = computedMin - margin;
                maxY = computedMax + margin;
            }
            else if (hasData)
            {
                minY = computedMin - 1.0;
                maxY = computedMax + 1.0;
            }
        }

        if (Math.Abs(maxY - minY) < 0.0001)
        {
            maxY = minY + 1.0;
        }

        // Draw horizontal grid lines and Y-axis labels
        int gridDivisions = 4;
        for (int i = 0; i <= gridDivisions; i++)
        {
            double t = (double)i / gridDivisions;
            double y = paddingTop + plotHeight * (1.0 - t);
            double val = minY + (maxY - minY) * t;

            // Grid line
            context.DrawLine(gridPen, new Point(paddingLeft, y), new Point(bounds.Width - paddingRight, y));

            // Tick label
            var label = new FormattedText(
                val.ToString("0.0#", CultureInfo.InvariantCulture),
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                10,
                textBrush);
            context.DrawText(label, new Point(paddingLeft - label.Width - 6, y - label.Height / 2));
        }

        // Plot outline
        context.DrawRectangle(new Pen(borderBrush, 1), plotRect);

        // Draw series lines
        if (seriesList == null) return;

        using (context.PushClip(plotRect))
        {
            foreach (var series in seriesList)
            {
                if (!series.IsVisible || series.Values.Count < 2) continue;

                IBrush seriesBrush = Brushes.Cyan;
                if (Color.TryParse(series.ColorHex, out var c))
                {
                    seriesBrush = new SolidColorBrush(c);
                }

                var pen = new Pen(seriesBrush, 2);
                int count = series.Values.Count;
                double xStep = plotWidth / Math.Max(1, count - 1);

                Point prev = new(
                    paddingLeft,
                    paddingTop + plotHeight * (1.0 - (series.Values[0] - minY) / (maxY - minY)));

                for (int i = 1; i < count; i++)
                {
                    double x = paddingLeft + i * xStep;
                    double normY = (series.Values[i] - minY) / (maxY - minY);
                    normY = Math.Clamp(normY, -0.2, 1.2);
                    double y = paddingTop + plotHeight * (1.0 - normY);

                    Point curr = new(x, y);
                    context.DrawLine(pen, prev, curr);
                    prev = curr;
                }
            }
        }
    }
}
