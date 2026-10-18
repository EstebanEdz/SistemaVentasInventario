using Inventario.Web.Data;
using Inventario.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Web.Controllers;

[Authorize(Roles = Roles.AdminAlmacen)]
public class MovimientosController : Controller
{
    private readonly AppDbContext _db;

    public MovimientosController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(MovimientosVm filtro)
    {
        var query = _db.Movimientos.Include(m => m.Producto).AsQueryable();

        if (filtro.ProductoId.HasValue)
        {
            var productoId = filtro.ProductoId.Value;
            query = query.Where(m => m.ProductoId == productoId);
        }
        if (filtro.Tipo.HasValue)
        {
            var tipo = filtro.Tipo.Value;
            query = query.Where(m => m.Tipo == tipo);
        }
        if (filtro.Desde.HasValue)
        {
            var desde = filtro.Desde.Value.Date;
            query = query.Where(m => m.Fecha >= desde);
        }
        if (filtro.Hasta.HasValue)
        {
            var hasta = filtro.Hasta.Value.Date.AddDays(1);
            query = query.Where(m => m.Fecha < hasta);
        }

        filtro.Items = await query.OrderByDescending(m => m.Fecha).ThenByDescending(m => m.Id).Take(300).ToListAsync();

        var productos = await _db.Productos.OrderBy(p => p.Nombre).ToListAsync();
        filtro.Productos = productos.Select(p => new SelectListItem($"{p.Codigo} - {p.Nombre}", p.Id.ToString())).ToList();

        return View(filtro);
    }
}
