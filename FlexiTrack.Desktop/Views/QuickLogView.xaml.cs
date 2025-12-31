using System.ComponentModel;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.Messaging;
using FlexiTrack.Desktop.Infrastructure;
using FlexiTrack.Desktop.ViewModels;

namespace FlexiTrack.Desktop.Views;

public partial class QuickLogView : UserControl
{
    private TaskLogViewModel? _viewModel;
    private bool _isUpdatingCombo;

    public QuickLogView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        ListClientFilterCombo.SelectionChanged += ListClientFilterCombo_SelectionChanged;
        ListClientFilterCombo.DropDownOpened += Combo_DropDownOpened;
        ListClientFilterCombo.DropDownClosed += Combo_DropDownClosed;
    }

    private void Combo_DropDownOpened(object? sender, EventArgs e)
    {
        WeakReferenceMessenger.Default.Send(new ShowingDialogMessage(true));
    }

    private void Combo_DropDownClosed(object? sender, EventArgs e)
    {
        WeakReferenceMessenger.Default.Send(new ShowingDialogMessage(false));
    }

    private void OnDataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        }

        _viewModel = DataContext as TaskLogViewModel;

        if (_viewModel != null)
        {
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            UpdateClientCombo();
        }
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TaskLogViewModel.Clients))
        {
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
            ListClientFilterCombo.Items.Clear();

            var allClientsItem = new ComboBoxItem { Content = "All Clients", Tag = "all" };
            ListClientFilterCombo.Items.Add(allClientsItem);

            int selectedIndex = 0;
            bool foundSelection = _viewModel.ListClientFilter == "all";

            int index = 1;
            foreach (var client in _viewModel.Clients)
            {
                ListClientFilterCombo.Items.Add(new ComboBoxItem { Content = client, Tag = client });
                if (_viewModel.ListClientFilter == client)
                {
                    selectedIndex = index;
                    foundSelection = true;
                }
                index++;
            }

            if (!foundSelection)
            {
                _viewModel.ListClientFilter = "all";
                selectedIndex = 0;
            }

            ListClientFilterCombo.SelectedIndex = selectedIndex;
        }
        finally
        {
            _isUpdatingCombo = false;
        }
    }

    private void ListClientFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingCombo || _viewModel == null)
            return;

        if (ListClientFilterCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            _viewModel.ListClientFilter = tag;
        }
    }
}
