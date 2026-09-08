namespace PuntoDeVenta.Domain.Entities.Finanzas;

public enum TipoGasto
{
    Fijo = 1,              // Alquiler, internet, luz, monotributo
    Variable = 2,          // Limpieza, mantenimiento, viáticos
    Packaging = 3,         // Bolsas, cajas, etiquetas de presentación
    Envio = 4,             // Fletes, motomensajería, envíos por correo
    Merma = 5,             // Pérdida por rotura o falla insalvable
    RetiroPropietario = 6  // Sueldo del emprendedor / retiros personales del dueño
}
