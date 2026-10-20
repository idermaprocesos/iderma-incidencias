using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace IdermaIncidencias.Models;

public enum TipoReporte
{
    EquipoElectronico,
    Equipo,
    Insumo,
    Recurso
}

public enum EstadoReporte
{
    Pendiente,
    Aceptada,
    Denegada
}

public sealed class Reporte : INotifyPropertyChanged
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-PE");
    private EstadoReporte _estado = EstadoReporte.Pendiente;
    private string _motivo = string.Empty;
    private string _estadoOrden = string.Empty;

    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Folio { get; set; } = string.Empty;
    public string UsuarioId { get; set; } = string.Empty;
    public string Solicitante { get; set; } = string.Empty;
    public string Enlace { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public TipoReporte Tipo { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string DetalleTexto { get; set; } = string.Empty;
    public string Unidad { get; set; } = "und";
    public double Cantidad { get; set; } = 1;
    public double PrecioReposicion { get; set; }
    public bool Urgente { get; set; }
    public DateTimeOffset Fecha { get; set; } = DateTimeOffset.Now;

    public EstadoReporte Estado
    {
        get => _estado;
        set
        {
            _estado = value;
            Avisar();
            Avisar(nameof(EstadoTexto));
            Avisar(nameof(ResumenRespuesta));
            Avisar(nameof(EstadoSeguimiento));
        }
    }

    public string EstadoOrden
    {
        get => _estadoOrden;
        set
        {
            _estadoOrden = value ?? string.Empty;
            Avisar();
            Avisar(nameof(EstadoSeguimiento));
        }
    }

    public string Motivo
    {
        get => _motivo;
        set
        {
            _motivo = value ?? string.Empty;
            Avisar();
            Avisar(nameof(ResumenRespuesta));
            Avisar(nameof(EstadoSeguimiento));
        }
    }

    public DateTimeOffset? FechaRespuesta { get; set; }

    [JsonIgnore]
    public string FechaTexto => Fecha.ToLocalTime().ToString("dd/MM/yyyy HH:mm");

    [JsonIgnore]
    public string TipoTexto => Tipo switch
    {
        TipoReporte.EquipoElectronico => "Equipo electrónico",
        TipoReporte.Equipo => "Equipo",
        TipoReporte.Recurso => "Recurso",
        _ => "Insumo"
    };

    [JsonIgnore]
    public string EstadoTexto => Estado switch
    {
        EstadoReporte.Aceptada => "Aceptada",
        EstadoReporte.Denegada => "Denegada",
        _ => "Pendiente"
    };

    [JsonIgnore]
    public string Resumen => $"{TipoTexto} · {Area}";

    [JsonIgnore]
    public string Detalle
    {
        get
        {
            var precio = PrecioReposicion > 0 ? " · " + PrecioReposicion.ToString("C", Cultura) : string.Empty;
            var enlace = string.IsNullOrWhiteSpace(Enlace) ? string.Empty : " · " + Enlace.Trim();
            return $"{Nombre} · {Cantidad.ToString("0.##", Cultura)} {Unidad}{precio}{enlace}";
        }
    }

    [JsonIgnore]
    public string EstadoSeguimiento
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(EstadoOrden))
            {
                return EstadoOrden;
            }

            return Estado switch
            {
                EstadoReporte.Denegada => "Denegada",
                EstadoReporte.Aceptada => "Aceptada · sin orden",
                _ => "Pendiente"
            };
        }
    }

    public string ResumenRespuesta => EstadoSeguimiento;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Avisar([CallerMemberName] string? nombre = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombre));
}
