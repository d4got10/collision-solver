using System;
using System.Collections.Generic;
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
    public event Action Updated = delegate { };
    
    private const int CircleCount = 10;
    private const double CircleRadius = 10;
    private Point[] _circleCenters = new Point[CircleCount];
    private int? _draggedCircleIndex = null;

    public double LastTime { get; private set; } = 1;
    public double MaxValue { get; private set; } = 1;
    
    private IEnumerable<Point> ScreenPoints => _circleCenters.Select(MapToScreen);
    
    public BorderConditions BorderConditions
    {
        get
        {
            var baseY = 0.5;
            var points = _circleCenters
                .Select(center =>
                {
                    var time = LastTime * center.X;
                    var value = MaxValue * (baseY - center.Y);
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

            LastTime = value.Points.Max(x => x.Time);
            MaxValue = value.Points.Max(x => Math.Abs(x.Value));
            
            _circleCenters = value.Points
                .Select(p => new Point(p.Time / LastTime, 0.5 - 0.5 * (p.Value / MaxValue)))
                .ToArray();
            
            InvalidateVisual();
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var position = e.GetPosition(this);
        var pointerPoint = e.GetCurrentPoint(this);

        var points = ScreenPoints.ToArray();
        if (pointerPoint.Properties.IsRightButtonPressed)
        {
            int? clickedCircleIndex = null;
            // Проверяем, был ли клик на каком-либо круге
            for (int i = 0; i < points.Length; i++)
            {
                if (IsPointInCircle(position, points[i]))
                {
                    clickedCircleIndex = i;
                    break;
                }
            }

            if (clickedCircleIndex.HasValue)
            {
                if (clickedCircleIndex > 0 && clickedCircleIndex < points.Length - 1)
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
                var newMappedPoint = MapToRelative(newPointPosition);
                // Добавляем новый круг по правому клику
                var circleCenters = _circleCenters.Where(p => p.X < newMappedPoint.X)
                    .Append(newMappedPoint)
                    .Concat(_circleCenters.Where(p => p.X > newMappedPoint.X))
                    .ToArray();
                _circleCenters = circleCenters;
            }
        }
        else
        {
            // Проверяем, был ли клик на каком-либо круге
            for (int i = 1; i < points.Length; i++)
            {
                if (IsPointInCircle(position, points[i]))
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
            
            var p = MapToRelative(new Point(0, boundedY));
            
            // Обновляем только Y-координату выбранного круга
            _circleCenters[_draggedCircleIndex.Value] = new Point(_circleCenters[_draggedCircleIndex.Value].X, p.Y);
            
            InvalidateVisual();
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _draggedCircleIndex = null;
        Updated();
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        InvalidateVisual();
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
        
        context.FillRectangle(Brushes.GhostWhite, new Rect(0, 0, Bounds.Width, Bounds.Height));

        var mappedCircleCenters = ScreenPoints.ToArray();
        
        // Рисуем соединительную линию
        var linePen = new ImmutablePen(Brushes.Black, 1);
        for (int i = 0; i < mappedCircleCenters.Length - 1; i++)
        {
            context.DrawLine(linePen, mappedCircleCenters[i], mappedCircleCenters[i + 1]);
        }

        // Рисуем круги
        var circleBrush = Brushes.Black;
        for (int i = 0; i < mappedCircleCenters.Length; i++)
        {
            context.DrawEllipse(circleBrush, linePen, mappedCircleCenters[i], CircleRadius, CircleRadius);
        }
    }

    private Point MapToScreen(Point relativePoint)
    {
        var width = Bounds.Width - 2 * CircleRadius;
        var height = Bounds.Height - 2 * CircleRadius;

        return new Point(relativePoint.X * width + CircleRadius, relativePoint.Y * height + CircleRadius);
    }

    private Point MapToRelative(Point screenPoint)
    {
        var width = Bounds.Width - 2 * CircleRadius;
        var height = Bounds.Height - 2 * CircleRadius;

        return new Point((screenPoint.X - CircleRadius) / width, (screenPoint.Y - CircleRadius) / height);
    }
}