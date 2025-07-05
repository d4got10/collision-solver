using Avalonia;
using Avalonia.Controls;

namespace AvaloniaPixelDrawing;

public partial class GradientScaleControl : UserControl
{
    // Определяем свойство зависимости для начального значения
    public static readonly StyledProperty<double> StartValueProperty =
        AvaloniaProperty.Register<GradientScaleControl, double>(
            nameof(StartValue),
            defaultValue: -100.0);

    // CLR-свойство для удобного доступа из кода и XAML
    public string StartValue
    {
        get => GetValue(StartValueProperty).ToString();
        set => SetValue(StartValueProperty, double.Parse(value));
    }

    // Определяем свойство зависимости для конечного значения
    public static readonly StyledProperty<double> EndValueProperty =
        AvaloniaProperty.Register<GradientScaleControl, double>(
            nameof(EndValue), 
            defaultValue: 100.0);

    // CLR-свойство
    public string EndValue
    {
        get => GetValue(EndValueProperty).ToString();
        set => SetValue(EndValueProperty, double.Parse(value));
    }

    public GradientScaleControl()
    {
        DataContext = this;
        InitializeComponent();
    }
}