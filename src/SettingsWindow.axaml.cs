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

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }
    
    private void Window_OnLoaded(object? sender, RoutedEventArgs e)
    {
        try
        {
            string json = File.ReadAllText("data.json");
            DataSettings dataSettings = JsonSerializer.Deserialize<DataSettings>(json);
            
            SpinPumpStart.Text = dataSettings.PumpStartHour.ToString();
            SpinPumpStop.Text = dataSettings.PumpStopHour.ToString();
            SpinWeatherTemp.Text = dataSettings.WeatherTempTrigger.ToString();
            SpinPoolTemp.Text = dataSettings.PoolTempTrigger.ToString();
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

            dataSettings.PumpStartHour = Convert.ToInt32(SpinPumpStart.Value);
            dataSettings.PumpStopHour = Convert.ToInt32(SpinPumpStop.Value);
            dataSettings.WeatherTempTrigger = Convert.ToInt32(SpinWeatherTemp.Value);
            dataSettings.PoolTempTrigger = Convert.ToInt32(SpinPoolTemp.Value);
            
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

    private void BtnSettings2_OnClick(object? sender, RoutedEventArgs e)
    {
        var settings2Window = new Settings2Window();
        settings2Window.Show();
    }
}