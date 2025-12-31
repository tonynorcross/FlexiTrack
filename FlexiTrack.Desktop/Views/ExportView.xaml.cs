using System.Windows.Controls;
using CommunityToolkit.Mvvm.Messaging;
using FlexiTrack.Desktop.Infrastructure;
using FlexiTrack.Desktop.ViewModels;

namespace FlexiTrack.Desktop.Views;

public partial class ExportView : UserControl
{
    private ExportViewModel? _viewModel;
    private bool _isUpdatingCombo;

    public ExportView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        ClientCombo.SelectionChanged += ClientCombo_SelectionChanged;
        ClientCombo.DropDownOpened += ClientCombo_DropDownOpened;
        ClientCombo.DropDownClosed += ClientCombo_DropDownClosed;
    }

    private void ClientCombo_DropDownOpened(object? sender, EventArgs e)
    {
        WeakReferenceMessenger.Default.Send(new ShowingDialogMessage(true));
    }

    private void ClientCombo_DropDownClosed(object? sender, EventArgs e)
    {
        WeakReferenceMessenger.Default.Send(new ShowingDialogMessage(false));
    }

    private void ClientCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingCombo || _viewModel == null)
            return;

        if (ClientCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            _viewModel.SelectedClient = tag;
        }
    }

    private void OnDataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is ExportViewModel vm)
        {
            _viewModel = vm;
            vm.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(ExportViewModel.AvailableClients))
                {
                    UpdateClientCombo();
                }
            };
            UpdateClientCombo();
        }
    }

    private void UpdateClientCombo()
    {
        if (_viewModel == null)
            return;

        _isUpdatingCombo = true;
        try
        {
            ClientCombo.Items.Clear();

            var allClientsItem = new ComboBoxItem { Content = "All Clients", Tag = "all" };
            ClientCombo.Items.Add(allClientsItem);

            int selectedIndex = 0; // Default to "All Clients"

            int index = 1;
            foreach (var client in _viewModel.AvailableClients)
            {
                ClientCombo.Items.Add(new ComboBoxItem { Content = client, Tag = client });
                if (_viewModel.SelectedClient == client)
                {
                    selectedIndex = index;
                }
                index++;
            }

            // Set the selected index
            ClientCombo.SelectedIndex = selectedIndex;
        }
        finally
        {
            _isUpdatingCombo = false;
        }
    }
}
