using Inventario.Web.Data;
using Inventario.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Web.Controllers;

[Authorize(Roles = Roles.AdminAlmacen)]
public class ProveedoresController : Controller
{
    private readonly AppDbContext _db;

    public ProveedoresController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? q)
    {
        var query = _db.Proveedores.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(p => p.Nombre.Contains(q) || (p.Documento != null && p.Documento.Contains(q)));

        ViewBag.Q = q;
        return View(await query.OrderBy(p => p.Nombre).ToListAsync());
    }

    public IActionResult Create() => View("Form", new Proveedor());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Nombre,Documento,Telefono,Email,Direccion,Activo")] Proveedor modelo)
    {
        if (!ModelState.IsValid)
            return View("Form", modelo);

        _db.Proveedores.Add(modelo);
        await _db.SaveChangesAsync();
        TempData["Ok"] = "Proveedor creado.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var proveedor = await _db.Proveedores.FindAsync(id);
        if (proveedor == null) return NotFound();
        return View("Form", proveedor);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([Bind("Id,Nombre,Documento,Telefono,Email,Direccion,Activo")] Proveedor modelo)
    {
        if (!ModelState.IsValid)
            return View("Form", modelo);

        _db.Proveedores.Update(modelo);
        await _db.SaveChangesAsync();
        TempData["Ok"] = "Proveedor actualizado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id)
    {
        var proveedor = await _db.Proveedores.FindAsync(id);
        if (proveedor == null) return NotFound();

        proveedor.Activo = !proveedor.Activo;
        await _db.SaveChangesAsync();
        TempData["Ok"] = proveedor.Activo ? "Proveedor activado." : "Proveedor desactivado.";
        return RedirectToAction(nameof(Index));
    }
}
