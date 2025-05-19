using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MyDiplomaSolver;

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
    
    public double LastTime { get; set; }
    public double MaxPosition { get; set; }
    public double MaxValue { get; set; }
    public SimulationState[] History { get; set; } = [];
    
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