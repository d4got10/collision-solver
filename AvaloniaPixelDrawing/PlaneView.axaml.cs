using System;
using System.Linq;
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
                UpdateOnPress(position);
            }
        };
        PlaneCanvas.PointerMoved += (sender, e) =>
        {
            if (e.GetCurrentPoint(PlaneCanvas).Properties.IsLeftButtonPressed)
            {
                var position = e.GetPosition(PlaneCanvas);
                UpdateOnPress(position);
            }
        };
        PlaneCanvas.PointerReleased += (sender, e) =>
        {
        };
    }

    private void UpdateOnPress(Point position)
    {
        var x = Math.Clamp(position.X, 0, Bounds.Width);
        ViewModel.SelectedGraphTime = GetTimeFromX(x);
        UpdateInfo();
    }
    
    private void UpdateInfo()
    {
        var x = ViewModel.SelectedGraphTime / ViewModel.LastTime * PlaneCanvas.Bounds.Width;
        SelectedVerticalLine.StartPoint = new Point(x, 0);
        SelectedVerticalLine.EndPoint = new Point(x, PlaneCanvas.Bounds.Height);
        SelectedTime.Text = "t=" + ViewModel.SelectedGraphTime.ToString("F6");
        Canvas.SetLeft(SelectedTime, x + 3);
        
        var state = ViewModel.History.LastOrDefault(x => x.Time < ViewModel.SelectedGraphTime);
        WaveCount.Text = state != default ? state.Waves.Length.ToString() : "None";
    }

    public void Update(SimulationState[] history)
    {
        PlaneCanvas.ViewModel = ViewModel;
        PlaneCanvas.Update(history, ViewModel.LastTime, ViewModel.MaxPosition);
        UpdateInfo();
    }

    private double GetTimeFromX(double x)
    {
        return x / Bounds.Width * ViewModel.LastTime;
    }
}