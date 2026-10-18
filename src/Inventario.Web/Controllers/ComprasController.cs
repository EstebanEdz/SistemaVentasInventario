using Inventario.Web.Data;
using Inventario.Web.Models;
using Inventario.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Web.Controllers;

[Authorize(Roles = Roles.AdminAlmacen)]
public class ComprasController : Controller
{
    private readonly AppDbContext _db;
    private readonly InventarioService _inventario;

    public ComprasController(AppDbContext db, InventarioService inventario)
    {
        _db = db;
        _inventario = inventario;
    }

    public async Task<IActionResult> Index() =>
        View(await _db.Compras.Include(c => c.Proveedor).OrderByDescending(c => c.Fecha).Take(200).ToListAsync());

    public async Task<IActionResult> Details(int id)
    {
        var compra = await _db.Compras
            .Include(c => c.Proveedor)
            .Include(c => c.Detalles).ThenInclude(d => d.Producto)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (compra == null) return NotFound();
        return View(compra);
    }

    public async Task<IActionResult> Create()
    {
        var vm = new CompraCrearVm();
        await PrepararAsync(vm);
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CompraCrearVm vm)
    {
        var resultado = await _inventario.RegistrarCompraAsync(vm, User.Identity?.Name ?? "sistema");
        if (!resultado.Exito)
        {
            ModelState.AddModelError(string.Empty, resultado.Error ?? "No fue posible registrar la compra.");
            await PrepararAsync(vm);
            return View(vm);
        }

        TempData["Ok"] = "Compra registrada y existencias actualizadas.";
        return RedirectToAction(nameof(Details), new { id = resultado.Id });
    }

    private async Task PrepararAsync(CompraCrearVm vm)
    {
        var proveedores = await _db.Proveedores.Where(p => p.Activo).OrderBy(p => p.Nombre).ToListAsync();
        vm.Proveedores = proveedores.Select(p => new SelectListItem(p.Nombre, p.Id.ToString())).ToList();

        var productos = await _db.Productos.Where(p => p.Activo).OrderBy(p => p.Nombre).ToListAsync();
        vm.Productos = productos
            .Select(p => new ProductoOpcion(p.Id, p.Codigo, p.Nombre, p.PrecioCompra, p.PrecioVenta, p.StockActual))
            .ToList();
    }
}
