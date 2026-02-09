using System;
using System.Device.Gpio;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace PoolController;

public partial class MainWindow : Window
{
    private bool _isBypassOn = false;
    private GpioController _gpioController;
    private int _relayPin = 8;
    
    public MainWindow()
    {
        InitializeComponent();
        _gpioController = new GpioController();
        _gpioController.OpenPin(_relayPin, PinMode.Output);
    }

    private void Window_OnLoaded(object? sender, RoutedEventArgs e)
    {
        InitializeDataFiles();
         _ = GetAndDisplayTime();
         _ = GetAndDisplayWeatherData();
         _ = TogglePoolPumpRelay();
         _ = CheckChlorineAndSandDates();
         _ = CheckAndDisplayPoolTemp();
    }

    private void InitializeDataFiles()
    {
        try
        {
            if (!File.Exists("data.json"))
            {
                DataSettings dataSettings = new DataSettings();
            
                dataSettings.WeatherData = 0;
                dataSettings.PumpStartHour = 0;
                dataSettings.PumpStopHour = 0;
                dataSettings.WeatherTempTrigger = 0;
                dataSettings.PoolTempTrigger = 0;
                dataSettings.ChlorineDate = "2025/01/01";
                dataSettings.SandDate = "2025/01/01";
                
                string updatedData = JsonSerializer.Serialize(dataSettings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText("data.json", updatedData);
            }

            using StreamWriter file = File.CreateText("log.txt");  // Create log.txt File for logging

        }
        catch (Exception ex)
        {
            MessageBoxManager.GetMessageBoxStandard("Error !", ex.Message, ButtonEnum.Ok,
                MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
        }
    }

    private async Task GetAndDisplayTime()
    {
        while (true)
        {
            LblTime.Content = DateTime.Now.ToString("HH:mm:ss tt").ToUpper();
            await Task.Delay(1000);
        }
    }

    private async Task GetAndDisplayWeatherData()
    {
        while (true)
        {
            try
            {
                string apiKey = await File.ReadAllTextAsync("apikey.txt");
                string url =
                    $"https://api.openweathermap.org/data/2.5/weather?lat=-26.585360&lon=28.006899&units=metric&appid={apiKey}";
                
                
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                        
                    string weatherJson = await response.Content.ReadAsStringAsync();

                    using JsonDocument document = JsonDocument.Parse(weatherJson);
                        
                    // Extract JSON Data from Weather API Response  //////////
                    double temp =  document.RootElement.GetProperty("main").GetProperty("temp").GetDouble();
                    string condition = document.RootElement.GetProperty("weather")[0].GetProperty("main").GetString();
                    int humidity = document.RootElement.GetProperty("main").GetProperty("humidity").GetInt32();

                    
                    // Update UI Labels ///////
                    LblWeatherTemp.Content = Math.Round(temp, 1) + " °C";
                    LblWeatherCondition.Content = condition;
                    LblWeatherHumidity.Content = humidity + " %";
                    
                    
                    // Store Weather Temp in JSON Data file //////////
                    string readJson = await File.ReadAllTextAsync("data.json");
                    DataSettings dataSettings = JsonSerializer.Deserialize<DataSettings>(readJson);
                    
                    dataSettings.WeatherData = Convert.ToInt32(temp);
                    
                    string updatedData = JsonSerializer.Serialize(dataSettings, new JsonSerializerOptions { WriteIndented = true });
                    await File.WriteAllTextAsync("data.json", updatedData);
                }
            }
            catch (Exception ex)
            {
                ImgWarning.IsVisible = false;
                ImgWarningOn.IsVisible = true;
                await using (StreamWriter writer = new StreamWriter("log.txt", append: true))
                {
                    await writer.WriteLineAsync(DateTime.Now + " : [GetAndDisplayWeatherData()] : " + ex.Message);
                }
            }
            await Task.Delay(30000);
        }
    }

    private async Task TogglePoolPumpRelay()
    {
        while (_isBypassOn == false)
        {
            try
            {
                string readJson = await File.ReadAllTextAsync("data.json");
                DataSettings dataSettings = JsonSerializer.Deserialize<DataSettings>(readJson);
                
                int hourNow = DateTime.Now.Hour;
                int poolPumpStartHour = dataSettings.PumpStartHour;
                int poolPumpStopHour = dataSettings.PumpStopHour;
                int weatherTemp = dataSettings.WeatherData;
                int weatherTempTrigger = dataSettings.WeatherTempTrigger;

                if (hourNow >= poolPumpStartHour && hourNow < poolPumpStopHour && weatherTemp >= weatherTempTrigger)
                {
                    LblPumpStatus.Content = "PUMP ON";
                    ImgPumpOn.IsVisible = true;
                    ImgPumpBypass.IsVisible = false;
                    ImgPumpOff.IsVisible = false;
                    try
                    {
                       _gpioController.Write(_relayPin, PinValue.High);  //////////////////////////////////////////////////////////////////
                    }
                    catch (Exception ex)
                    {
                        ImgWarning.IsVisible = false;
                        ImgWarningOn.IsVisible = true;
                        await using (StreamWriter writer = new StreamWriter("log.txt", append: true))
                        {
                            await writer.WriteLineAsync(DateTime.Now + " : [TogglePoolPumpRelay() - _gpioController] : " + ex.Message);
                        }
                    }
                }
                else
                {
                    LblPumpStatus.Content = "PUMP OFF";
                    ImgPumpOn.IsVisible = false;
                    ImgPumpBypass.IsVisible = false;
                    ImgPumpOff.IsVisible = true;
                    try
                    {
                        _gpioController.Write(_relayPin, PinValue.Low); //////////////////////////////////////////////////////////
                    }
                    catch (Exception ex)
                    {
                        ImgWarning.IsVisible = false;
                        ImgWarningOn.IsVisible = true;
                        await using (StreamWriter writer = new StreamWriter("log.txt", append: true))
                        {
                            await writer.WriteLineAsync(DateTime.Now + " : [TogglePoolPumpRelay() - _gpioController] : " + ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ImgWarning.IsVisible = false;
                ImgWarningOn.IsVisible = true;
                await using (StreamWriter writer = new StreamWriter("log.txt", append: true))
                {
                    await writer.WriteLineAsync(DateTime.Now + " : [TogglePoolPumpRelay()] : " + ex.Message);
                }
            }
            await Task.Delay(2000);
        }
    }

    private async Task CheckChlorineAndSandDates()
    {
        while (true)
        {
            try
            {
                DateTime dateNow = DateTime.Now.Date;
                
                string readJson = await File.ReadAllTextAsync("data.json");
                DataSettings dataSettings = JsonSerializer.Deserialize<DataSettings>(readJson);
                
                string chlorineDate = dataSettings.ChlorineDate;
                string sandDate = dataSettings.SandDate;
                
                DateTime cDate = DateTime.Parse(chlorineDate);
                DateTime sDate = DateTime.Parse(sandDate);
                

                if (cDate <= dateNow)
                {
                    ImgChlorine.IsVisible = false;
                    ImgChlorineOn.IsVisible = true;
                }
                else
                {
                    ImgChlorine.IsVisible = true;
                    ImgChlorineOn.IsVisible = false;
                }

                if (sDate <= dateNow)
                {
                    ImgSand.IsVisible = false;
                    ImgSandOn.IsVisible = true;
                }
                else
                {
                    ImgSand.IsVisible = true;
                    ImgSandOn.IsVisible = false;
                }
            }
            catch (Exception ex)
            {
                ImgWarning.IsVisible = false;
                ImgWarningOn.IsVisible = true;
                await using (StreamWriter writer = new StreamWriter("log.txt", append: true))
                {
                    await writer.WriteLineAsync(DateTime.Now + " : [CheckChlorineAndSandDates()] : " + ex.Message);
                }
            }
            await Task.Delay(5000);
        }
    }

    private async Task CheckAndDisplayPoolTemp()
    {
        while (true)
        {
            try
            {
                
                double poolTemp = 0; // Pool Sensor Data Value Temp C
                
                string sensorPath = "/sys/bus/w1/devices/28-25780e1e64ff/w1_slave"; ////////////////////////////////////////////////////
                string[] lines = await File.ReadAllLinesAsync(sensorPath);
                
                if (lines[0].Contains("YES"))
                {
                    int tempIndex = lines[1].IndexOf("t=");
                    if (tempIndex != -1)
                    {
                        string tempString = lines[1].Substring(tempIndex + 2);
                        poolTemp = double.Parse(tempString) / 1000.0;
                        LblPoolTemp.Content = Math.Round(poolTemp, 1) + " °C";
                    }
                }
                
                
                
                string readJson = await File.ReadAllTextAsync("data.json");
                DataSettings dataSettings = JsonSerializer.Deserialize<DataSettings>(readJson);
                
                
                int poolTempTrigger =  dataSettings.PoolTempTrigger;
                
                
                if (poolTemp >= poolTempTrigger)
                {
                    LblPoolTemp.Foreground = Brushes.LawnGreen;
                }
                else
                {
                    LblPoolTemp.Foreground = Brushes.Cyan;
                }
            }
            catch (Exception ex)
            {
                ImgWarning.IsVisible = false;
                ImgWarningOn.IsVisible = true;
                await using (StreamWriter writer = new StreamWriter("log.txt", append: true))
                {
                    await writer.WriteLineAsync(DateTime.Now + " : [CheckAndDisplayPoolTemp()] : " + ex.Message);
                }
            }
            await Task.Delay(5000);
        }
    }

    private void BtnBypass_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_isBypassOn == false)
        {
            LblPumpStatus.Content = "BYPASS ON";
            BtnBypass.Background = Brushes.Magenta;
            ImgPumpOn.IsVisible = false;
            ImgPumpBypass.IsVisible = true;
            ImgPumpOff.IsVisible = false;
            _isBypassOn = true;
            try
            {
                _gpioController.Write(_relayPin, PinValue.High); ////////////////////////////////////////////////////////////
            }
            catch (Exception ex)
            {
                MessageBoxManager.GetMessageBoxStandard("Error !", ex.Message, ButtonEnum.Ok,
                    MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
            }
        }
        else
        {
            LblPumpStatus.Content = "PUMP OFF";
            BtnBypass.Background = Brush.Parse("#94B4C1");
            ImgPumpOn.IsVisible = false;
            ImgPumpBypass.IsVisible = false;
            ImgPumpOff.IsVisible = true;
            _isBypassOn = false;
            try
            {
                _ = TogglePoolPumpRelay();  // Re-fire and forget this Async Task.......
                _gpioController.Write(_relayPin, PinValue.Low);  ////////////////////////////////////////////////////////////
            }
            catch (Exception ex)
            {
                MessageBoxManager.GetMessageBoxStandard("Error !", ex.Message, ButtonEnum.Ok,
                    MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
            }
        }
    }

    private void BtnSettings_OnClick(object? sender, RoutedEventArgs e)
    {
        var settings = new SettingsWindow();
        settings.Show();
    }

    private void BtnInfo_OnClick(object? sender, RoutedEventArgs e)
    {
        var info = new InfoWindow();
        info.Show();
        ImgWarning.IsVisible = true;
        ImgWarningOn.IsVisible = false;
    }

    private async void BtnRestart_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var msg = MessageBoxManager.GetMessageBoxStandard("Warning",
                "You are about to Restart the System. !", ButtonEnum.OkAbort, MsBox.Avalonia.Enums.Icon.Warning);
            var result = await msg.ShowAsync();
            if (result != ButtonResult.Ok) return;
            if (OperatingSystem.IsWindows())
            {
                Process.Start("shutdown", "/r /t 0");
            }
            else
            {
                Process.Start("sudo", "reboot");
            }
        }
        catch (Exception ex)
        {
            LblTitle.Content = ex.Message;
            ImgWarning.IsVisible = false;
            ImgWarningOn.IsVisible = true;
        }
    }

    private async void BtnExit_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var msg = MessageBoxManager.GetMessageBoxStandard("Warning", 
                "You are about to Exit the Application. !", ButtonEnum.OkAbort, MsBox.Avalonia.Enums.Icon.Warning);
            var result = await msg.ShowAsync();
            if (result == ButtonResult.Ok)
            {
                Environment.Exit(0);
            }
        }
        catch (Exception ex)
        {
            LblTitle.Content = ex.Message;
            ImgWarning.IsVisible = false;
            ImgWarningOn.IsVisible = true;
        }
    }
}
