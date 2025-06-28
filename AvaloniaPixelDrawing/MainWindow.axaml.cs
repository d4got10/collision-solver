using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using MyDiplomaSolver;

namespace AvaloniaPixelDrawing;

public partial class MainWindow : Window
{
    public SimulationViewModel ViewModel { get; } = new();
    
    public MainWindow()
    {
        InitializeComponent();
        
        DataContext = ViewModel;
        double giga = 1_000_000_000;
        double a = 3702.77;
        double b = 2378.63;
        ViewModel.Mu = 10 * giga;
        ViewModel.Lambda = 3.24 * giga;
        ViewModel.V = 4.83 * giga;
        ViewModel.Ro = 2400;
        ViewModel.TaskName = "Новое задание";
        
        UpdateTexts();

        ViewModel.BorderConditions = new BorderConditions(
        [
            new BorderConditionPoint(0.000 * 0.001,  0.0 * 0.001),
            new BorderConditionPoint(0.125 * 0.001, -5.0 * 0.001),
            new BorderConditionPoint(0.250 * 0.001, +5.0 * 0.001),
            new BorderConditionPoint(0.375 * 0.001, -5.0 * 0.001),
            new BorderConditionPoint(0.500 * 0.001, +5.0 * 0.001),
            new BorderConditionPoint(0.625 * 0.001, -5.0 * 0.001),
            new BorderConditionPoint(0.750 * 0.001, +5.0 * 0.001),
            new BorderConditionPoint(0.875 * 0.001, -5.0 * 0.001),
        ]);
        
        // ViewModel.BorderConditions = new BorderConditions(
        // [
        //     new BorderConditionPoint(0.000 * 0.001,  0.0 * 0.001),
        //     new BorderConditionPoint(0.200 * 0.001, -3.0 * 0.001),
        //     new BorderConditionPoint(0.500 * 0.001,  0.0 * 0.001),
        //     new BorderConditionPoint(0.750 * 0.001,  4.0 * 0.001),
        //     new BorderConditionPoint(0.900 * 0.001,  0.0 * 0.001),
        // ]);
        
        // ViewModel.BorderConditions = new BorderConditions(
        // [
        //     new BorderConditionPoint(0.000 * 0.001,  0.0 * 0.001),
        //     new BorderConditionPoint(0.300 * 0.001, -4.5 * 0.001),
        //     new BorderConditionPoint(0.500 * 0.001, -5.0 * 0.001),
        //     new BorderConditionPoint(0.600 * 0.001, -4.0 * 0.001),
        //     new BorderConditionPoint(0.800 * 0.001,  0.0 * 0.001),
        // ]);
        
        // Обработчики кнопок
        EditButton.Click += (s, e) => 
        {
            ValueLambdaInput.IsEnabled = true;
            ValueMuInput.IsEnabled = true;
            ValueVInput.IsEnabled = true;
            ValueRoInput.IsEnabled = true;
            AcceptButton.IsEnabled = true;
            TaskNameInput.IsEnabled = true;
            EditButton.IsEnabled = false;
        };
        
        AcceptButton.Click += (s, e) => 
        {
            try
            {
                ViewModel.Lambda = double.Parse(ValueLambdaInput.Text);
                ViewModel.Mu = double.Parse(ValueMuInput.Text);
                ViewModel.V = double.Parse(ValueVInput.Text);
                ViewModel.Ro = double.Parse(ValueRoInput.Text);
                
                ValueLambdaInput.IsEnabled = false;
                ValueMuInput.IsEnabled = false;
                ValueVInput.IsEnabled = false;
                ValueRoInput.IsEnabled = false;
                
                TaskNameInput.IsEnabled = false;
                AcceptButton.IsEnabled = false;
                EditButton.IsEnabled = true;

                // Здесь можно добавить обработку введенных значений
                SimulationView.Update();
            }
            catch
            {
                // ignored
            }
        };
        
        SimulationView.Update();
    }
    private void UpdateTexts()
    {

        ValueLambdaInput.Text = ViewModel.Lambda.ToString();
        ValueMuInput.Text = ViewModel.Mu.ToString();
        ValueVInput.Text = ViewModel.V.ToString();
        ValueRoInput.Text = ViewModel.Ro.ToString();
    }

    private void SaveTaskButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var taskInfo = new TaskInfo
        {
            Id = Guid.NewGuid(),
            Name = TaskNameInput.Text!,
            SpeedA = ViewModel.SpeedA,
            SpeedB = ViewModel.SpeedB,
        };
        
        MyCustomList.ItemsSource.Add(taskInfo);
                
        Debug.WriteLine($"[AddItemButton_OnClick] Added: '{taskInfo}'. New this.ItemsSource count: {MyCustomList.ItemsSource.Count}");
        Console.WriteLine($"Добавлен: {taskInfo.Id}"); // Ваш оригинальный вывод

        // Попробуем проверить ItemsSource у ListBox напрямую
        int count = 0;
        foreach (var item in MyCustomList.ItemsListBox.ItemsSource!) count++;
        Debug.WriteLine($"[AddItemButton_OnClick] ItemsListBox.ItemsSource item count: {count}");
    }
}