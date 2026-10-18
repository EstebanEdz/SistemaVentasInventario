using Inventario.Web.Data;
using Inventario.Web.Models;
using Inventario.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Web.Controllers;

public class ProductosController : Controller
{
    private readonly AppDbContext _db;
    private readonly InventarioService _inventario;

    public ProductosController(AppDbContext db, InventarioService inventario)
    {
        _db = db;
        _inventario = inventario;
    }

    // Todos los roles pueden consultar; "soloBajo" funciona como pantalla de alertas
    public async Task<IActionResult> Index(string? q, int? categoriaId, bool soloBajo = false)
    {
        var query = _db.Productos.Include(p => p.Categoria).Include(p => p.Proveedor).AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(p => p.Nombre.Contains(q) || p.Codigo.Contains(q));
        if (categoriaId.HasValue)
            query = query.Where(p => p.CategoriaId == categoriaId.Value);
        if (soloBajo)
            query = query.Where(p => p.Activo && p.StockActual <= p.StockMinimo);

        ViewBag.Categorias = new SelectList(await _db.Categorias.OrderBy(c => c.Nombre).ToListAsync(), "Id", "Nombre", categoriaId);
        ViewBag.Q = q;
        ViewBag.SoloBajo = soloBajo;
        return View(await query.OrderBy(p => p.Nombre).ToListAsync());
    }

    [Authorize(Roles = Roles.AdminAlmacen)]
    public async Task<IActionResult> Create()
    {
        var producto = new Producto();
        await CargarListasAsync(producto);
        return View("Form", producto);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.AdminAlmacen)]
    public async Task<IActionResult> Create(
        [Bind("Codigo,Nombre,Descripcion,CategoriaId,ProveedorId,PrecioCompra,PrecioVenta,StockMinimo,Activo")] Producto modelo)
    {
        await ValidarAsync(modelo);
        if (!ModelState.IsValid)
        {
            await CargarListasAsync(modelo);
            return View("Form", modelo);
        }

        modelo.StockActual = 0; // el stock solo cambia mediante compras, ventas o ajustes
        _db.Productos.Add(modelo);
        await _db.SaveChangesAsync();
        TempData["Ok"] = "Producto creado. Registre una compra o un ajuste para cargar existencias.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = Roles.AdminAlmacen)]
    public async Task<IActionResult> Edit(int id)
    {
        var producto = await _db.Productos.FindAsync(id);
        if (producto == null) return NotFound();
        await CargarListasAsync(producto);
        return View("Form", producto);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.AdminAlmacen)]
    public async Task<IActionResult> Edit(
        [Bind("Id,Codigo,Nombre,Descripcion,CategoriaId,ProveedorId,PrecioCompra,PrecioVenta,StockMinimo,Activo")] Producto modelo)
    {
        await ValidarAsync(modelo);
        var producto = await _db.Productos.FindAsync(modelo.Id);
        if (producto == null) return NotFound();

        if (!ModelState.IsValid)
        {
            modelo.StockActual = producto.StockActual;
            await CargarListasAsync(modelo);
            return View("Form", modelo);
        }

        producto.Codigo = modelo.Codigo;
        producto.Nombre = modelo.Nombre;
        producto.Descripcion = modelo.Descripcion;
        producto.CategoriaId = modelo.CategoriaId;
        producto.ProveedorId = modelo.ProveedorId;
        producto.PrecioCompra = modelo.PrecioCompra;
        producto.PrecioVenta = modelo.PrecioVenta;
        producto.StockMinimo = modelo.StockMinimo;
        producto.Activo = modelo.Activo;
        await _db.SaveChangesAsync();
        TempData["Ok"] = "Producto actualizado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.AdminAlmacen)]
    public async Task<IActionResult> CambiarEstado(int id)
    {
        var producto = await _db.Productos.FindAsync(id);
        if (producto == null) return NotFound();

        producto.Activo = !producto.Activo;
        await _db.SaveChangesAsync();
        TempData["Ok"] = producto.Activo ? "Producto activado." : "Producto desactivado.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = Roles.AdminAlmacen)]
    public async Task<IActionResult> Ajustar(int id)
    {
        var producto = await _db.Productos.FindAsync(id);
        if (producto == null) return NotFound();

        return View(new AjusteVm
        {
            ProductoId = producto.Id,
            ProductoNombre = $"{producto.Codigo} - {producto.Nombre}",
            StockActual = producto.StockActual,
            NuevoStock = producto.StockActual
        });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.AdminAlmacen)]
    public async Task<IActionResult> Ajustar(AjusteVm vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var resultado = await _inventario.AjustarStockAsync(vm.ProductoId, vm.NuevoStock, vm.Motivo, User.Identity?.Name ?? "sistema");
        if (!resultado.Exito)
        {
            ModelState.AddModelError(string.Empty, resultado.Error ?? "No fue posible ajustar el stock.");
            return View(vm);
        }

        TempData["Ok"] = "Stock ajustado y movimiento registrado.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidarAsync(Producto p)
    {
        if (await _db.Productos.AnyAsync(x => x.Codigo == p.Codigo && x.Id != p.Id))
            ModelState.AddModelError(nameof(p.Codigo), "Ya existe un producto con ese código.");
        if (!await _db.Categorias.AnyAsync(c => c.Id == p.CategoriaId))
            ModelState.AddModelError(nameof(p.CategoriaId), "Seleccione una categoría.");
    }

    private async Task CargarListasAsync(Producto p)
    {
        ViewBag.ListaCategorias = new SelectList(
            await _db.Categorias.OrderBy(c => c.Nombre).ToListAsync(), "Id", "Nombre", p.CategoriaId);
        ViewBag.ListaProveedores = new SelectList(
            await _db.Proveedores.Where(x => x.Activo).OrderBy(x => x.Nombre).ToListAsync(), "Id", "Nombre", p.ProveedorId);
    }
}
