using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Solver.Extensions;
using Solver.Models;
using UI.ViewModels;

namespace UI.Views;

public partial class PlaneCanvas : UserControl
{
    public SimulationViewModel ViewModel { get; set; }
    
    public const int ResolutionWidth = 1920;
    public const int ResolutionHeight = 1080;
    
    public PlaneCanvas()
    {
        InitializeComponent();
    }

    private SimulationState[] _history = [];
    private double _lastTime = 0;
    private double _maxPosition = 0;

    public void Update(SimulationState[] history, double lastTime, double maxPosition)
    {
        _history = history;
        _lastTime = lastTime;
        _maxPosition = maxPosition;
        DrawPixels(new Rect(0, 0, ResolutionWidth, ResolutionHeight));
    }
    
    private void DrawPixels(Rect bounds)
    {
        var width = (int)bounds.Width;
        var height = (int)bounds.Height;

        if (width == 0 || height == 0)
        {
            return;
        }
        
        // Создаём WriteableBitmap
        var writeableBitmap = new WriteableBitmap(
            new PixelSize(width, height),
            new Vector(96, 96),
            PixelFormat.Bgra8888, // Формат пикселей (8 бит на канал: B, G, R, A)
            AlphaFormat.Premul);

        using (var buffer = writeableBitmap.Lock())
        {
            unsafe
            {
                // Получаем указатель на пиксельные данные
                byte* ptr = (byte*)buffer.Address;

                Parallel.For(0, height, y =>
                {
                    for (int x = 0; x < width; x++)
                    {
                        // Получаем цвет для пикселя (x, y)
                        var color = GetColor(x, height - y, width, height);

                        // Вычисляем позицию в буфере (Bgra8888 = 4 байта на пиксель)
                        int offset = y * buffer.RowBytes + x * 4;

                        // Записываем цвет (порядок BGRA)
                        ptr[offset] = color.B;     // Blue
                        ptr[offset + 1] = color.G; // Green
                        ptr[offset + 2] = color.R; // Red
                        ptr[offset + 3] = color.A; // Alpha
                    }
                });
            }
        }

        // Отображаем изображение
        ImageControl.Source = writeableBitmap;
    }

    // Функция, определяющая цвет пикселя
    private Color GetColor(int x, int y, int width, int height)
    {
        var time = x / (double)width * _lastTime;
        var position = y / (double)height * _maxPosition;
        var state = GetState(_history, time);

        var cnt = state.Waves.Count(x => x.GetPosition(time) >= position);
        var nextCnt = state.Waves.Count(x => x.GetPosition(time) >= position + _maxPosition / height);
        if (cnt != nextCnt)
        {
            return Colors.Black;
        }
            
        var segment = state.Segments[cnt];

        if (cnt == 0)
        {
            return Colors.White;
        }

        if (segment.Coefficients.C == 0)
        {
            return Colors.Yellow;
        }
        
        return GetColorFromGradient(-ViewModel.MaxC, -ViewModel.MinC, -segment.Coefficients.C);
        
        return segment.Coefficients.C switch
        {
            > 0 => Color.FromRgb(0, (byte)(255 - 127 * segment.Coefficients.C / 0.04), 0),
            < 0 => Color.FromRgb((byte)(255 + 127 * segment.Coefficients.C / 0.04), 0, 0),
            _ => Colors.Yellow
        };
    }
    
    /// <summary>
    /// Вычисляет цвет на основе сложного, многосегментного градиента.
    /// </summary>
    /// <param name="min">Минимальное значение шкалы.</param>
    /// <param name="max">Максимальное значение шкалы.</param>
    /// <param name="t">Текущее значение, для которого нужно найти цвет.</param>
    /// <returns>Объект Avalonia.Media.Color.</returns>
    public static Color GetColorFromGradient(double min, double max, double t)
    {
        // Предотвращение деления на ноль, возврат начального цвета.
        if (min >= max)
        {
            return Colors.Green;
        }

        // 1. Нормализуем значение t в диапазон [0, 1], получая p.
        double clampedT = Math.Clamp(t, min, max);
        double p = (clampedT - min) / (max - min);

        // 2. Определяем сегмент и выполняем линейную интерполяцию.
        // Используем константы для смещений, чтобы избежать магических чисел.
        const double greenOffset = 0.0;
        const double limeOffset = 0.4999;
        const double yellowOffset = 0.5;
        const double redOffset = 0.5001;
        const double brownOffset = 1.0;

        Color startColor, endColor;
        double segmentStart, segmentEnd;

        if (p <= yellowOffset) 
        {
            // Первая половина градиента: Green -> Lime -> (Yellow)
            if (p <= limeOffset)
            {
                // Сегмент: Green -> Lime
                startColor = Colors.Green;
                endColor = Colors.Lime;
                segmentStart = greenOffset;
                segmentEnd = limeOffset;
            }
            else // p > limeOffset && p <= yellowOffset
            {
                // Сегмент: Lime -> Yellow
                startColor = Colors.Lime;
                endColor = Colors.Yellow;
                segmentStart = limeOffset;
                segmentEnd = yellowOffset;
            }
        }
        else 
        {
            // Вторая половина градиента: (Red) -> Brown
            if (p >= redOffset)
            {
                // Сегмент: Red -> Brown
                startColor = Colors.Red;
                endColor = Colors.Brown;
                segmentStart = redOffset;
                segmentEnd = brownOffset;
            }
            else // p > yellowOffset && p < redOffset
            {
                // Сегмент: Yellow -> Red
                startColor = Colors.Yellow;
                endColor = Colors.Red;
                segmentStart = yellowOffset;
                segmentEnd = redOffset;
            }
        }
        
        // 3. Пере-нормализуем p для конкретного сегмента
        double segmentLength = segmentEnd - segmentStart;
        // Защита от деления на ноль для точечных сегментов
        double subP = (segmentLength > 0) ? ((p - segmentStart) / segmentLength) : 0;

        // 4. Вычисляем итоговый цвет
        byte r = (byte)((1 - subP) * startColor.R + subP * endColor.R);
        byte g = (byte)((1 - subP) * startColor.G + subP * endColor.G);
        byte b = (byte)((1 - subP) * startColor.B + subP * endColor.B);

        return Color.FromRgb(r, g, b);
    }
    
    private SimulationState GetState(SimulationState[] history, double time)
    {
        if (time < history[0].Time)
        {
            return history[0];
        }
            
        return history.Last(x => x.Time <= time);
    }
}