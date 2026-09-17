using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Baudr.Core.Buffers;
using Baudr.Core.Models;
using Baudr.Core.Search;

namespace Baudr.App.Controls;

public class TerminalControl : Control
{
    private TerminalBuffer? _buffer;
    private ScrollBar? _verticalScrollBar;
    private double _charWidth = 8.0;
    private double _lineHeight = 18.0;
    private bool _autoScroll = true;
    private int _firstVisibleLine;
    private int _unreadCount;
    private (int Line, int Col)? _selectionStart;
    private (int Line, int Col)? _selectionEnd;
    private bool _isSelecting;
    private IReadOnlyList<TerminalLine> _cachedLines = [];

    public static readonly DirectProperty<TerminalControl, TerminalBuffer?> BufferProperty =
        AvaloniaProperty.RegisterDirect<TerminalControl, TerminalBuffer?>(
            nameof(Buffer), o => o.Buffer, (o, v) => o.Buffer = v);

    public TerminalBuffer? Buffer
    {
        get => _buffer;
        set
        {
            if (_buffer != value)
            {
                if (_buffer != null)
                {
                    _buffer.LinesAdded -= OnLinesAdded;
                    _buffer.Cleared -= OnBufferCleared;
                }
                _buffer = value;
                if (_buffer != null)
                {
                    _buffer.LinesAdded += OnLinesAdded;
                    _buffer.Cleared += OnBufferCleared;
                    _cachedLines = _buffer.GetSnapshot();
                }
                else
                {
                    _cachedLines = [];
                }
                UpdateScrollRange();
                InvalidateVisual();
            }
        }
    }

    public static readonly StyledProperty<double> TerminalFontSizeProperty =
        AvaloniaProperty.Register<TerminalControl, double>(nameof(TerminalFontSize), 13.0);

    public double TerminalFontSize
    {
        get => GetValue(TerminalFontSizeProperty);
        set => SetValue(TerminalFontSizeProperty, value);
    }

    public static readonly StyledProperty<string> TerminalFontFamilyProperty =
        AvaloniaProperty.Register<TerminalControl, string>(nameof(TerminalFontFamily), "Cascadia Code, Consolas, Menlo, Monaco, monospace");

    public string TerminalFontFamily
    {
        get => GetValue(TerminalFontFamilyProperty);
        set => SetValue(TerminalFontFamilyProperty, value);
    }

    public static readonly StyledProperty<bool> ShowLineNumbersProperty =
        AvaloniaProperty.Register<TerminalControl, bool>(nameof(ShowLineNumbers), false);

    public bool ShowLineNumbers
    {
        get => GetValue(ShowLineNumbersProperty);
        set => SetValue(ShowLineNumbersProperty, value);
    }

    public static readonly StyledProperty<TimestampMode> TimestampDisplayModeProperty =
        AvaloniaProperty.Register<TerminalControl, TimestampMode>(nameof(TimestampDisplayMode), TimestampMode.None);

    public TimestampMode TimestampDisplayMode
    {
        get => GetValue(TimestampDisplayModeProperty);
        set => SetValue(TimestampDisplayModeProperty, value);
    }

    public static readonly StyledProperty<SearchResult?> ActiveSearchResultProperty =
        AvaloniaProperty.Register<TerminalControl, SearchResult?>(nameof(ActiveSearchResult));

    public SearchResult? ActiveSearchResult
    {
        get => GetValue(ActiveSearchResultProperty);
        set => SetValue(ActiveSearchResultProperty, value);
    }

    public static readonly StyledProperty<IReadOnlyList<HighlightRule>?> HighlightRulesProperty =
        AvaloniaProperty.Register<TerminalControl, IReadOnlyList<HighlightRule>?>(nameof(HighlightRules));

    public IReadOnlyList<HighlightRule>? HighlightRules
    {
        get => GetValue(HighlightRulesProperty);
        set => SetValue(HighlightRulesProperty, value);
    }

    public bool AutoScroll
    {
        get => _autoScroll;
        set
        {
            _autoScroll = value;
            if (_autoScroll)
            {
                _unreadCount = 0;
                ScrollToBottom();
            }
        }
    }

    public int UnreadCount => _unreadCount;
    public event Action<int>? UnreadCountChanged;

    public TerminalControl()
    {
        ClipToBounds = true;
        Focusable = true;
        Cursor = new Cursor(StandardCursorType.Ibeam);
    }

    public void AttachScrollBar(ScrollBar scrollBar)
    {
        _verticalScrollBar = scrollBar;
        _verticalScrollBar.ValueChanged += (s, e) =>
        {
            _firstVisibleLine = (int)_verticalScrollBar.Value;
            int max = (int)_verticalScrollBar.Maximum;

            if (_firstVisibleLine >= max - 1)
            {
                _autoScroll = true;
                _unreadCount = 0;
                UnreadCountChanged?.Invoke(_unreadCount);
            }
            else
            {
                _autoScroll = false;
            }

            InvalidateVisual();
        };
        UpdateScrollRange();
    }

    private void OnLinesAdded(IReadOnlyList<TerminalLine> newLines)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            _cachedLines = _buffer?.GetSnapshot() ?? [];
            UpdateScrollRange();

