using System.Runtime.InteropServices;
using IdermaIncidencias.Pages;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;
using WinRT.Interop;

namespace IdermaIncidencias;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        Iderma.Shell.BotonLauncher.Colocar(AppTitleBar);
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        AppWindow.Resize(new SizeInt32(1280, 860));
        Activated += AlAbrir;
    }

    private void AlAbrir(object sender, WindowActivatedEventArgs args)
    {
        Activated -= AlAbrir;
        AplicarIcono();
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.Maximize();
        }
    }

    private void AplicarIcono()
    {
        var ruta = Path.Combine(AppContext.BaseDirectory, "img", "logo-incidencias.ico");
        if (!File.Exists(ruta))
        {
            return;
        }

        AppWindow.SetIcon(ruta);
        var hwnd = WindowNative.GetWindowHandle(this);
        _iconoGrande = LoadImage(IntPtr.Zero, ruta, 1, 32, 32, 0x0010);
        _iconoChico = LoadImage(IntPtr.Zero, ruta, 1, 16, 16, 0x0010);
        if (_iconoGrande != IntPtr.Zero)
        {
            SendMessage(hwnd, 0x0080, (IntPtr)1, _iconoGrande);
            SetClassLongPtr(hwnd, -14, _iconoGrande);
        }

        if (_iconoChico != IntPtr.Zero)
        {
            SendMessage(hwnd, 0x0080, IntPtr.Zero, _iconoChico);
            SetClassLongPtr(hwnd, -34, _iconoChico);
        }
    }

    private static IntPtr _iconoGrande;
    private static IntPtr _iconoChico;

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr LoadImage(IntPtr instancia, string ruta, uint tipo, int ancho, int alto, uint opciones);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SendMessage(IntPtr hwnd, uint mensaje, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", EntryPoint = "SetClassLongPtrW", SetLastError = true)]
    private static extern IntPtr SetClassLongPtr(IntPtr hwnd, int indice, IntPtr valor);

    public void IrASeguimiento() => Seleccionar("seguimiento");

    private void Seleccionar(string tag)
    {
        foreach (var item in NavView.MenuItems.OfType<NavigationViewItem>())
        {
            if (item.Tag as string == tag)
            {
                NavView.SelectedItem = item;
                break;
            }
        }
    }

    private void TitleBar_PaneToggleRequested(TitleBar sender, object args) =>
        NavView.IsPaneOpen = !NavView.IsPaneOpen;

    private void TitleBar_BackRequested(TitleBar sender, object args)
    {
        if (NavFrame.CanGoBack)
        {
            NavFrame.GoBack();
        }
    }

    private void NavView_Loaded(object sender, RoutedEventArgs e)
    {
        if (NavFrame.Content is null)
        {
            NavFrame.Navigate(typeof(ReportePage));
        }
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem item)
        {
            return;
        }

        var pagina = item.Tag as string == "seguimiento" ? typeof(SeguimientoPage) : typeof(ReportePage);
        if (NavFrame.CurrentSourcePageType != pagina)
        {
            NavFrame.Navigate(pagina);
        }
    }
}
