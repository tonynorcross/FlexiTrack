using System.Windows;
using FlexiTrack.Desktop.ViewModels;

namespace FlexiTrack.Desktop.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
