using System;
using System.ComponentModel;
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

        ViewModel.History = history;
        ViewModel.LastTime = lastTime;
        ViewModel.MaxValue = maxValue;
        ViewModel.MaxPosition = maxPosition;
        ViewModel.SelectedGraphTime = lastTime / 2;
        
        ViewModel.PropertyChanged += ViewModelOnPropertyChanged;

        UpdateGraph();
        
        PlaneView.Update(history, lastTime, maxPosition);
        BorderConditionsView.BorderConditions = borderConditions;
    }

    private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.SelectedGraphTime))
        {
            UpdateGraph();
        }
    }
    private void UpdateGraph()
    {
        var state = GetState(ViewModel.SelectedGraphTime);

        GraphView.Update(state, ViewModel.MaxPosition, ViewModel.MaxValue, ViewModel.SelectedGraphTime);
    }

    private SimulationState GetState(double time)
    {
        var state = ViewModel.History.LastOrDefault(x => x.Time < time);
        if (state == default)
        {
            return ViewModel.History[^1];
        }
        return state;
    } 
}