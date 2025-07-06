using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
// Для Debug.WriteLine

namespace AvaloniaPixelDrawing; // Замените на ваш namespace

public partial class InteractiveTextList : UserControl
{
    public static readonly StyledProperty<ObservableCollection<TaskInfo>> ItemsSourceProperty =
        AvaloniaProperty.Register<InteractiveTextList, ObservableCollection<TaskInfo>>(
            nameof(ItemsSource),
            defaultValue: new ObservableCollection<TaskInfo>()
        );

    public ObservableCollection<TaskInfo> ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public event Action<TaskInfo>? OnCardClick = null;

    public InteractiveTextList()
    {
        InitializeComponent();
        Debug.WriteLine($"[InteractiveTextList Constructor] ItemsSource is null: {ItemsSource == null}");
        if (ItemsSource != null)
        {
            Debug.WriteLine($"[InteractiveTextList Constructor] ItemsSource count: {ItemsSource.Count}");
        }
        // Если вы присваиваете ItemsSource извне (например, в MainWindow.xaml.cs),
        // убедитесь, что это происходит ПОСЛЕ InitializeComponent() родительского окна.
    }

    private void ListItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { CommandParameter: TaskInfo itemValue })
        {
            Console.WriteLine($"Клик по карточке: {itemValue.Id}");

            OnCardClick?.Invoke(itemValue);
        }
    }

    private void DeleteItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { CommandParameter: TaskInfo itemToDelete })
        {
            ItemsSource.Remove(itemToDelete);
            Debug.WriteLine($"[DeleteItem_OnClick] Item removed at: {itemToDelete}. New this.ItemsSource count: {ItemsSource.Count}");
            Console.WriteLine($"Удален: {itemToDelete.Id}");
        }
        e.Handled = true;
    }
}