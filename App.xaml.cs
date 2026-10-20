using IdermaIncidencias.Services;
using Microsoft.UI.Xaml;

namespace IdermaIncidencias;

public partial class App : Application
{
    public static MainWindow? MainAppWindow { get; private set; }

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        ReportesNube.CargarLocal();
        MainAppWindow = new MainWindow();
        MainAppWindow.Activate();
        ReportesNube.IniciarEscucha(MainAppWindow.DispatcherQueue);
    }
}