            if (_autoScroll)
            {
                ScrollToBottom();
            }
            else
            {
                _unreadCount += newLines.Count;
                UnreadCountChanged?.Invoke(_unreadCount);
                InvalidateVisual();
            }
        });
    }

    private void OnBufferCleared()
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            _cachedLines = [];
            _unreadCount = 0;
            UnreadCountChanged?.Invoke(0);
            UpdateScrollRange();
            InvalidateVisual();
        });
    }

    public void ScrollToBottom()
    {
        _autoScroll = true;
        _unreadCount = 0;
        UnreadCountChanged?.Invoke(0);

        int visibleCount = (int)Math.Max(1, Bounds.Height / _lineHeight);
        int max = Math.Max(0, _cachedLines.Count - visibleCount);

        if (_verticalScrollBar != null)
        {
            _verticalScrollBar.Maximum = max;
            _verticalScrollBar.Value = max;
        }
        _firstVisibleLine = max;
        InvalidateVisual();
    }

    private void UpdateScrollRange()
    {
        if (_verticalScrollBar == null) return;

        int visibleCount = (int)Math.Max(1, Bounds.Height / _lineHeight);
        int max = Math.Max(0, _cachedLines.Count - visibleCount);

        _verticalScrollBar.Minimum = 0;
        _verticalScrollBar.Maximum = max;
        _verticalScrollBar.ViewportSize = visibleCount;
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        UpdateScrollRange();
        if (_autoScroll) ScrollToBottom();
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (_verticalScrollBar != null)
        {
            int delta = (int)(e.Delta.Y * 3);
            int newVal = Math.Clamp(_firstVisibleLine - delta, 0, (int)_verticalScrollBar.Maximum);
            _verticalScrollBar.Value = newVal;
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus();

        var point = e.GetPosition(this);
        int lineIdx = _firstVisibleLine + (int)(point.Y / _lineHeight);
        int colIdx = Math.Max(0, (int)(point.X / _charWidth));

        _selectionStart = (lineIdx, colIdx);
        _selectionEnd = (lineIdx, colIdx);
        _isSelecting = true;
        InvalidateVisual();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_isSelecting)
        {
            var point = e.GetPosition(this);
            int lineIdx = _firstVisibleLine + (int)(point.Y / _lineHeight);
            int colIdx = Math.Max(0, (int)(point.X / _charWidth));
            _selectionEnd = (lineIdx, colIdx);
            InvalidateVisual();
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isSelecting = false;
    }

    public string? GetSelectedText()
    {
        if (_selectionStart == null || _selectionEnd == null) return null;
        var start = _selectionStart.Value;
        var end = _selectionEnd.Value;

        if (start.Line > end.Line || (start.Line == end.Line && start.Col > end.Col))
        {
            (start, end) = (end, start);
        }

        var sb = new System.Text.StringBuilder();
        for (int l = start.Line; l <= end.Line; l++)
        {
            if (l < 0 || l >= _cachedLines.Count) continue;
            var text = _cachedLines[l].Text;
            if (l == start.Line && l == end.Line)
            {
                int s = Math.Clamp(start.Col, 0, text.Length);
                int c = Math.Clamp(end.Col - s, 0, text.Length - s);
                sb.Append(text.AsSpan(s, c));
            }
            else if (l == start.Line)
            {
                int s = Math.Clamp(start.Col, 0, text.Length);
                sb.AppendLine(text[s..]);
            }
            else if (l == end.Line)
            {
                int c = Math.Clamp(end.Col, 0, text.Length);
                sb.Append(text[..c]);
            }
            else
            {
                sb.AppendLine(text);
            }
        }

        return sb.ToString();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bounds = Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0) return;

        var typeface = new Typeface(FontFamily.Parse(TerminalFontFamily));
        _lineHeight = TerminalFontSize * 1.45;

        // Measure single char for monospace alignment
        var measureText = new FormattedText("M", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, TerminalFontSize, Brushes.White);
        _charWidth = Math.Max(7.0, measureText.Width);

        // Background
        var bgBrush = this.FindResource("TerminalBackgroundBrush") as IBrush ?? Brushes.Black;
        context.FillRectangle(bgBrush, bounds);

        var fgBrush = this.FindResource("TerminalForegroundBrush") as IBrush ?? Brushes.LightGray;
        var tsBrush = this.FindResource("TerminalTimestampBrush") as IBrush ?? Brushes.Gray;
        var lineNumBrush = this.FindResource("TerminalLineNumberBrush") as IBrush ?? Brushes.DimGray;
        var rxBrush = this.FindResource("RxBrush") as IBrush ?? Brushes.LightGreen;
        var txBrush = this.FindResource("TxBrush") as IBrush ?? Brushes.LightSkyBlue;
        var sysBrush = this.FindResource("SysBrush") as IBrush ?? Brushes.Plum;
        var selBrush = this.FindResource("TerminalSelectionBrush") as IBrush ?? new SolidColorBrush(Color.FromArgb(100, 30, 80, 180));
        var matchBrush = this.FindResource("TerminalSearchHighlightBrush") as IBrush ?? Brushes.Orange;

        int visibleLinesCount = (int)Math.Ceiling(bounds.Height / _lineHeight) + 1;
        int totalLines = _cachedLines.Count;

        for (int i = 0; i < visibleLinesCount; i++)
        {
            int lineIndex = _firstVisibleLine + i;
            if (lineIndex < 0 || lineIndex >= totalLines) break;

            var line = _cachedLines[lineIndex];
            double y = i * _lineHeight;
            double x = 8.0;

            // Optional search match highlight background
            if (ActiveSearchResult != null && ActiveSearchResult.MatchingLineIndices.Contains(lineIndex))
            {
                context.FillRectangle(matchBrush, new Rect(0, y, bounds.Width, _lineHeight));
            }

            // Optional selection highlight
            if (IsLineSelected(lineIndex))
            {
                context.FillRectangle(selBrush, new Rect(0, y, bounds.Width, _lineHeight));
            }

            // Line numbers
            if (ShowLineNumbers)
            {
                var lineNumText = new FormattedText(
                    (lineIndex + 1).ToString("D4", CultureInfo.InvariantCulture),
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    TerminalFontSize * 0.9,
                    lineNumBrush);
                context.DrawText(lineNumText, new Point(x, y + 1));
                x += 40;
            }

            // Direction badge (RX / TX / SYS)
            string dirStr = line.Direction switch
            {
                Direction.Rx => "RX",
                Direction.Tx => "TX",
                _ => "SYS"
            };
            var dirBrush = line.Direction switch
            {
                Direction.Rx => rxBrush,
                Direction.Tx => txBrush,
                _ => sysBrush
            };

            var dirText = new FormattedText(dirStr, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, TerminalFontSize * 0.85, dirBrush);
            context.DrawText(dirText, new Point(x, y + 1));
            x += 28;

            // Optional timestamps
            if (TimestampDisplayMode != TimestampMode.None)
            {
                string tsStr = TimestampDisplayMode switch
                {
                    TimestampMode.Relative => $"+{line.RelativeTime:mm\\:ss\\.fff}",
                    TimestampMode.AbsoluteTime => line.Timestamp.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture),
                    TimestampMode.AbsoluteDateTime => line.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture),
                    _ => ""
                };

                var tsFormatted = new FormattedText(tsStr, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, TerminalFontSize * 0.85, tsBrush);
                context.DrawText(tsFormatted, new Point(x, y + 1));
                x += tsFormatted.Width + 8;
            }

            // Text rendering: check highlight rules or ANSI spans
            IBrush currentTextBrush = fgBrush;
            if (HighlightRules != null && HighlightRules.Count > 0)
            {
                var rule = HighlightEngine.GetMatchingRule(line, HighlightRules);
                if (rule != null && !string.IsNullOrEmpty(rule.ForegroundHex))
                {
                    if (Color.TryParse(rule.ForegroundHex, out var ruleColor))
                    {
                        currentTextBrush = new SolidColorBrush(ruleColor);
                    }
                }
            }

            var lineContent = line.Text;
            if (line.Spans != null && line.Spans.Count > 0)
            {
                // Draw ANSI styled segments
                int charPos = 0;
                double spanX = x;
                foreach (var span in line.Spans)
                {
                    if (span.Start > charPos && span.Start <= lineContent.Length)
                    {
                        var plainPart = lineContent[charPos..span.Start];
                        var plainText = new FormattedText(plainPart, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, TerminalFontSize, fgBrush);
                        context.DrawText(plainText, new Point(spanX, y));
                        spanX += plainText.Width;
                    }

                    int spanEnd = Math.Min(lineContent.Length, span.Start + span.Length);
                    if (spanEnd > span.Start)
                    {
                        var styledPart = lineContent[span.Start..spanEnd];
                        IBrush spanBrush = fgBrush;
                        if (span.Foreground.HasValue)
                        {
                            var rgb = span.Foreground.Value;
                            spanBrush = new SolidColorBrush(Color.FromRgb(rgb.R, rgb.G, rgb.B));
                        }

                        var styledText = new FormattedText(styledPart, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, TerminalFontSize, spanBrush);
                        context.DrawText(styledText, new Point(spanX, y));
                        spanX += styledText.Width;
                    }
                    charPos = spanEnd;
                }

                if (charPos < lineContent.Length)
                {
                    var remainder = lineContent[charPos..];
                    var remText = new FormattedText(remainder, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, TerminalFontSize, fgBrush);
                    context.DrawText(remText, new Point(spanX, y));
                }
            }
            else
            {
                // Plain formatted line
                var mainText = new FormattedText(
                    lineContent,
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    TerminalFontSize,
                    currentTextBrush);
                context.DrawText(mainText, new Point(x, y));
            }
        }
    }

    private bool IsLineSelected(int lineIndex)
    {
        if (_selectionStart == null || _selectionEnd == null) return false;
        int min = Math.Min(_selectionStart.Value.Line, _selectionEnd.Value.Line);
        int max = Math.Max(_selectionStart.Value.Line, _selectionEnd.Value.Line);
        return lineIndex >= min && lineIndex <= max;
    }
}
