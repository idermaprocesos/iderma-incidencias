using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Google.Cloud.Firestore;
using Iderma.Core.Services;
using IdermaIncidencias.Models;
using Microsoft.UI.Dispatching;

namespace IdermaIncidencias.Services;

public static class ReportesNube
{
    public const string Coleccion = "solicitudes-incidencias";

    private static readonly JsonSerializerOptions JsonOpciones = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private static FirestoreDb? _base;
    private static FirestoreChangeListener? _escucha;
    private static int _iniciado;
    private static DispatcherQueue? _ui;

    public static ObservableCollection<Reporte> Items { get; } = [];

    public static event Action? Cambiado;

    public static string RutaLocal { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "IdermaCapilar",
        "incidencias-reportes.json");

    public static void CargarLocal()
    {
        Items.Clear();
        if (!File.Exists(RutaLocal))
        {
            return;
        }

        try
        {
            var lista = JsonSerializer.Deserialize<List<Reporte>>(File.ReadAllText(RutaLocal), JsonOpciones);
            if (lista is null)
            {
                return;
            }

            foreach (var item in lista.OrderByDescending(r => r.Fecha))
            {
                Items.Add(item);
            }
        }
        catch
        {
        }
    }

    public static void IniciarEscucha(DispatcherQueue cola)
    {
        _ui = cola;
        if (Interlocked.Exchange(ref _iniciado, 1) == 1)
        {
            return;
        }

        _ = EscucharAsync();
    }

    public static async Task PublicarAsync(Reporte reporte)
    {
        await AsegurarBaseAsync();
        if (_base is null)
        {
            throw new InvalidOperationException("No se encontraron las credenciales de Firebase.");
        }

        var datos = new Dictionary<string, object>
        {
            ["Folio"] = reporte.Folio,
            ["Usuario id"] = reporte.UsuarioId,
            ["Solicitante"] = reporte.Solicitante,
            ["Área"] = reporte.Area,
            ["Tipo"] = reporte.Tipo.ToString(),
            ["Nombre"] = reporte.Nombre,
            ["Enlace"] = reporte.Enlace,
            ["Detalle"] = reporte.DetalleTexto,
            ["Unidad"] = reporte.Unidad,
            ["Cantidad"] = reporte.Cantidad,
            ["Precio reposición"] = reporte.PrecioReposicion,
            ["Urgente"] = reporte.Urgente,
            ["Fecha"] = reporte.Fecha.ToString("o", CultureInfo.InvariantCulture),
            ["Estado"] = "Pendiente",
            ["Motivo"] = string.Empty,
            ["Fecha respuesta"] = string.Empty,
            ["Origen"] = "incidencias",
            ["Actualizado"] = FieldValue.ServerTimestamp
        };

        await _base.Collection(Coleccion).Document(reporte.Id).SetAsync(datos);
        Items.Insert(0, reporte);
        GuardarLocal();
        Cambiado?.Invoke();
    }

    private static async Task EscucharAsync()
    {
        try
        {
            await AsegurarBaseAsync();
            if (_base is null)
            {
                return;
            }

            var guardado = CursorNube.Cargar(Coleccion + "-respuestas");
            var desde = guardado ?? CursorNube.Epoca;
            _escucha = _base.Collection(Coleccion)
                .WhereGreaterThan("Actualizado", desde)
                .OrderBy("Actualizado")
                .Listen(snapshot =>
                {
                    Aplicar(snapshot);
                    CursorNube.GuardarSiAvanza(Coleccion + "-respuestas", snapshot.ReadTime);
                });
        }
        catch
        {
        }
    }

    private static void Aplicar(QuerySnapshot snapshot)
    {
        var cambios = new List<Reporte>();
        foreach (var cambio in snapshot.Changes)
        {
            if (cambio.ChangeType == DocumentChange.Type.Removed || !cambio.Document.Exists)
            {
                continue;
            }

            var leido = Leer(cambio.Document);
            if (leido is not null)
            {
                cambios.Add(leido);
            }
        }

        if (cambios.Count == 0)
        {
            return;
        }

        var cola = _ui;
        if (cola is null || cola.HasThreadAccess)
        {
            Fusionar(cambios);
            return;
        }

        cola.TryEnqueue(() => Fusionar(cambios));
    }

    private static void Fusionar(IReadOnlyList<Reporte> cambios)
    {
        foreach (var cambio in cambios)
        {
            var actual = Items.FirstOrDefault(r => r.Id.Equals(cambio.Id, StringComparison.OrdinalIgnoreCase));
            if (actual is null)
            {
                continue;
            }

            actual.Estado = cambio.Estado;
            actual.EstadoOrden = cambio.EstadoOrden;
            actual.Motivo = cambio.Motivo;
            actual.FechaRespuesta = cambio.FechaRespuesta;
        }

        GuardarLocal();
        Cambiado?.Invoke();
    }

    private static void GuardarLocal()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(RutaLocal)!);
        File.WriteAllText(RutaLocal, JsonSerializer.Serialize(Items.ToList(), JsonOpciones));
    }

    private static Reporte? Leer(DocumentSnapshot doc)
    {
        var datos = doc.ToDictionary();
        var estado = Texto(datos, "Estado").ToLowerInvariant() switch
        {
            "aceptada" => EstadoReporte.Aceptada,
            "denegada" => EstadoReporte.Denegada,
            _ => EstadoReporte.Pendiente
        };
        return new Reporte
        {
            Id = doc.Id,
            Estado = estado,
            EstadoOrden = TextoEstadoOrden(Texto(datos, "Estado orden")),
            Motivo = Texto(datos, "Motivo"),
            FechaRespuesta = DateTimeOffset.TryParse(Texto(datos, "Fecha respuesta"), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var fecha)
                ? fecha
                : null
        };
    }

    private static async Task AsegurarBaseAsync()
    {
        _base ??= await ConexionFirebase.AbrirAsync();
    }

    private static string TextoEstadoOrden(string texto) => texto.Trim().ToLowerInvariant() switch
    {
        "borrador" => "Borrador",
        "en revisión" or "en revision" or "revisión" or "revision" => "En revisión",
        "aprobada" or "aprobado" => "Aprobada",
        _ => texto.Trim()
    };

    private static string Texto(IDictionary<string, object> datos, string clave) =>
        datos.TryGetValue(clave, out var valor)
            ? Convert.ToString(valor, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty
            : string.Empty;
}
