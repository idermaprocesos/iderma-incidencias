using IdermaIncidencias.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace IdermaIncidencias.Pages;

public sealed partial class SeguimientoPage : Page
{
    public SeguimientoPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        ReportesNube.Cambiado += Refrescar;
        Refrescar();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e) =>
        ReportesNube.Cambiado -= Refrescar;

    private void Refrescar()
    {
        if (!DispatcherQueue.HasThreadAccess)
        {
            DispatcherQueue.TryEnqueue(Refrescar);
            return;
        }

        Lista.ItemsSource = null;
        Lista.ItemsSource = ReportesNube.Items;
        var hay = ReportesNube.Items.Count > 0;
        Lista.Visibility = hay ? Visibility.Visible : Visibility.Collapsed;
        Vacio.Visibility = hay ? Visibility.Collapsed : Visibility.Visible;
    }
}
