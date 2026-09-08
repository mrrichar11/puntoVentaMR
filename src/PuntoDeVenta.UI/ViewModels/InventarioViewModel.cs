using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PuntoDeVenta.Application.Contracts;
using PuntoDeVenta.Application.DTOs.Inventario;
using PuntoDeVenta.Application.Services;
using PuntoDeVenta.Domain.Entities.Catalogo;
using PuntoDeVenta.Domain.Entities.Inventario;

namespace PuntoDeVenta.UI.ViewModels;

public partial class InventarioViewModel : ObservableObject
{
    private readonly IInventarioService _inventarioService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguracionService _configuracionService;
    private readonly IBarcodeService _barcodeService;

    // --- Listado y Búsqueda de Variantes ---
    [ObservableProperty]
    private ObservableCollection<VarianteArticuloDto> _variantes = new();

    [ObservableProperty]
    private VarianteArticuloDto? _varianteSeleccionada;

    [ObservableProperty]
    private string _filtroTexto = string.Empty;

    [ObservableProperty]
    private string _mensaje = string.Empty;

    [ObservableProperty]
    private bool _esMensajeError;

    // --- Maestros para Formulario ---
    [ObservableProperty]
    private ObservableCollection<Categoria> _categorias = new();

    [ObservableProperty]
    private Categoria? _categoriaSeleccionada;

    [ObservableProperty]
    private ObservableCollection<Marca> _marcas = new();

    [ObservableProperty]
    private Marca? _marcaSeleccionada;

    [ObservableProperty]
    private ObservableCollection<Articulo> _articulosExistentes = new();

    [ObservableProperty]
    private Articulo? _articuloSeleccionadoParaPrecios;

    // --- Formulario Nuevo Artículo con Matriz ---
    [ObservableProperty]
    private string _codigoEstilo = string.Empty;

    [ObservableProperty]
    private string _nombreArticulo = string.Empty;

    [ObservableProperty]
    private string _codigoBarrasNuevo = string.Empty;

    [ObservableProperty]
    private string _codigoBarrasEscaneoRapido = string.Empty;

    [ObservableProperty]
    private string _temporada = "Todo el año";

    [ObservableProperty]
    private string _genero = "Unisex";

    [ObservableProperty]
    private decimal _precioCosto = 10000m;

    [ObservableProperty]
    private decimal _precioLista = 25000m;

    [ObservableProperty]
    private decimal? _precioOferta;

    [ObservableProperty]
    private bool _permiteDescuentoMedioPago = true;

    // --- Parámetros de Precios Sugeridos ---
    [ObservableProperty]
    private decimal _margenGananciaSugeridoConfig = 80.0m;

    [ObservableProperty]
    private decimal _costosBancariosEstimadosConfig = 5.0m;

    [ObservableProperty]
    private decimal _precioSugerido;

    [ObservableProperty]
    private string _textoPrecioSugerido = string.Empty;

    [ObservableProperty]
    private string _tallesInput = "S, M, L, XL";

    [ObservableProperty]
    private string _coloresInput = "Negro, Blanco";

    [ObservableProperty]
    private int _stockInicialDefecto = 5;

    [ObservableProperty]
    private int _stockMinimo = 2;

    // --- Formulario Ajuste de Stock ---
    [ObservableProperty]
    private int _cantidadAjuste = 10;

    [ObservableProperty]
    private TipoMovimientoStock _tipoMovimientoAjuste = TipoMovimientoStock.EntradaCompra;

    [ObservableProperty]
    private string _motivoAjuste = "Recepción de mercadería";

    [ObservableProperty]
    private string _comprobanteAjuste = string.Empty;

    // --- Edición de Variante Individual ---
    [ObservableProperty]
    private string _editNombreArticulo = string.Empty;

    [ObservableProperty]
    private string _editSku = string.Empty;

    [ObservableProperty]
    private string _editCodigoBarras = string.Empty;

    [ObservableProperty]
    private string _editTalle = string.Empty;

    [ObservableProperty]
    private string _editColor = string.Empty;

    [ObservableProperty]
    private decimal _editPrecioCosto;

    [ObservableProperty]
    private decimal _editPrecioLista;

    [ObservableProperty]
    private decimal? _editPrecioOferta;

    [ObservableProperty]
    private bool _editPermiteDescuentoMedioPago;

    [ObservableProperty]
    private int _editStockMinimo;

