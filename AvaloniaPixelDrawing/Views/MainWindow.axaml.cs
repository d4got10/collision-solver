using System;
using System.Diagnostics;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Solver;

namespace AvaloniaPixelDrawing;

public partial class MainWindow : Window
{
    public SimulationViewModel ViewModel { get; } = new();
    
    public const double Giga = 1_000_000_000;

    private readonly SaveLoader _saveLoader = new("data.json");
    
    public MainWindow()
    {
        InitializeComponent();
        
        DataContext = ViewModel;

        var tempSavedTasks = _saveLoader.Load().ToArray();

        if (tempSavedTasks.Length > 0)
        {
            var task = tempSavedTasks[0];

            ApplyTask(task);

            foreach (var savedTask in tempSavedTasks)
            {
                MyCustomList.ItemsSource.Add(savedTask);
            }
        }
        else
        {
            InitializeDefaultTask();
        }
        
        UpdateTexts();

        
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
                ViewModel.Lambda = double.Parse(ValueLambdaInput.Text) * Giga;
                ViewModel.Mu = double.Parse(ValueMuInput.Text) * Giga;
                ViewModel.V = double.Parse(ValueVInput.Text) * Giga;
                ViewModel.Ro = double.Parse(ValueRoInput.Text);
                
                ValueLambdaInput.IsEnabled = false;
                ValueMuInput.IsEnabled = false;
                ValueVInput.IsEnabled = false;
                ValueRoInput.IsEnabled = false;

                ViewModel.TaskName = TaskNameInput.Text;
                TaskNameInput.IsEnabled = false;
                
                AcceptButton.IsEnabled = false;
                EditButton.IsEnabled = true;

                var currentTask = MyCustomList.ItemsSource.FirstOrDefault(x => x.Id == ViewModel.TaskId);
                
                if (currentTask is not null)
                {
                    var index = MyCustomList.ItemsSource.IndexOf(currentTask);
                    MyCustomList.ItemsSource.RemoveAt(index);

                    currentTask.Lambda = ViewModel.Lambda;
                    currentTask.Mu = ViewModel.Mu;
                    currentTask.V = ViewModel.V;
                    currentTask.Ro = ViewModel.Ro;
                    currentTask.Name = ViewModel.TaskName!;
                    currentTask.BorderConditions = ViewModel.BorderConditions;
                    MyCustomList.ItemsSource.Insert(index, currentTask);
                }
                
                // Здесь можно добавить обработку введенных значений
                SimulationView.Update();
            }
            catch
            {
                // ignored
            }
        };

        MyCustomList.ItemsSource.CollectionChanged += (sender, args) =>
        {
            SaveCurrentTasks();
        };

        MyCustomList.OnCardClick += taskInfo =>
        {
            ApplyTask(taskInfo);
            UpdateTexts();
            SimulationView.Update();
        };
        
        SimulationView.Update();
    }
    private void ApplyTask(TaskInfo task)
    {
        ViewModel.TaskId = task.Id;
        ViewModel.Mu = task.Mu;
        ViewModel.Lambda = task.Lambda;
        ViewModel.V = task.V;
        ViewModel.Ro = task.Ro;
        ViewModel.TaskName = task.Name;
        ViewModel.BorderConditions = task.BorderConditions;
    }
    
    private void SaveCurrentTasks()
    {
        _saveLoader.Save(MyCustomList.ItemsSource);
    }
    
    private void InitializeDefaultTask()
    {
        ViewModel.TaskId = Guid.NewGuid();
        ViewModel.Mu = 10 * Giga;
        ViewModel.Lambda = 3.24 * Giga;
        ViewModel.V = 4.83 * Giga;
        ViewModel.Ro = 2400;
        ViewModel.TaskName = "Новое задание";
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
    }
    private void UpdateTexts()
    {
        TaskNameInput.Text = ViewModel.TaskName;
        ValueLambdaInput.Text = (ViewModel.Lambda / Giga).ToString();
        ValueMuInput.Text = (ViewModel.Mu / Giga).ToString();
        ValueVInput.Text = (ViewModel.V / Giga).ToString();
        ValueRoInput.Text = (ViewModel.Ro).ToString();
    }

    private void SaveTaskButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var taskInfo = GetCurrentTaskInfo();
        taskInfo.Id = Guid.NewGuid();
        
        MyCustomList.ItemsSource.Add(taskInfo);
                
        Console.WriteLine($"Добавлен: {taskInfo.Id}"); // Ваш оригинальный вывод
    }
    
    private TaskInfo GetCurrentTaskInfo()
    {
        var taskInfo = new TaskInfo
        {
            Id = ViewModel.TaskId,
            Name = ViewModel.TaskName,
            Lambda = ViewModel.Lambda,
            Mu = ViewModel.Mu,
            Ro = ViewModel.Ro,
            BorderConditions = ViewModel.BorderConditions,
            V = ViewModel.V
        };
        return taskInfo;
    }
}