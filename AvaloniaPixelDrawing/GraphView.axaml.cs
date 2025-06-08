using Avalonia;
using Avalonia.Controls;
using MyDiplomaSolver;

namespace AvaloniaPixelDrawing;

public partial class GraphView : UserControl
{
    public GraphView()
    {
        InitializeComponent();
    }

    public void Update(SimulationState state, double maxPosition, double maxValue, double time)
    {
        GraphControl.Update(state, maxPosition, maxValue, time);
        MaxValueText.Text = maxValue.ToString("F6");
        MinValueText.Text = (-maxValue).ToString("F6");
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        var y = e.NewSize.Height / 2;
        VerticalLine.StartPoint = new Point(0, y);
        VerticalLine.EndPoint = new Point(e.NewSize.Width, y);
        GraphControl.InvalidateVisual();
    }
}