    [ObservableProperty]
    private decimal _editPrecioSugerido;

    [ObservableProperty]
    private string _textoEditPrecioSugerido = string.Empty;

    // --- Actualización Masiva de Precios por Artículo ---
    [ObservableProperty]
    private decimal? _masivoAumentoListaPorcentaje;

    [ObservableProperty]
    private decimal? _masivoNuevoPrecioLista;

    [ObservableProperty]
    private decimal? _masivoAumentoCostoPorcentaje;

    [ObservableProperty]
    private decimal? _masivoNuevoPrecioCosto;

    [ObservableProperty]
    private decimal? _masivoNuevoPrecioOferta;

    [ObservableProperty]
    private bool _masivoPermiteDescuento = true;

    // --- Alta Rápida de Categoría y Marca ---
    [ObservableProperty]
    private string _nuevaCategoriaNombre = string.Empty;

    [ObservableProperty]
    private string _nuevaMarcaNombre = string.Empty;


    private string _ultimaSugerenciaSku = string.Empty;
    private decimal _descuentoEfectivoConfig = 10m;
    private string _nombreComercioConfig = "MR. SYS";

    public InventarioViewModel(
        IInventarioService inventarioService,
        IUnitOfWork unitOfWork,
        IConfiguracionService configuracionService,
        IBarcodeService barcodeService)
    {
        _inventarioService = inventarioService ?? throw new ArgumentNullException(nameof(inventarioService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _configuracionService = configuracionService ?? throw new ArgumentNullException(nameof(configuracionService));
        _barcodeService = barcodeService ?? throw new ArgumentNullException(nameof(barcodeService));
    }

    public async Task CargarDatosAsync()
    {
        try
        {
            var config = await _configuracionService.ObtenerConfiguracionAsync();
            MargenGananciaSugeridoConfig = config.MargenGananciaSugerido;
            CostosBancariosEstimadosConfig = config.CostosBancariosEstimados;
            _nombreComercioConfig = !string.IsNullOrWhiteSpace(config.NombreComercio) ? config.NombreComercio : "MR. SYS";
            _descuentoEfectivoConfig = config.PorcentajeDescuentoEfectivo;
            RecalcularPrecioSugerido();

            await RecargarMaestrosAsync();
            await FiltrarVariantesAsync();
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al cargar inventario: {ex.Message}", true);
        }
    }

    partial void OnNombreArticuloChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(CodigoEstilo) || CodigoEstilo == _ultimaSugerenciaSku)
        {
            SugerirCodigoEstilo();
        }
    }

    partial void OnCategoriaSeleccionadaChanged(Categoria? value)
    {
        if (CodigoEstilo == _ultimaSugerenciaSku && !string.IsNullOrWhiteSpace(NombreArticulo))
        {
            SugerirCodigoEstilo();
        }
    }

    partial void OnMarcaSeleccionadaChanged(Marca? value)
    {
        if (CodigoEstilo == _ultimaSugerenciaSku && !string.IsNullOrWhiteSpace(NombreArticulo))
        {
            SugerirCodigoEstilo();
        }
    }

    [RelayCommand]
    public void SugerirCodigoEstilo()
    {
        if (string.IsNullOrWhiteSpace(NombreArticulo)) return;
        var sugerencia = _barcodeService.SugerirSku(NombreArticulo, CategoriaSeleccionada?.Nombre, MarcaSeleccionada?.Nombre);
        _ultimaSugerenciaSku = sugerencia;
        CodigoEstilo = sugerencia;
    }

    partial void OnPrecioCostoChanged(decimal value)
    {
        RecalcularPrecioSugerido();
    }

    partial void OnEditPrecioCostoChanged(decimal value)
    {
        RecalcularEditPrecioSugerido();
    }

    public void RecalcularPrecioSugerido()
    {
        PrecioSugerido = CalcularPrecioSugerido(PrecioCosto);
        TextoPrecioSugerido = PrecioSugerido > 0
            ? $"💡 Sugerido: ${PrecioSugerido:N0} (+{MargenGananciaSugeridoConfig:G29}% ganancia + {CostosBancariosEstimadosConfig:G29}% bancario)"
            : string.Empty;
    }

    public void RecalcularEditPrecioSugerido()
    {
        EditPrecioSugerido = CalcularPrecioSugerido(EditPrecioCosto);
        TextoEditPrecioSugerido = EditPrecioSugerido > 0
            ? $"💡 Sugerido: ${EditPrecioSugerido:N0}"
            : string.Empty;
    }

