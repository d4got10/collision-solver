using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using MyDiplomaSolver;

namespace AvaloniaPixelDrawing;

public class GraphControl : Control
{
    public double MaxValue { get; private set; }
    public double MaxPosition { get; private set; }
    public SimulationState State { get; private set; }
    public double Time { get; private set; }
    
    public void Update(SimulationState state, double maxPosition, double maxValue, double time)
    {
        State = state;
        MaxPosition = maxPosition;
        MaxValue = maxValue;
        Time = time;
        InvalidateVisual();
    }
    
    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        context.FillRectangle(Brushes.GhostWhite, new Rect(0, 0, Bounds.Width, Bounds.Height));
        
        var linePen = new ImmutablePen(Brushes.Black, 2);

        var prevY = GetY(0);

        for (var i = State.Waves.Length - 1; i > 0; i--)
        {
            var right = State.Waves[i - 1];
            var left = State.Waves[i];
            var segment = State.Segments[i];

            var leftX = GetX(left.GetPosition(Time));
            var rightX = GetX(right.GetPosition(Time));
            var y = GetY(segment.Coefficients.C);
            
            context.DrawLine(linePen, new Point(leftX, prevY), new Point(leftX, y));
            context.DrawLine(linePen, new Point(leftX, y), new Point(rightX, y));

            prevY = y;
        }
    }
    
    private double GetX(double position)
    {
        return Bounds.Width * position / MaxPosition;
    }
    
    private double GetY(double value)
    {
        return Bounds.Height / 2 * (value / MaxValue) + Bounds.Height / 2;
    }
}