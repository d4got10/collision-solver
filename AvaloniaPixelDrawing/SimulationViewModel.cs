using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AvaloniaPixelDrawing;

public class SimulationViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public bool HadError
    {
        get => _hadError;
        set => SetField(ref _hadError, value);
    }
    private bool _hadError = false;
    
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