    public decimal CalcularPrecioSugerido(decimal costo)
    {
        if (costo <= 0) return 0m;
        var factorMargen = 1m + (MargenGananciaSugeridoConfig / 100m);
        var factorBancario = 1m + (CostosBancariosEstimadosConfig / 100m);
        var sugerido = costo * factorMargen * factorBancario;
        return Math.Round(sugerido, 0);
    }

    [RelayCommand]
    public void AplicarPrecioSugerido()
    {
        if (PrecioSugerido > 0)
        {
            PrecioLista = PrecioSugerido;
            MostrarMensaje($"¡Precio de lista actualizado a ${PrecioLista:N0} según precio sugerido!", false);
        }
    }

    [RelayCommand]
    public void AplicarEditPrecioSugerido()
    {
        if (EditPrecioSugerido > 0)
        {
            EditPrecioLista = EditPrecioSugerido;
            MostrarMensaje($"¡Precio de lista editado a ${EditPrecioLista:N0} según precio sugerido!", false);
        }
    }

    private async Task RecargarMaestrosAsync()
    {
        var categorias = await _unitOfWork.Categorias.GetAllAsync();
        Categorias.Clear();
        foreach (var c in categorias.OrderBy(x => x.Nombre)) Categorias.Add(c);
        if (Categorias.Count > 0 && CategoriaSeleccionada == null) CategoriaSeleccionada = Categorias[0];

        var marcas = await _unitOfWork.Marcas.GetAllAsync();
        Marcas.Clear();
        foreach (var m in marcas.OrderBy(x => x.Nombre)) Marcas.Add(m);
        if (Marcas.Count > 0 && MarcaSeleccionada == null) MarcaSeleccionada = Marcas[0];

        var articulos = await _unitOfWork.Articulos.GetAllAsync();
        ArticulosExistentes.Clear();
        foreach (var a in articulos.Where(x => x.Activo).OrderBy(x => x.Nombre)) ArticulosExistentes.Add(a);
        if (ArticulosExistentes.Count > 0 && ArticuloSeleccionadoParaPrecios == null) ArticuloSeleccionadoParaPrecios = ArticulosExistentes[0];
    }

