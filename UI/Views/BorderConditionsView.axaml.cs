using System;
using System.Linq;
using Avalonia.Controls;
using Solver.Models;

namespace UI.Views;

public partial class BorderConditionsView : UserControl
{
    public BorderConditionsView()
    {
        InitializeComponent();
    }

    public BorderConditions BorderConditions
    {
        get => BorderConditionsControl.BorderConditions;
        set
        {
            BorderConditionsControl.BorderConditions = value;

            var maxValue = BorderConditions.Points.Max(x => Math.Abs(x.Value));
            MaxValueText.Text = maxValue.ToString("F6");
            MinValueText.Text = (-maxValue).ToString("F6");
        }
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        BorderConditionsControl.InvalidateVisual();
    }
}