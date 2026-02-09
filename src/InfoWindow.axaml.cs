using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace PoolController;

public partial class InfoWindow : Window
{
    public InfoWindow()
    {
        InitializeComponent();
    }
    
    private void InfoWindow_OnLoaded(object? sender, RoutedEventArgs e)
    {
        using (StreamReader reader = new StreamReader("log.txt"))
        {
            TxtLog.Text = reader.ReadToEnd();
        }
    }

    private void BtnInfoExit_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}