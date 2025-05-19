using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using MyDiplomaSolver;

namespace AvaloniaPixelDrawing;

public partial class PlaneCanvas : UserControl
{
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

        return segment.Coefficients.C switch
        {
            > 0 => Color.FromRgb(0, (byte)(255 - 127 * segment.Coefficients.C / 0.04), 0),
            < 0 => Color.FromRgb((byte)(255 + 127 * segment.Coefficients.C / 0.04), 0, 0),
            _ => Colors.Yellow
        };
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