using System;
using System.IO;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace PoolController;

public partial class Settings2Window : Window
{
    public Settings2Window()
    {
        InitializeComponent();
    }
    
    private void Window2_OnLoaded(object? sender, RoutedEventArgs e)
    {
        try
        {
            string readJson = File.ReadAllText("data.json");
            DataSettings dataSettings = JsonSerializer.Deserialize<DataSettings>(readJson);
            
            string chlorineDate = dataSettings.ChlorineDate;
            DateTime cDate = DateTime.Parse(chlorineDate);
            int year = cDate.Year;
            int month = cDate.Month;
            int day = cDate.Day;
            DatePickerChlorine.SelectedDate = new DateTimeOffset(new  DateTime(year, month, day));
            
            string sandDate = dataSettings.SandDate;
            DateTime sDate = DateTime.Parse(sandDate);
            int year2 = sDate.Year;
            int month2 = sDate.Month;
            int day2 = sDate.Day;
            DatePickerSand.SelectedDate = new DateTimeOffset(new  DateTime(year2, month2, day2));
        }
        catch (Exception ex)
        {
            MessageBoxManager.GetMessageBoxStandard("Error !", ex.Message, ButtonEnum.Ok,
                MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
        }
    }

    private void BtnSettingsExit_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void BtnSettingsSave_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            string readJson = File.ReadAllText("data.json");
            DataSettings dataSettings = JsonSerializer.Deserialize<DataSettings>(readJson);
            
            dataSettings.ChlorineDate = DatePickerChlorine.SelectedDate?.Date.ToString("yyyy/MM/dd");
            dataSettings.SandDate = DatePickerSand.SelectedDate?.Date.ToString("yyyy/MM/dd");
            
            string writeJson = JsonSerializer.Serialize(dataSettings);
            File.WriteAllText("data.json", writeJson);
            
            MessageBoxManager.GetMessageBoxStandard("Saved !", "Settings Saved Successfully !", ButtonEnum.Ok,
                MsBox.Avalonia.Enums.Icon.Success).ShowAsync();
        }
        catch (Exception ex)
        {
            MessageBoxManager.GetMessageBoxStandard("Error !", ex.Message, ButtonEnum.Ok,
                MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
        }
    }
}