    [RelayCommand]
    public async Task FiltrarVariantesAsync()
    {
        try
        {
            var resultados = await _inventarioService.BuscarVariantesAsync(FiltroTexto);
            Variantes.Clear();
            foreach (var v in resultados)
            {
                Variantes.Add(v);
            }

            if (VarianteSeleccionada == null && Variantes.Count > 0)
            {
                VarianteSeleccionada = Variantes[0];
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al buscar: {ex.Message}", true);
        }
    }

    partial void OnVarianteSeleccionadaChanged(VarianteArticuloDto? value)
    {
        if (value != null)
        {
            EditNombreArticulo = value.NombreArticulo;
            EditSku = value.SKU;
            EditCodigoBarras = value.CodigoBarras ?? string.Empty;
            EditTalle = value.Talle;
            EditColor = value.Color;
            EditPrecioCosto = value.PrecioCosto;
            EditPrecioLista = value.PrecioLista;
            EditPrecioOferta = value.PrecioOferta;
            EditPermiteDescuentoMedioPago = value.PermiteDescuentoMedioPago;
            EditStockMinimo = value.StockMinimo;
        }
    }

    [RelayCommand]
    public async Task GuardarEdicionVarianteAsync()
    {
        if (VarianteSeleccionada == null)
        {
            MostrarMensaje("Seleccione una variante para editar.", true);
            return;
        }

        var idModificado = VarianteSeleccionada.Id;

        try
        {
            var dto = new ActualizarVarianteDto
            {
                VarianteId = idModificado,
                NombreArticulo = string.IsNullOrWhiteSpace(EditNombreArticulo) ? null : EditNombreArticulo.Trim(),
                SKU = EditSku.Trim(),
                CodigoBarras = string.IsNullOrWhiteSpace(EditCodigoBarras) ? null : EditCodigoBarras.Trim(),
                Talle = EditTalle.Trim(),
                Color = EditColor.Trim(),
                PrecioCosto = EditPrecioCosto,
                PrecioLista = EditPrecioLista,
                PrecioOferta = EditPrecioOferta,
                PermiteDescuentoMedioPago = EditPermiteDescuentoMedioPago,
                StockMinimo = EditStockMinimo
            };

            await _inventarioService.ActualizarVarianteAsync(dto);
            MostrarMensaje($"¡Producto / Variante '{EditSku}' actualizado con éxito!", false);

            await FiltrarVariantesAsync();

            // Mantener la variante seleccionada tras recargar la lista
            VarianteSeleccionada = Variantes.FirstOrDefault(v => v.Id == idModificado) ?? (Variantes.Count > 0 ? Variantes[0] : null);
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al actualizar variante: {ex.Message}", true);
        }
    }

    [RelayCommand]
    public async Task EliminarVarianteSeleccionadaAsync()
    {
        if (VarianteSeleccionada == null)
        {
            MostrarMensaje("Seleccione una variante de la lista para eliminar.", true);
            return;
        }

        var confirmacion = System.Windows.MessageBox.Show(
            $"¿Está seguro de que desea eliminar la variante '{VarianteSeleccionada.NombreArticulo} - {VarianteSeleccionada.Talle} / {VarianteSeleccionada.Color}' (SKU: {VarianteSeleccionada.SKU})?\n\n" +
            "Si no tiene ventas registradas, se eliminará permanentemente. Si tiene historial contable, se dará de baja del catálogo activo.",
            "Confirmar Eliminación de Variante",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (confirmacion != System.Windows.MessageBoxResult.Yes) return;

        try
        {
            var fueFisica = await _inventarioService.EliminarVarianteAsync(VarianteSeleccionada.Id);
            var msg = fueFisica
                ? $"¡Variante SKU '{VarianteSeleccionada.SKU}' eliminada permanentemente del sistema!"
                : $"¡Variante SKU '{VarianteSeleccionada.SKU}' dada de baja del catálogo activo (se preserva su histórico contable)!";

            MostrarMensaje(msg, false);
            VarianteSeleccionada = null;
            await FiltrarVariantesAsync();
            await RecargarMaestrosAsync();
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al eliminar variante: {ex.Message}", true);
        }
    }

    [RelayCommand]
    public async Task EliminarVarianteFilaAsync(VarianteArticuloDto? variante)
    {
        if (variante == null) return;
        VarianteSeleccionada = variante;
        await EliminarVarianteSeleccionadaAsync();
    }

    [RelayCommand]
    public async Task EliminarArticuloCompletoAsync()
    {
        if (VarianteSeleccionada == null)
        {
            MostrarMensaje("Seleccione una variante para identificar qué artículo desea eliminar.", true);
            return;
        }

        var nombreArticulo = VarianteSeleccionada.NombreArticulo;
        var articuloId = VarianteSeleccionada.ArticuloId;

        var confirmacion = System.Windows.MessageBox.Show(
            $"⚠️ ¡ATENCIÓN!\n\n¿Está seguro de que desea eliminar TODO el artículo '{nombreArticulo}' y TODAS sus variantes asociadas?\n\n" +
            "Esta acción retirará el modelo completo del catálogo de ventas.",
            "Confirmar Eliminación Total de Artículo",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (confirmacion != System.Windows.MessageBoxResult.Yes) return;

        try
        {
            int variantesEliminadas = await _inventarioService.EliminarArticuloCompletoAsync(articuloId);
            MostrarMensaje($"¡Artículo '{nombreArticulo}' y sus {variantesEliminadas} variantes fueron eliminados del catálogo!", false);

            VarianteSeleccionada = null;
            await FiltrarVariantesAsync();
            await RecargarMaestrosAsync();
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al eliminar artículo completo: {ex.Message}", true);
        }
    }

    [RelayCommand]
    public async Task AplicarActualizacionPreciosMasivaAsync()
    {
        if (ArticuloSeleccionadoParaPrecios == null)
        {
            MostrarMensaje("Seleccione el artículo/modelo a actualizar.", true);
            return;
        }

        try
        {
            var dto = new ActualizarPreciosMasivosDto
            {
                ArticuloId = ArticuloSeleccionadoParaPrecios.Id,
                NuevoPrecioCosto = MasivoNuevoPrecioCosto,
                PorcentajeAumentoCosto = MasivoAumentoCostoPorcentaje,
                NuevoPrecioLista = MasivoNuevoPrecioLista,
                PorcentajeAumentoLista = MasivoAumentoListaPorcentaje,
                NuevoPrecioOferta = MasivoNuevoPrecioOferta,
                PermiteDescuentoMedioPago = MasivoPermiteDescuento
            };

            var total = await _inventarioService.ActualizarPreciosMasivosAsync(dto);
            MostrarMensaje($"¡Precios actualizados en bloque para {total} variantes del artículo '{ArticuloSeleccionadoParaPrecios.Nombre}'!", false);

            await FiltrarVariantesAsync();
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al actualizar precios masivos: {ex.Message}", true);
        }
    }

    [RelayCommand]
    public async Task AgregarNuevaCategoriaAsync()
    {
        if (string.IsNullOrWhiteSpace(NuevaCategoriaNombre))
        {
            MostrarMensaje("Escriba el nombre de la nueva categoría.", true);
            return;
        }

        try
        {
            var cat = await _inventarioService.CrearCategoriaAsync(NuevaCategoriaNombre);
            NuevaCategoriaNombre = string.Empty;
            await RecargarMaestrosAsync();
            CategoriaSeleccionada = Categorias.FirstOrDefault(c => c.Id == cat.Id) ?? CategoriaSeleccionada;
            MostrarMensaje($"¡Categoría '{cat.Nombre}' agregada con éxito!", false);
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al agregar categoría: {ex.Message}", true);
        }
    }

    [RelayCommand]
    public async Task AgregarNuevaMarcaAsync()
    {
        if (string.IsNullOrWhiteSpace(NuevaMarcaNombre))
        {
            MostrarMensaje("Escriba el nombre de la nueva marca.", true);
            return;
        }

        try
        {
            var m = await _inventarioService.CrearMarcaAsync(NuevaMarcaNombre);
            NuevaMarcaNombre = string.Empty;
            await RecargarMaestrosAsync();
            MarcaSeleccionada = Marcas.FirstOrDefault(x => x.Id == m.Id) ?? MarcaSeleccionada;
            MostrarMensaje($"¡Marca '{m.Nombre}' agregada con éxito!", false);
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al agregar marca: {ex.Message}", true);
        }
    }

    [RelayCommand]
    public void AplicarCurvaTalles(string tipoCurva)
    {
        switch (tipoCurva?.ToLowerInvariant())
        {
            case "letras":
                TallesInput = string.Join(", ", CurvasTallesPresets.IndumentariaLetrasBasica);
                break;
            case "letras_completas":
                TallesInput = string.Join(", ", CurvasTallesPresets.IndumentariaLetras);
                break;
            case "adultos_numeros":
            case "numeros":
                TallesInput = string.Join(", ", CurvasTallesPresets.AdultosNumeros);
                break;
            case "denim":
                TallesInput = "38, 40, 42, 44, 46, 48";
                break;
            case "calzado":
                TallesInput = "38, 39, 40, 41, 42, 43, 44";
                break;
            case "infantil":
                TallesInput = "4, 6, 8, 10, 12, 14";
                break;
            case "unico":
                TallesInput = "U";
                break;
            default:
                break;
        }

        MostrarMensaje($"Curva de talles '{tipoCurva}' aplicada.", false);
    }

    [RelayCommand]
    public async Task CrearArticuloMatrizAsync()
    {
        if (string.IsNullOrWhiteSpace(CodigoEstilo))
        {
            MostrarMensaje("Ingrese el código de estilo o modelo.", true);
            return;
        }

        if (string.IsNullOrWhiteSpace(NombreArticulo))
        {
            MostrarMensaje("Ingrese el nombre del artículo.", true);
            return;
        }

        if (CategoriaSeleccionada == null)
        {
            MostrarMensaje("Seleccione una categoría.", true);
            return;
        }

        if (MarcaSeleccionada == null)
        {
            MostrarMensaje("Seleccione una marca.", true);
            return;
        }

        var listaTalles = TallesInput
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim().ToUpperInvariant())
            .Distinct()
            .ToList();

        var listaColores = ColoresInput
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(c => c.Trim())
            .Distinct()
            .ToList();

        if (listaTalles.Count == 0)
        {
            MostrarMensaje("Especifique al menos un talle.", true);
            return;
        }

        if (listaColores.Count == 0)
        {
            MostrarMensaje("Especifique al menos un color.", true);
            return;
        }

        try
        {
            var dto = new CrearArticuloDto
            {
                CodigoEstilo = CodigoEstilo.Trim(),
                Nombre = NombreArticulo.Trim(),
                CategoriaId = CategoriaSeleccionada.Id,
                MarcaId = MarcaSeleccionada.Id,
                Temporada = Temporada,
                Genero = Genero,
                PrecioCosto = PrecioCosto,
                PrecioLista = PrecioLista,
                PrecioOferta = PrecioOferta,
                PermiteDescuentoMedioPago = PermiteDescuentoMedioPago,
                StockMinimo = StockMinimo,
                Talles = listaTalles,
                Colores = listaColores,
                StockInicialDefecto = StockInicialDefecto,
                CodigoBarrasUnico = string.IsNullOrWhiteSpace(CodigoBarrasNuevo) ? null : CodigoBarrasNuevo.Trim()
            };

            var nuevoArticulo = await _inventarioService.CrearArticuloConMatrizAsync(dto);
            var totalVariantes = nuevoArticulo.Variantes.Count;

            MostrarMensaje($"¡Artículo '{nuevoArticulo.Nombre}' creado con éxito! Se generaron {totalVariantes} variantes en inventario.", false);

            CodigoEstilo = string.Empty;
            NombreArticulo = string.Empty;
            CodigoBarrasNuevo = string.Empty;
            PrecioOferta = null;

            await RecargarMaestrosAsync();
            await FiltrarVariantesAsync();
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al crear artículo: {ex.Message}", true);
        }
    }

    [RelayCommand]
    public async Task BuscarPorCodigoBarrasAsync()
    {
        if (string.IsNullOrWhiteSpace(CodigoBarrasEscaneoRapido)) return;

        var codigo = CodigoBarrasEscaneoRapido.Trim();
        try
        {
            var encontrada = await _inventarioService.ObtenerVariantePorCodigoBarrasOSkuAsync(codigo);
            if (encontrada != null)
            {
                VarianteSeleccionada = Variantes.FirstOrDefault(v => v.Id == encontrada.Id) ?? encontrada;
                MostrarMensaje($"¡Producto localizado! {encontrada.NombreArticulo} - Talle: {encontrada.Talle} / Color: {encontrada.Color} (Stock: {encontrada.StockActual})", false);
            }
            else
            {
                CodigoBarrasNuevo = codigo;
                MostrarMensaje($"Código '{codigo}' no encontrado. Se precargó en el formulario para crear un nuevo producto con este código de barras de fábrica.", false);
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al buscar por código de barras: {ex.Message}", true);
        }
        finally
        {
            CodigoBarrasEscaneoRapido = string.Empty;
        }
    }

    [RelayCommand]
    public void ImprimirEtiquetas(VarianteArticuloDto? variante = null)
    {
        var item = variante ?? VarianteSeleccionada;
        if (item == null)
        {
            MostrarMensaje("Seleccione una variante de la grilla para imprimir sus etiquetas.", true);
            return;
        }

        try
        {
            var dlg = new Views.Inventario.ImprimirEtiquetasDialog(
                item,
                _nombreComercioConfig,
                _descuentoEfectivoConfig,
                _barcodeService)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };
            dlg.ShowDialog();
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al abrir diálogo de etiquetas: {ex.Message}", true);
        }
    }

    [RelayCommand]
    public async Task GuardarAjusteStockAsync()
    {
        if (VarianteSeleccionada == null)
        {
            MostrarMensaje("Seleccione una variante de la grilla para ajustar su stock.", true);
            return;
        }

        if (CantidadAjuste == 0)
        {
            MostrarMensaje("La cantidad a ajustar no puede ser cero.", true);
            return;
        }

        try
        {
            var dto = new AjustarStockDto
            {
                VarianteId = VarianteSeleccionada.Id,
                Cantidad = CantidadAjuste,
                Tipo = TipoMovimientoAjuste,
                Motivo = MotivoAjuste,
                ReferenciaDocumento = ComprobanteAjuste
            };

            var movimiento = await _inventarioService.AjustarStockAsync(dto);
            MostrarMensaje($"¡Stock de {VarianteSeleccionada.SKU} actualizado! Nuevo stock: {movimiento.StockResultante} (previo: {movimiento.StockPrevio}).", false);

            await FiltrarVariantesAsync();
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al ajustar stock: {ex.Message}", true);
        }
    }

    private void MostrarMensaje(string texto, bool esError)
    {
        Mensaje = texto;
        EsMensajeError = esError;
    }
}
