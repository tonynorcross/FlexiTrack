using System.Windows.Controls;
using FlexiTrack.Desktop.ViewModels;

namespace FlexiTrack.Desktop.Views;

public partial class SummaryView : UserControl
{
    public SummaryView()
    {
        InitializeComponent();
        DataContextChanged += SummaryView_DataContextChanged;
    }

    private void SummaryView_DataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
    {
        if (DataContext is TodaySummaryViewModel vm)
        {
            vm.PropertyChanged += Vm_PropertyChanged;
            UpdateClientFilterItems(vm);
        }
    }

    private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TodaySummaryViewModel.AvailableClients) && DataContext is TodaySummaryViewModel vm)
        {
            UpdateClientFilterItems(vm);
        }
    }

    private void UpdateClientFilterItems(TodaySummaryViewModel vm)
    {
        ClientFilterCombo.Items.Clear();
        ClientFilterCombo.Items.Add(new ComboBoxItem { Content = "All Clients", Tag = "all" });
        ClientFilterCombo.Items.Add(new ComboBoxItem { Content = "No Client", Tag = "none" });

        if (vm.AvailableClients.Count > 0)
        {
            ClientFilterCombo.Items.Add(new Separator());
            foreach (var client in vm.AvailableClients)
            {
                ClientFilterCombo.Items.Add(new ComboBoxItem { Content = client, Tag = client });
            }
        }

        // Select the current filter
        foreach (ComboBoxItem item in ClientFilterCombo.Items.OfType<ComboBoxItem>())
        {
            if (item.Tag?.ToString() == vm.SelectedClientFilter)
            {
                ClientFilterCombo.SelectedItem = item;
                break;
            }
        }

        if (ClientFilterCombo.SelectedItem == null)
        {
            ClientFilterCombo.SelectedIndex = 0;
        }
    }

    private void ClientFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ClientFilterCombo.SelectedItem is ComboBoxItem item && DataContext is TodaySummaryViewModel vm)
        {
            vm.SelectedClientFilter = item.Tag?.ToString() ?? "all";
        }
    }
}
