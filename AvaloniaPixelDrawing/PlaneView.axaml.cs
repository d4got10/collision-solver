using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using MyDiplomaSolver;

namespace AvaloniaPixelDrawing;

public partial class PlaneView : UserControl
{
    public SimulationViewModel ViewModel => (SimulationViewModel)DataContext!;
    
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
        PlaneCanvas.PointerPressed += (sender, e) =>
        {
            if (e.GetCurrentPoint(PlaneCanvas).Properties.IsLeftButtonPressed)
            {
                var position = e.GetPosition(PlaneCanvas);
                var x = Math.Clamp(position.X, 0, Bounds.Width);
                
                SelectedVerticalLine.StartPoint = new Point(x, 0);
                SelectedVerticalLine.EndPoint = new Point(x, PlaneCanvas.Bounds.Height);
                ViewModel.SelectedGraphTime = GetTimeFromX(x);
            }
        };
        PlaneCanvas.PointerMoved += (sender, e) =>
        {
            if (e.GetCurrentPoint(PlaneCanvas).Properties.IsLeftButtonPressed)
            {
                var position = e.GetPosition(PlaneCanvas);
                var x = Math.Clamp(position.X, 0, Bounds.Width);
                
                SelectedVerticalLine.StartPoint = new Point(x, 0);
                SelectedVerticalLine.EndPoint = new Point(x, PlaneCanvas.Bounds.Height);
                ViewModel.SelectedGraphTime = GetTimeFromX(x);
            }
        };
        PlaneCanvas.PointerReleased += (sender, e) =>
        {
        };
    }

    public void Update(SimulationState[] history, double lastTime, double maxPosition)
    {
        PlaneCanvas.Update(history, lastTime, maxPosition);
    }

    private double GetTimeFromX(double x)
    {
        return x / Bounds.Width * ViewModel.LastTime;
    }
}