namespace PuntoDeVenta.Application.DTOs.Asistente;

public class BorradorVarianteDto
{
    public string Sku { get; set; } = string.Empty;
    public string Talle { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int Cantidad { get; set; } = 1;
    public decimal PrecioCosto { get; set; }
    public decimal PrecioLista { get; set; }
    public decimal? PrecioOferta { get; set; }
}

public class BorradorCargaArticuloDto
{
    public string NombreArticulo { get; set; } = string.Empty;
    public string CodigoEstilo { get; set; } = string.Empty;
    public string CategoriaNombre { get; set; } = "Indumentaria";
    public string MarcaNombre { get; set; } = "Genérica";
    public string Temporada { get; set; } = "Todo el año";
    public string Genero { get; set; } = "Unisex";

    public List<string> Talles { get; set; } = new();
    public List<string> Colores { get; set; } = new();

    public int CantidadTotal { get; set; }
    public int CantidadPorVariante { get; set; } = 1;

    public decimal PrecioCosto { get; set; }
    public decimal PrecioLista { get; set; }
    public decimal? PrecioOferta { get; set; }

    public List<BorradorVarianteDto> Variantes { get; set; } = new();
    public List<string> DatosFaltantes { get; set; } = new();

    public int TotalVariantes => Variantes.Count;
    public int TotalPrendas => Variantes.Count > 0 ? Variantes.Sum(v => v.Cantidad) : (CantidadTotal > 0 ? CantidadTotal : CantidadPorVariante * Math.Max(1, Talles.Count * Math.Max(1, Colores.Count)));
    public string DescripcionEstado => EsValidoParaGuardar ? "Listo para guardar" : $"Faltan datos ({DatosFaltantes.Count})";

    public bool EsValidoParaGuardar =>
        !string.IsNullOrWhiteSpace(NombreArticulo) &&
        Talles.Count > 0 &&
        Colores.Count > 0 &&
        PrecioCosto > 0 &&
        PrecioLista > 0 &&
        Variantes.Count > 0;

    public void GenerarMatrizVariantes()
    {
        Variantes.Clear();
        if (Talles.Count == 0 || Colores.Count == 0) return;

        // Limpiar prefijo para SKU
        string prefijo = GenerarPrefijoSku(NombreArticulo);

        int totalCombinaciones = Talles.Count * Colores.Count;
        int cantidadAsignada = CantidadPorVariante;
        if (CantidadTotal > 0 && totalCombinaciones > 0)
        {
            cantidadAsignada = (int)Math.Ceiling((double)CantidadTotal / totalCombinaciones);
            if (cantidadAsignada <= 0) cantidadAsignada = 1;
        }

        foreach (var talle in Talles)
        {
            foreach (var color in Colores)
            {
                string sku = $"{prefijo}-{talle.ToUpper().Replace(" ", "")}-{color.ToUpper().Substring(0, Math.Min(3, color.Length))}";
                Variantes.Add(new BorradorVarianteDto
                {
                    Sku = sku,
                    Talle = talle,
                    Color = color,
                    Cantidad = cantidadAsignada,
                    PrecioCosto = PrecioCosto,
                    PrecioLista = PrecioLista,
                    PrecioOferta = PrecioOferta
                });
            }
        }
    }

    public static string GenerarPrefijoSku(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre)) return "ART";
        var palabras = nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (palabras.Length == 1)
        {
            return palabras[0].Substring(0, Math.Min(6, palabras[0].Length)).ToUpper();
        }
        var p1 = palabras[0].Substring(0, Math.Min(3, palabras[0].Length)).ToUpper();
        var p2 = palabras[1].Substring(0, Math.Min(3, palabras[1].Length)).ToUpper();
        return $"{p1}{p2}";
    }
}
