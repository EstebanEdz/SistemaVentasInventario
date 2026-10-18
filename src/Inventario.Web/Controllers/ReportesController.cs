using System.Globalization;
using System.Text;
using Inventario.Web.Data;
using Inventario.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Web.Controllers;

[Authorize(Roles = Roles.Admin)]
public class ReportesController : Controller
{
    private static readonly CultureInfo CulturaCsv = new("es-CO");

    private readonly AppDbContext _db;

    public ReportesController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Ventas(DateTime? desde, DateTime? hasta) =>
        View(await ConstruirReporteAsync(desde, hasta));

    public async Task<IActionResult> Inventario() =>
        View(await _db.Productos.Include(p => p.Categoria).OrderBy(p => p.Nombre).ToListAsync());

    public async Task<IActionResult> ExportarVentas(DateTime? desde, DateTime? hasta)
    {
        var d = (desde ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)).Date;
        var fin = (hasta ?? DateTime.Today).Date.AddDays(1);

        var ventas = await _db.Ventas.Include(v => v.Cliente).Include(v => v.Factura)
            .Where(v => v.Fecha >= d && v.Fecha < fin)
            .OrderBy(v => v.Fecha).ToListAsync();

        var filas = new List<string[]> { new[] { "Factura", "Fecha", "Cliente", "Subtotal", "IVA", "Total", "Usuario" } };
        filas.AddRange(ventas.Select(v => new[]
        {
            v.Factura?.Numero ?? "",
            v.Fecha.ToString("yyyy-MM-dd HH:mm"),
            v.Cliente?.Nombre ?? "Cliente general",
            v.Subtotal.ToString("0.00", CulturaCsv),
            v.Impuesto.ToString("0.00", CulturaCsv),
            v.Total.ToString("0.00", CulturaCsv),
            v.Usuario
        }));
        return Csv("ventas.csv", filas);
    }

    public async Task<IActionResult> ExportarInventario()
    {
        var productos = await _db.Productos.Include(p => p.Categoria).OrderBy(p => p.Nombre).ToListAsync();

        var filas = new List<string[]> { new[] { "Código", "Producto", "Categoría", "Stock", "Stock mínimo", "Costo", "Valor en inventario", "Estado" } };
        filas.AddRange(productos.Select(p => new[]
        {
            p.Codigo,
            p.Nombre,
            p.Categoria?.Nombre ?? "",
            p.StockActual.ToString(),
            p.StockMinimo.ToString(),
            p.PrecioCompra.ToString("0.00", CulturaCsv),
            (p.StockActual * p.PrecioCompra).ToString("0.00", CulturaCsv),
            !p.Activo ? "Inactivo" : p.StockBajo ? "Stock bajo" : "OK"
        }));
        return Csv("inventario.csv", filas);
    }

    private async Task<ReporteVentasVm> ConstruirReporteAsync(DateTime? desde, DateTime? hasta)
    {
        var d = (desde ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)).Date;
        var h = (hasta ?? DateTime.Today).Date;
        var fin = h.AddDays(1);

        var ventas = await _db.Ventas.Where(v => v.Fecha >= d && v.Fecha < fin).ToListAsync();
        var detalles = await _db.DetallesVenta
            .Where(x => x.Venta!.Fecha >= d && x.Venta.Fecha < fin)
            .Select(x => new { Nombre = x.Producto!.Nombre, x.Cantidad, x.PrecioUnitario })
            .ToListAsync();

        var total = ventas.Sum(v => v.Total);

        return new ReporteVentasVm
        {
            Desde = d,
            Hasta = h,
            NumeroVentas = ventas.Count,
            TotalVentas = total,
            TicketPromedio = ventas.Count == 0 ? 0 : total / ventas.Count,
            PorDia = ventas.GroupBy(v => v.Fecha.Date).OrderBy(g => g.Key)
                .Select(g => new VentaDiaria(g.Key.ToString("yyyy-MM-dd"), g.Sum(v => v.Total))).ToList(),
            TopProductos = detalles.GroupBy(x => x.Nombre)
                .Select(g => new ProductoVendido(g.Key, g.Sum(x => x.Cantidad), g.Sum(x => x.Cantidad * x.PrecioUnitario)))
                .OrderByDescending(x => x.Cantidad).Take(10).ToList()
        };
    }

    private static FileContentResult Csv(string nombre, IEnumerable<string[]> filas)
    {
        var sb = new StringBuilder();
        foreach (var fila in filas)
            sb.AppendLine(string.Join(";", fila.Select(Escapar)));

        // BOM UTF-8 para que Excel respete tildes y eñes
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return new FileContentResult(bytes, "text/csv") { FileDownloadName = nombre };
    }

    private static string Escapar(string valor) =>
        valor.Contains(';') || valor.Contains('"') || valor.Contains('\n')
            ? "\"" + valor.Replace("\"", "\"\"") + "\""
            : valor;
}
