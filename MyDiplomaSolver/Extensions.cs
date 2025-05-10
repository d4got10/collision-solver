using System.Numerics;
using Raylib_cs;

namespace MyDiplomaSolver;

public static class Extensions
{
    public static bool ContainsPoint(this Rectangle rectangle, int x, int y)
    {
        return rectangle.X <= x 
               && x <= rectangle.X + rectangle.Width
               && rectangle.Y <= y 
               && y <= rectangle.Y + rectangle.Height;
    }
    
    public static bool ContainsPoint(this Rectangle rectangle, Vector2 position)
    {
        return ContainsPoint(rectangle, (int)position.X, (int)position.Y);
    }
}