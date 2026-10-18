using Inventario.Web.Data;
using Inventario.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Web.Controllers;

[Authorize(Roles = Roles.AdminVentas)]
public class ClientesController : Controller
{
    private readonly AppDbContext _db;

    public ClientesController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? q)
    {
        var query = _db.Clientes.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(p => p.Nombre.Contains(q) || (p.Documento != null && p.Documento.Contains(q)));

        ViewBag.Q = q;
        return View(await query.OrderBy(p => p.Nombre).ToListAsync());
    }

    public IActionResult Create() => View("Form", new Cliente());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Nombre,Documento,Telefono,Email,Direccion,Activo")] Cliente modelo)
    {
        if (!ModelState.IsValid)
            return View("Form", modelo);

        _db.Clientes.Add(modelo);
        await _db.SaveChangesAsync();
        TempData["Ok"] = "Cliente creado.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var cliente = await _db.Clientes.FindAsync(id);
        if (cliente == null) return NotFound();
        return View("Form", cliente);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([Bind("Id,Nombre,Documento,Telefono,Email,Direccion,Activo")] Cliente modelo)
    {
        if (!ModelState.IsValid)
            return View("Form", modelo);

        _db.Clientes.Update(modelo);
        await _db.SaveChangesAsync();
        TempData["Ok"] = "Cliente actualizado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id)
    {
        var cliente = await _db.Clientes.FindAsync(id);
        if (cliente == null) return NotFound();

        cliente.Activo = !cliente.Activo;
        await _db.SaveChangesAsync();
        TempData["Ok"] = cliente.Activo ? "Cliente activado." : "Cliente desactivado.";
        return RedirectToAction(nameof(Index));
    }
}
