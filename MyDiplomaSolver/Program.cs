using System.Drawing;
using System.Drawing.Imaging;
using MyDiplomaSolver;

double a = 3702.77;
double b = 2378.63;

var initialState = new SimulationState([], [ default ], -double.Epsilon);
var borderConditions = new BorderCondition(
[
    new BorderConditionPoint(0.0 * 0.001,  0.0 * 0.001),
    new BorderConditionPoint(0.3 * 0.001, -4.5 * 0.001),
    new BorderConditionPoint(0.5 * 0.001, -5.0 * 0.001),
    new BorderConditionPoint(0.75 * 0.001, -1.5 * 0.001),
    new BorderConditionPoint(0.8 * 0.001,  0.0 * 0.001),
]);

var simulation = new Simulation(initialState, borderConditions, a, b);

List<SimulationState> history = new();

var iterationNumber = 0;
bool iterationSuccessful;
Console.WriteLine($"Iteration [{iterationNumber}]:");
PrintState(simulation.State, iterationNumber);
do
{
    iterationNumber++;
    Console.WriteLine($"Iteration [{iterationNumber}]:");
    iterationSuccessful = simulation.Iterate();
    if(iterationSuccessful)
        history.Add(simulation.State);
    PrintState(simulation.State, iterationNumber);
} while (iterationSuccessful && iterationNumber < 100);

GenerateImage(history);

// var lastTime = history[^1].Time;
// Console.WriteLine($"History recap of {history.Count - 1} iterations [0.000ms .. {lastTime*1000:0.000}ms]:");
// Console.WriteLine($"Last time: {(lastTime + 0.05) * 1000}");
// for (var i = 0; i < history.Count; i++)
// {
//     Console.WriteLine($"\"from\": {history[i].Time}");
//     var state = history[i];
// }

void GenerateImage(List<SimulationState> history)
{
    var width = 1920;
    var height = 1080;
    var startTime = history[0].Time;
    var lastTime = history[^1].Time * 1.05;

    var lastIteration = history[^1];
    var furthestWave = lastIteration.Waves[0];
    var furthest = furthestWave.Velocity * (lastTime - furthestWave.StartTime) + furthestWave.StartPosition;

    var colorsCount = history.Max(x => x.Waves.Length) + 1; 
    var colors = Enumerable
        .Range(0, colorsCount)
        .Select(x =>
        {
            var val = 127 + 128 * (x + 1) / colorsCount;
            return Color.FromArgb(val, val, val);
        })
        .Reverse()
        .ToArray();
    
    using Bitmap bitmap = new Bitmap(width, height);
    for (int x = 0; x < width; x++)
    {
        for (int y = 0; y < height; y++)
        {
            bitmap.SetPixel(x, y, GetColor(x, y));
        }
    }
            
    // Сохраняем изображение
    bitmap.Save("output.png", ImageFormat.Png);
            
    Console.WriteLine("Изображение успешно сохранено как 'output.png'");
    
    Color GetColor(int x, int y)
    {
        y = height - y;
        
        var time = x / (double)width * lastTime;
        var position = y / (double)height * furthest;
        var state = GetState(time);

        var cnt = state.Waves.Count(x => x.GetPosition(time) >= position);
        var segment = state.Segments[cnt];

        if (segment.Coefficients.C < 0)
        {
            return Color.FromArgb(255 - 30 * cnt, 0, 0);
        }
        else if (segment.Coefficients.C > 0)
        {
            return Color.FromArgb(0, 255 - 30 * cnt, 0);
        }
        else
        {
            return Color.White;
        }
        
        return colors[cnt];
    }

    SimulationState GetState(double time)
    {
        if (time < history[0].Time)
        {
            return history[0];
        }
        
        return history.Last(x => x.Time <= time);
    }
}

void PrintState(SimulationState state, int iterationNumber)
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