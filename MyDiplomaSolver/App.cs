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
        
        var history = RunSimulation(borderConditions);
        
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

            Raylib.EndDrawing();
        }

        // Очистка ресурсов
        Raylib.UnloadRenderTexture(planeTexture);
        Raylib.UnloadRenderTexture(graphTexture);
        Raylib.UnloadRenderTexture(borderConditionsTexture);
        Raylib.CloseWindow();
    }

    private SimulationState[] RunSimulation(BorderConditions borderConditions)
    {
        double a = 3702.77;
        double b = 2378.63;

        var initialState = new SimulationState([], [ default ], -double.Epsilon);

        var simulation = new Simulation(initialState, borderConditions, a, b);

        List<SimulationState> history = new();

        var iterationNumber = 0;
        bool iterationSuccessful;
        Console.WriteLine($"Iteration [{iterationNumber}]:");
        LogState(simulation.State);
        do
        {
            iterationNumber++;
            Console.WriteLine($"Iteration [{iterationNumber}]:");
            iterationSuccessful = simulation.Iterate();
            if(iterationSuccessful)
                history.Add(simulation.State);
            LogState(simulation.State);
        } while (iterationSuccessful && iterationNumber < 100);

        return history.ToArray();
    }
    
    private void LogState(SimulationState state)
    {
        foreach (var wave in state.Waves)
        {
            Console.WriteLine($"Wave[{wave.Id}] at {wave.GetPosition(state.Time) * 1000:0.00} with velocity {wave.Velocity:0.0000000}");
        }

        for (var i = 0; i < state.Segments.Length; i++)
        {
            var segment = state.Segments[i];
            Console.WriteLine($"Segment [{i}]: A={segment.Coefficients.A} B={segment.Coefficients.B} C={segment.Coefficients.C}");
        }
        Console.WriteLine();
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