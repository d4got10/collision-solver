using System;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using MyDiplomaSolver;

namespace AvaloniaPixelDrawing;

public partial class SimulationView : UserControl
{
    public SimulationViewModel ViewModel => (SimulationViewModel)DataContext!;
    
    public SimulationView()
    {
        InitializeComponent();
        
        BorderConditionsView.BorderConditionsControl.Updated += () =>
        {
            ViewModel.BorderConditions = BorderConditionsView.BorderConditions;
            Update();
        };
    }

    public void Update()
    {
        var simulationRunner = new SimulationRunner();
        var result = simulationRunner.Run(ViewModel.BorderConditions, ViewModel.SpeedA, ViewModel.SpeedB);

        ViewModel.HadError = !result.Successful;
        
        var history = result.History;
        var lastTime = history[^1].Time;
        var maxPosition = history[^1].Waves[0].GetPosition(lastTime);
        var maxValue = history.Max(x => x.Segments.Max(s => Math.Abs(s.Coefficients.C)));

        ViewModel.History = history;
        ViewModel.LastTime = lastTime;
        ViewModel.MaxValue = maxValue;
        ViewModel.MaxPosition = maxPosition;
        if (ViewModel.SelectedGraphTime == 0)
        {
            ViewModel.SelectedGraphTime = lastTime / 2;
        }

        ViewModel.PropertyChanged += ViewModelOnPropertyChanged;

        UpdateGraph();
        
        PlaneView.Update(history);
        BorderConditionsView.BorderConditions = ViewModel.BorderConditions;
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