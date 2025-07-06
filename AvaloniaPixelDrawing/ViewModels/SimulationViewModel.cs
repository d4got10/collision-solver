using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Solver;

namespace AvaloniaPixelDrawing;

public class SimulationViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public bool HadError
    {
        get => _hadError;
        set => SetField(ref _hadError, value);
    }

    public double SelectedGraphTime
    {
        get => _selectedGraphTime;
        set => SetField(ref _selectedGraphTime, value);
    }

    public Guid TaskId { get; set; }
    public string TaskName { get; set; } = "";
    public double SpeedA => Math.Sqrt((Lambda + 2 * Mu + 2 * V) / Ro);
    public double SpeedB => Math.Sqrt((Lambda + 2 * Mu - 2 * V) / Ro);
    
    public double Lambda { get; set; }
    public double Mu { get; set; }
    public double V { get; set; }
    public double Ro { get; set; }
    
    public double LastTime { get; set; }
    public double MaxPosition { get; set; }
    public double MaxValue { get; set; }
    public SimulationState[] History { get; set; } = [];
    public BorderConditions BorderConditions { get; set; }
    
    public double MaxC { get; set; }
    public double MinC { get; set; }
    
    private bool _hadError = false;
    private double _selectedGraphTime = 0;
    
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}