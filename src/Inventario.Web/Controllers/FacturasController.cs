using Inventario.Web.Data;
using Inventario.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Web.Controllers;

[Authorize(Roles = Roles.AdminVentas)]
public class FacturasController : Controller
{
    private readonly AppDbContext _db;

    public FacturasController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index() =>
        View(await _db.Facturas.Include(f => f.Venta).ThenInclude(v => v!.Cliente)
            .OrderByDescending(f => f.Fecha).Take(200).ToListAsync());

    public async Task<IActionResult> Details(int id)
    {
        var factura = await _db.Facturas
            .Include(f => f.Venta).ThenInclude(v => v!.Cliente)
            .Include(f => f.Venta).ThenInclude(v => v!.Detalles).ThenInclude(d => d.Producto)
            .FirstOrDefaultAsync(f => f.Id == id);
        if (factura == null) return NotFound();
        return View(factura);
    }
}
