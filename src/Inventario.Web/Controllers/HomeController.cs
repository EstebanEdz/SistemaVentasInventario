using Inventario.Web.Data;
using Inventario.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Web.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;

    public HomeController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var hoy = DateTime.Now;
        var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);

        var vm = new DashboardVm
        {
            TotalProductos = await _db.Productos.CountAsync(p => p.Activo),
            ValorInventario = await _db.Productos.Where(p => p.Activo).SumAsync(p => p.StockActual * p.PrecioCompra),
            VentasMes = await _db.Ventas.Where(v => v.Fecha >= inicioMes).SumAsync(v => v.Total),
            ComprasMes = await _db.Compras.Where(c => c.Fecha >= inicioMes).SumAsync(c => c.Total),
            TotalStockBajo = await _db.Productos.CountAsync(p => p.Activo && p.StockActual <= p.StockMinimo),
            StockBajo = await _db.Productos.Include(p => p.Categoria)
                .Where(p => p.Activo && p.StockActual <= p.StockMinimo)
                .OrderBy(p => p.StockActual).Take(10).ToListAsync(),
            UltimosMovimientos = await _db.Movimientos.Include(m => m.Producto)
                .OrderByDescending(m => m.Fecha).ThenByDescending(m => m.Id).Take(8).ToListAsync()
        };
        return View(vm);
    }

    [AllowAnonymous]
    public IActionResult Error() => View();
}
