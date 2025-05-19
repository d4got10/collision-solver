using Avalonia;
using Avalonia.Controls;
using MyDiplomaSolver;

namespace AvaloniaPixelDrawing;

public partial class PlaneView : UserControl
{
    public PlaneView()
    {
        InitializeComponent();
        
        PlaneCanvas.PointerEntered += (sender, e) =>
        {
            VerticalLine.IsVisible = true;
        };
        PlaneCanvas.PointerMoved += (sender, e) => {
            var position = e.GetPosition(PlaneCanvas);
            VerticalLine.StartPoint = new Point(position.X, 0);
            VerticalLine.EndPoint = new Point(position.X, PlaneCanvas.Bounds.Height);
        };
        PlaneCanvas.PointerExited += (sender, e) => {
            VerticalLine.IsVisible = false;
        };
    }

    public void Update(SimulationState[] history, double lastTime, double maxPosition)
    {
        PlaneCanvas.Update(history, lastTime, maxPosition);
    }
}