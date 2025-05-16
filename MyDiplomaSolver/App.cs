using System.Numerics;
using Raylib_cs;

namespace MyDiplomaSolver;

public class App
{
    public void Run()
    {
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
        
        var simulationRunner = new SimulationRunner();
        var result = simulationRunner.Run(borderConditions);
        var history = result.History;
        var hadErrors = !result.Successful;
        var errorTime = result.ErrorTime ?? 0;
        
        var width = 1280;
        var height = 920;
        
        Raylib.InitWindow(width, height, "Raylib Pixel Drawing");
        List<IWidget> widgets = [];

        var timeExtensionCoefficient = 1.05;
        var planeWidth = width;
        var planeHeight = 3 * height / 4;
        var planeRenderer = new TextureRenderer(planeWidth, planeHeight);
        var planeTexture = planeRenderer.RenderPlane(history, timeExtensionCoefficient);
        var planeWidget = new RenderTextureWidget(planeTexture, 0, height / 4);
        widgets.Add(planeWidget);

        var graphWidth = width / 2;
        var graphHeight = height / 4;
        var graphRenderer = new TextureRenderer(graphWidth, graphHeight);
        var graphTexture = graphRenderer.RenderGraph(history, history[^1].Time);
        var graphWidget = new RenderTextureWidget(graphTexture, 0, 0);
        widgets.Add(graphWidget);
        
        var borderConditionsWidth = width / 2;
        var borderConditionsHeight = height / 4;
        var borderConditionsRenderer = new TextureRenderer(borderConditionsWidth, borderConditionsHeight);
        var borderConditionsTexture = borderConditionsRenderer.RenderBorderConditions(borderConditions);
        var borderConditionsWidget = new RenderTextureWidget(borderConditionsTexture, width / 2, 0);
        widgets.Add(borderConditionsWidget);

        int? selectedX = null;
        int? draggingIndex = null;

        while (!Raylib.WindowShouldClose())
        {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Gray);

            var mousePosition = Raylib.GetMousePosition();
            
            foreach (var widget in widgets)
            {
                Raylib.DrawTextureRec(widget.Texture,  new Rectangle(0, 0, widget.Texture.Width, -widget.Texture.Height), new Vector2(widget.X, widget.Y), Color.White);
            }

            var fromY = (int)(planeWidget.BoundingBox.Y);
            var toY = (int)(fromY + planeWidget.BoundingBox.Height);
            
            if (selectedX is not null)
            {
                Raylib.DrawLine(selectedX.Value, fromY, selectedX.Value, toY, Color.Black);
                var relativeX = (selectedX - planeWidget.BoundingBox.X) / planeWidget.BoundingBox.Width;
                var lastTime = history[^1].Time * timeExtensionCoefficient;
                var time = lastTime * relativeX;
                
                Raylib.DrawText($"T={time*1000:0.000}", selectedX.Value, fromY, 24, Color.Black);
            }
            
            if (planeWidget.BoundingBox.ContainsPoint(mousePosition))
            {
                var x = (int)mousePosition.X;
                
                Raylib.DrawLine(x, fromY, x, toY, Color.Yellow);

                if (Raylib.IsMouseButtonDown(MouseButton.Left))
                {
                    selectedX = x;
                    var relativeX = (mousePosition.X - planeWidget.BoundingBox.X) / planeWidget.BoundingBox.Width;
                    var lastTime = history[^1].Time * timeExtensionCoefficient;
                    var time = lastTime * relativeX;
                    
                    graphRenderer.RenderGraph(history, time);
                }
            }
            
            if (borderConditionsWidget.BoundingBox.ContainsPoint(mousePosition))
            {
                const int borderOffset = 20;
                const double multiplier = 0.001;

                const double maxValue = 5 * multiplier;
                const double minValue = -maxValue;

                var lastTime = borderConditions.Points.Last().Time * 1.1;
                
                var x = (int)(mousePosition.X - borderConditionsWidget.X);
                var y = (int)(mousePosition.Y - borderConditionsWidget.Y);
                
                Raylib.DrawLine(x, fromY, x, toY, Color.Yellow);

                if (Raylib.IsMouseButtonPressed(MouseButton.Left) && draggingIndex is null)
                {
                    draggingIndex = borderConditions.Points
                        .Select((b, i) =>
                        {
                            var p = GetPosition(b.Time);
                            return (PointIndex: i, Distance: Math.Abs(p - x));
                        })
                        .MinBy(p => p.Distance)
                        .PointIndex;
                }

                if (Raylib.IsMouseButtonDown(MouseButton.Left) && draggingIndex is not null)
                {
                    var newValue = GetValue(y);
                    borderConditions.Points[draggingIndex.Value] = borderConditions.Points[draggingIndex.Value] with
                    {
                        Value = newValue
                    };

                    borderConditionsRenderer.RenderBorderConditions(borderConditions);
                }

                int GetPosition(double t)
                {
                    var width = borderConditionsWidget.BoundingBox.Width - 2 * borderOffset;
                    var xOffset = borderOffset + (int)(width * t / lastTime);
                    return xOffset;
                }

                double GetValue(int yValue)
                {
                    var height = borderConditionsWidget.BoundingBox.Height - 2 * borderOffset;
                    var value = (-1) * (yValue - borderOffset - height / 2) / (height / 2) * maxValue;
                    return Math.Clamp(value, minValue, maxValue);
                }
            }

            if (hadErrors)
            {
                var lastTime = history[^1].Time * timeExtensionCoefficient;
                var x = (int)(planeWidget.BoundingBox.Width * (errorTime / lastTime));
                
                Raylib.DrawText("ERROR", planeWidget.X, planeWidget.Y + 24, 24, Color.Red);
                Raylib.DrawLine(x, planeWidget.Y, x, (int)(planeWidget.Y + planeWidget.BoundingBox.Height), Color.Red);
            }
            
            if (Raylib.IsMouseButtonReleased(MouseButton.Left))
            {
                if (draggingIndex is not null)
                {
                    draggingIndex = null;
                    Raylib.DrawText("Simulating...", planeWidget.X, planeWidget.Y, 24, Color.Black);
                    Raylib.EndDrawing();
                    
                    result = simulationRunner.Run(borderConditions);
                    history = result.History;
                    hadErrors = !result.Successful;
                    errorTime = result.ErrorTime ?? 0;
                    
                    planeRenderer.RenderPlane(history, timeExtensionCoefficient);

                    var time = 0.0;
                    if (selectedX is not null)
                    {
                        var relativeX = (selectedX.Value - planeWidget.BoundingBox.X) / planeWidget.BoundingBox.Width;
                        var lastTime = history[^1].Time * timeExtensionCoefficient;
                        time = lastTime * relativeX;
                    }
                    
                    graphRenderer.RenderGraph(history, time);
                }
                else
                {
                    Raylib.EndDrawing();
                }
            }
            else
            {
                Raylib.EndDrawing();
            }
        }

        // Очистка ресурсов
        Raylib.UnloadRenderTexture(planeTexture);
        Raylib.UnloadRenderTexture(graphTexture);
        Raylib.UnloadRenderTexture(borderConditionsTexture);
        Raylib.CloseWindow();
    }
}

public interface IWidget
{
    int X { get; }
    int Y { get; }
    Texture2D Texture { get; }
}

public readonly record struct RenderTextureWidget(
    RenderTexture2D RenderTexture, 
    int X, 
    int Y) : IWidget
{
    public Texture2D Texture => RenderTexture.Texture;
    public Rectangle BoundingBox => new(X, Y, Texture.Width, Texture.Height);
}