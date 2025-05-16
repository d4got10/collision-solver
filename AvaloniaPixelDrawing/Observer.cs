using System;

namespace AvaloniaPixelDrawing;

public class Observer<T>(Action<T> listener) : IObserver<T>
{
    public void OnCompleted() { }
    public void OnError(Exception error) { }
    public void OnNext(T value) => listener(value);
}