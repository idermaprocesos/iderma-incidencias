using Iderma.Core.Services;
using IdermaIncidencias.Models;
using IdermaIncidencias.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace IdermaIncidencias.Pages;

public sealed partial class ReportePage : Page
{
    private static readonly string[] Areas = ["Clínica", "Oficina administrativa", "Streaming"];
    private string _usuarioId = string.Empty;

    public ReportePage()
    {
        InitializeComponent();
        ComboArea.ItemsSource = Areas;
        ComboArea.SelectedIndex = 0;
        ComboTipo.ItemsSource = new[]
        {
            "Equipo electrónico averiado",
            "Otro equipo averiado",
            "Falta de insumo",
            "Falta de recurso"
        };
        ComboTipo.SelectedIndex = 2;
        CargarUsuario();
    }

    private void ComboTipo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CajaNombre is null)
        {
            return;
        }

        CajaNombre.Header = ComboTipo.SelectedIndex is 0 or 1
            ? "Equipo"
            : ComboTipo.SelectedIndex == 3 ? "Recurso" : "Insumo";
    }

    private async void Enviar_Click(object sender, RoutedEventArgs e)
    {
        var solicitante = (CajaSolicitante.Text ?? string.Empty).Trim();
        var area = ComboArea.SelectedItem as string ?? string.Empty;
        var nombre = (CajaNombre.Text ?? string.Empty).Trim();
        var enlace = (CajaEnlace.Text ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(_usuarioId) || solicitante.Length == 0)
        {
            Mostrar("Inicie sesión en el launcher. El reporte usa esa cuenta.", InfoBarSeverity.Warning);
            return;
        }

        if (area.Length == 0 || nombre.Length == 0)
        {
            Mostrar("Elija el área y escriba qué falta o qué se malogró.", InfoBarSeverity.Warning);
            return;
        }

        if (!double.IsNaN(CajaPrecio.Value) && CajaPrecio.Value < 0)
        {
            Mostrar("El precio no puede ser negativo.", InfoBarSeverity.Warning);
            return;
        }

        var ahora = DateTimeOffset.Now;
        var reporte = new Reporte
        {
            Folio = "INC-" + ahora.ToLocalTime().ToString("yyyyMMdd-HHmmss"),
            UsuarioId = _usuarioId,
            Solicitante = solicitante,
            Area = area,
            Tipo = ComboTipo.SelectedIndex switch
            {
                0 => TipoReporte.EquipoElectronico,
                1 => TipoReporte.Equipo,
                3 => TipoReporte.Recurso,
                _ => TipoReporte.Insumo
            },
            Nombre = nombre,
            Enlace = enlace,
            DetalleTexto = (CajaDetalle.Text ?? string.Empty).Trim(),
            Unidad = string.IsNullOrWhiteSpace(CajaUnidad.Text) ? "und" : CajaUnidad.Text.Trim(),
            Cantidad = double.IsNaN(CajaCantidad.Value) || CajaCantidad.Value < 1 ? 1 : CajaCantidad.Value,
            PrecioReposicion = double.IsNaN(CajaPrecio.Value) ? 0 : CajaPrecio.Value,
            Urgente = CajaUrgente.IsChecked == true,
            Fecha = ahora
        };

        try
        {
            await ReportesNube.PublicarAsync(reporte);
            CajaNombre.Text = string.Empty;
            CajaEnlace.Text = string.Empty;
            CajaDetalle.Text = string.Empty;
            CajaPrecio.Value = 0;
            CajaUrgente.IsChecked = false;
            Mostrar("El reporte se envió a logística.", InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            Mostrar(ex.Message, InfoBarSeverity.Error);
        }
    }

    private void CargarUsuario()
    {
        try
        {
            using var sesiones = new SessionStore();
            var sesion = sesiones.Snapshot();
            if (string.IsNullOrWhiteSpace(sesion.UserId))
            {
                BotonEnviar.IsEnabled = false;
                Mostrar("No hay una sesión de Iderma. Entre con su cuenta en el launcher.", InfoBarSeverity.Warning);
                return;
            }

            using var cuentas = new UserStore();
            var cuenta = cuentas.FindById(sesion.UserId);
            _usuarioId = cuenta?.Id ?? sesion.UserId;
            var nombre = cuenta?.DisplayName;
            if (string.IsNullOrWhiteSpace(nombre))
            {
                nombre = sesion.DisplayName;
            }

            if (string.IsNullOrWhiteSpace(nombre))
            {
                nombre = cuenta?.Username ?? sesion.Username;
            }

            CajaSolicitante.Text = nombre ?? string.Empty;
        }
        catch (Exception ex)
        {
            BotonEnviar.IsEnabled = false;
            Mostrar("No se pudo leer la cuenta: " + ex.Message, InfoBarSeverity.Error);
        }
    }

    private void Mostrar(string mensaje, InfoBarSeverity severidad)
    {
        Aviso.Severity = severidad;
        Aviso.Message = mensaje;
        Aviso.IsOpen = true;
    }
}
