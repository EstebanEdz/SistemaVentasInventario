using Inventario.Web.Data;
using Inventario.Web.Models;
using Inventario.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Web.Controllers;

[Authorize(Roles = Roles.AdminVentas)]
public class VentasController : Controller
{
    private readonly AppDbContext _db;
    private readonly InventarioService _inventario;

    public VentasController(AppDbContext db, InventarioService inventario)
    {
        _db = db;
        _inventario = inventario;
    }

    public async Task<IActionResult> Index() =>
        View(await _db.Ventas.Include(v => v.Cliente).Include(v => v.Factura)
            .OrderByDescending(v => v.Fecha).Take(200).ToListAsync());

    public async Task<IActionResult> Create()
    {
        var vm = new VentaCrearVm();
        await PrepararAsync(vm);
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(VentaCrearVm vm)
    {
        var resultado = await _inventario.RegistrarVentaAsync(vm, User.Identity?.Name ?? "sistema");
        if (!resultado.Exito)
        {
            ModelState.AddModelError(string.Empty, resultado.Error ?? "No fue posible registrar la venta.");
            await PrepararAsync(vm);
            return View(vm);
        }

        TempData["Ok"] = "Venta registrada. Se generó la factura.";
        return RedirectToAction("Details", "Facturas", new { id = resultado.Id });
    }

    private async Task PrepararAsync(VentaCrearVm vm)
    {
        var clientes = await _db.Clientes.Where(c => c.Activo).OrderBy(c => c.Nombre).ToListAsync();
        vm.Clientes = clientes.Select(c => new SelectListItem(c.Nombre, c.Id.ToString())).ToList();

        var productos = await _db.Productos.Where(p => p.Activo).OrderBy(p => p.Nombre).ToListAsync();
        vm.Productos = productos
            .Select(p => new ProductoOpcion(p.Id, p.Codigo, p.Nombre, p.PrecioCompra, p.PrecioVenta, p.StockActual))
            .ToList();
    }
}
