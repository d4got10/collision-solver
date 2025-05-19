using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using MyDiplomaSolver;

namespace AvaloniaPixelDrawing;

public partial class MainWindow : Window
{
    public SimulationViewModel ViewModel { get; } = new();
    
    public MainWindow()
    {
        InitializeComponent();
        
        DataContext = ViewModel;
        
        var borderConditions = new BorderConditions(
        [
            new BorderConditionPoint(0.000 * 0.001,  0.0 * 0.001),
            new BorderConditionPoint(0.125 * 0.001, -5.0 * 0.001),
            new BorderConditionPoint(0.250 * 0.001, +5.0 * 0.001),
            new BorderConditionPoint(0.375 * 0.001, -5.0 * 0.001),
            new BorderConditionPoint(0.500 * 0.001, +5.0 * 0.001),
            new BorderConditionPoint(0.625 * 0.001, -5.0 * 0.001),
            new BorderConditionPoint(0.750 * 0.001, +5.0 * 0.001),
            new BorderConditionPoint(0.875 * 0.001, -5.0 * 0.001),
            new BorderConditionPoint(1.000 * 0.001,  0.0 * 0.001),
        ]);
        
        Update(borderConditions);
        BorderConditionsView.Updated += () => Update(BorderConditionsView.BorderConditions);
    }

    private void Update(BorderConditions borderConditions)
    {
        var simulationRunner = new SimulationRunner();
        var result = simulationRunner.Run(borderConditions);

        ViewModel.HadError = !result.Successful;
        
        var history = result.History;
        var lastTime = history[^1].Time * 1.05;
        var maxPosition = history[^1].Waves[0].GetPosition(lastTime);

        var maxValue = history.Max(x => x.Segments.Max(s => Math.Abs(s.Coefficients.C)));
        
        GraphView.Update(history[^1], maxPosition, maxValue, lastTime);
        PlaneView.Update(history, lastTime, maxPosition);
        BorderConditionsView.BorderConditions = borderConditions;
    }
}