using Raylib_cs;

namespace MyDiplomaSolver;

public class TextureRenderer(int width, int height, RenderTexture2D target)
{
    public TextureRenderer(int width, int height) : this(width, height, Raylib.LoadRenderTexture(width, height))
    {
    }
    
    public RenderTexture2D RenderGraph(SimulationState[] history, double time)
    {
        const int borderOffset = 20;
        
        var state = GetState(history, time);
        var furthestWave = state.Waves[0];
        var furthest = furthestWave.GetPosition(time);

        // var minE = Enumerable.Range(0, width).Min(x =>
        // {
        //     var cnt = state.Waves.Count(wave => wave.GetPosition(time) >= x);
        //     var segment = state.Segments[cnt];
        //
        //     return CalculateE(segment.Coefficients, time, x);
        // });
        //
        // var maxE = Enumerable.Range(0, width).Max(x =>
        // {
        //     var cnt = state.Waves.Count(wave => wave.GetPosition(time) >= x);
        //     var segment = state.Segments[cnt];
        //
        //     return CalculateE(segment.Coefficients, time, x);
        // });

        var minE = Math.Abs(Math.Min(state.Segments.Min(x => x.Coefficients.C), 0));
        var maxE = Math.Abs(Math.Max(state.Segments.Max(x => x.Coefficients.C), 0));

        (minE, maxE) = (-Math.Max(minE, maxE), Math.Max(minE, maxE));
        
        var eDifference = maxE - minE;
        
        Raylib.BeginTextureMode(target);
        Raylib.ClearBackground(Color.Black);
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Raylib.DrawPixel(x, y, Color.White);
            }
            
            for (int y = 0; y < height; y++)
            {
                Raylib.DrawPixel(x, y, GetColor(x, y));
            }

            var cnt = 3;
            for (int y = height - 1; y >= 0; y--)
            {
                var color = GetColor(x, y);
                if (color.R == Color.DarkGray.R)
                {
                    Raylib.DrawPixel(x, y, Color.Black);
                    cnt--;
                    if (cnt == 0)
                    {
                        break;
                    }
                }
            }
        }

        var zeroOffset = (Math.Abs(minE) / eDifference);

        var zeroLineY = (int)((height - 2 * borderOffset) * zeroOffset + borderOffset);
        var bottomY = borderOffset;
        var topY = height - borderOffset;
        
        Raylib.DrawLine(0, zeroLineY, width, zeroLineY, Color.Black);
        
        Raylib.DrawText($"{maxE:0.0000}", 3, bottomY, 24, Color.White);
        Raylib.DrawText($"{minE:0.0000}", 3, topY, 24, Color.White);
        Raylib.DrawText($"0", 3, (topY + bottomY) / 2, 24, Color.White);
        Raylib.DrawText($"C(x, t)", width - 120, bottomY, 36, Color.White);
        
        Raylib.EndTextureMode();
                
        return target;
        
        Color GetColor(int x, int y)
        {
            var offset = x / (double)width * furthest;
            var graphE = (y - borderOffset) / ((double)height - 2 * borderOffset) * eDifference + minE;

            var cnt = state.Waves.Count(x => x.GetPosition(time) >= offset);
            var segment = state.Segments[cnt];
            
            var e = CalculateE(segment.Coefficients, time, offset);
            
            if (e <= graphE)
            {
                return Color.DarkGray;
            }
            
            return Color.LightGray;
        }

        double CalculateE(Coefficients coefficients, double t, double x)
        {
            var a = coefficients.A;
            var b = coefficients.B;
            var c = coefficients.C;
            return c;
            return a + b * t + c * x;
        }
    }
    
    public RenderTexture2D RenderPlane(SimulationState[] history, double extendTimeCoefficient)
    {
        var lastTime = history[^1].Time * extendTimeCoefficient;

        var lastIteration = history[^1];
        var furthestWave = lastIteration.Waves[0];
        var furthest = furthestWave.GetPosition(lastTime);
        
        Raylib.BeginTextureMode(target);
        Raylib.ClearBackground(Color.Black);
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var trueY = height - y - 1;
                var color = GetColor(x, trueY);
                Raylib.DrawPixel(x, y, color);
            }
        }
        
        Raylib.EndTextureMode();
                
        return target;
        
        Color GetColor(int x, int y)
        {
            var time = x / (double)width * lastTime;
            var position = y / (double)height * furthest;
            var state = GetState(history, time);

            var cnt = state.Waves.Count(x => x.GetPosition(time) >= position);
            var nextCnt = state.Waves.Count(x => x.GetPosition(time) >= position + furthest / height);
            if (cnt != nextCnt)
            {
                return Color.Black;
            }
            
            var segment = state.Segments[cnt];

            if (cnt == 0)
            {
                return Color.White;
            }

            return segment.Coefficients.C switch
            {
                > 0 => new Color((int)(255 - 127 * segment.Coefficients.C / 0.03), 0, 0),
                < 0 => new Color(0, (int)(255 + 127 * segment.Coefficients.C / 0.03), 0),
                _ => Color.Yellow
            };
        }
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