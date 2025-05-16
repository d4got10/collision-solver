using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
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
}