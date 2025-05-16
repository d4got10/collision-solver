using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using System.Linq;
using MyDiplomaSolver;

namespace AvaloniaPixelDrawing;

public class ConnectedCirclesControl : Control
{
    private const int CircleCount = 10;
    private const double CircleRadius = 10;
    private Point[] _circleCenters = new Point[CircleCount];
    private int? _draggedCircleIndex = null;

    public double LastTime { get; private set; } = 1;
    public double MaxValue { get; private set; } = 1;
    
    public BorderConditions BorderConditions
    {
        get
        {
            var baseY = Bounds.Height / 2;
            var width = Bounds.Width - 2 * CircleRadius;
            var height = Bounds.Height / 2  - CircleRadius;
            var points = _circleCenters
                .Select(center =>
                {
                    var time = LastTime * (center.X / width);
                    var value = MaxValue * ((baseY - center.Y) / height);
                    return new BorderConditionPoint(time, value);
                })
                .ToArray();
            return new BorderConditions(points);
        }
        set
        {
            if (value.Points[0].Time != 0 && value.Points[0].Value != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Начальная точка граничного условия должна быть (0, 0)");
            }

            if (value.Points.Length < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Граничное условие должно содержать минимум 2 точки");
            }
            
            var width = Bounds.Width - 2 * CircleRadius;
            var height = Bounds.Height / 2 - CircleRadius;
            var baseY = Bounds.Height / 2;

            LastTime = value.Points.Max(x => x.Time);
            MaxValue = value.Points.Max(x => Math.Abs(x.Value));
            
            _circleCenters = value.Points
                .Select(p => new Point(width * (p.Time / LastTime), baseY - height * (p.Value / MaxValue)))
                .ToArray();
        }
    }
    
    public ConnectedCirclesControl()
    {
        // Инициализируем позиции кругов
        this.GetObservable(BoundsProperty)
            .Subscribe(new Observer<Rect>(_ => InitializeCirclePositions()));
    }

    private void InitializeCirclePositions()
    {
        if (Bounds.Width == 0)
        {
            return;
        }

        _circleCenters[0] = new Point(CircleRadius, Bounds.Height / 2);
        _circleCenters[^1] = new Point(Bounds.Width - CircleRadius, Bounds.Height / 2);
        
        for (int i = 1; i < CircleCount - 1; i++)
        {
            _circleCenters[i] = new Point((i + 1) * Bounds.Width / (CircleCount + 1), Bounds.Height / 2);
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var position = e.GetPosition(this);
        var pointerPoint = e.GetCurrentPoint(this);

        if (pointerPoint.Properties.IsRightButtonPressed)
        {
            int? clickedCircleIndex = null;
            // Проверяем, был ли клик на каком-либо круге
            for (int i = 0; i < _circleCenters.Length; i++)
            {
                if (IsPointInCircle(position, _circleCenters[i]))
                {
                    clickedCircleIndex = i;
                    break;
                }
            }

            if (clickedCircleIndex.HasValue)
            {
                if (clickedCircleIndex > 0 && clickedCircleIndex < _circleCenters.Length - 1)
                {
                    var circleCenters = _circleCenters.Take(clickedCircleIndex.Value)
                        .Concat(_circleCenters.Skip(clickedCircleIndex.Value + 1))
                        .ToArray();
                    _circleCenters = circleCenters;
                }
            }
            else
            {
                var newPointPosition = new Point(
                    Math.Clamp(position.X, CircleRadius, Bounds.Width - CircleRadius),
                    Math.Clamp(position.Y, CircleRadius, Bounds.Height - CircleRadius));
                // Добавляем новый круг по правому клику
                var circleCenters = _circleCenters.Where(p => p.X < position.X)
                    .Append(newPointPosition)
                    .Concat(_circleCenters.Where(p => p.X > position.X))
                    .ToArray();
                _circleCenters = circleCenters;
            }
        }
        else
        {
            // Проверяем, был ли клик на каком-либо круге
            for (int i = 1; i < _circleCenters.Length; i++)
            {
                if (IsPointInCircle(position, _circleCenters[i]))
                {
                    _draggedCircleIndex = i;
                    break;
                }
            }
        }
        
        InvalidateVisual();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        
        if (_draggedCircleIndex.HasValue && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            var position = e.GetPosition(this);
            
            // Ограничиваем позицию по Y, чтобы круг не выходил за границы
            double boundedY = position.Y;
        
            // Верхняя граница (с учетом радиуса)
            boundedY = Math.Max(CircleRadius, boundedY);
        
            // Нижняя граница (с учетом радиуса)
            boundedY = Math.Min(Bounds.Height - CircleRadius, boundedY);
            
            // Обновляем только Y-координату выбранного круга
            _circleCenters[_draggedCircleIndex.Value] = new Point(
                _circleCenters[_draggedCircleIndex.Value].X,
                boundedY);
            
            InvalidateVisual();
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _draggedCircleIndex = null;
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        UpdateCirclePositions();
        InvalidateVisual();
    }

    private void UpdateCirclePositions()
    {
        if (Bounds.Width <= 0 || Bounds.Height <= 0)
            return;

        for (int i = 0; i < _circleCenters.Length; i++)
        {
            double x = _circleCenters[i].X;
            double y = _circleCenters[i].Y;
            
            _circleCenters[i] = new Point(x, y);
        }
    }

    private bool IsPointInCircle(Point point, Point circleCenter)
    {
        var diff = point - circleCenter;
        return diff.X * diff.X + diff.Y * diff.Y  <= CircleRadius * CircleRadius;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        
        if (Bounds.Width <= 0 || Bounds.Height <= 0)
            return;

        UpdateCirclePositions();
        
        context.FillRectangle(Brushes.GhostWhite, new Rect(0, 0, Bounds.Width, Bounds.Height));

        // Рисуем соединительную линию
        var linePen = new ImmutablePen(Brushes.Black, 1);
        for (int i = 0; i < _circleCenters.Length - 1; i++)
        {
            context.DrawLine(linePen, _circleCenters[i], _circleCenters[i + 1]);
        }

        // Рисуем круги
        var circleBrush = Brushes.Black;
        for (int i = 0; i < _circleCenters.Length; i++)
        {
            context.DrawEllipse(circleBrush, linePen, _circleCenters[i], CircleRadius, CircleRadius);
        }
    }
}