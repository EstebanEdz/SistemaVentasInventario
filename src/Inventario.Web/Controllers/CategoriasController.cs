using Inventario.Web.Data;
using Inventario.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Web.Controllers;

[Authorize(Roles = Roles.AdminAlmacen)]
public class CategoriasController : Controller
{
    private readonly AppDbContext _db;

    public CategoriasController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index() =>
        View(await _db.Categorias.Include(c => c.Productos).OrderBy(c => c.Nombre).ToListAsync());

    public IActionResult Create() => View("Form", new Categoria());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Nombre,Descripcion")] Categoria modelo)
    {
        if (await _db.Categorias.AnyAsync(c => c.Nombre == modelo.Nombre))
            ModelState.AddModelError(nameof(modelo.Nombre), "Ya existe una categoría con ese nombre.");
        if (!ModelState.IsValid)
            return View("Form", modelo);

        _db.Categorias.Add(modelo);
        await _db.SaveChangesAsync();
        TempData["Ok"] = "Categoría creada.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var categoria = await _db.Categorias.FindAsync(id);
        if (categoria == null) return NotFound();
        return View("Form", categoria);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([Bind("Id,Nombre,Descripcion")] Categoria modelo)
    {
        if (await _db.Categorias.AnyAsync(c => c.Nombre == modelo.Nombre && c.Id != modelo.Id))
            ModelState.AddModelError(nameof(modelo.Nombre), "Ya existe una categoría con ese nombre.");
        if (!ModelState.IsValid)
            return View("Form", modelo);

        var categoria = await _db.Categorias.FindAsync(modelo.Id);
        if (categoria == null) return NotFound();
        categoria.Nombre = modelo.Nombre;
        categoria.Descripcion = modelo.Descripcion;
        await _db.SaveChangesAsync();
        TempData["Ok"] = "Categoría actualizada.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var categoria = await _db.Categorias.Include(c => c.Productos).FirstOrDefaultAsync(c => c.Id == id);
        if (categoria == null) return NotFound();

        if (categoria.Productos.Any())
        {
            TempData["Error"] = "No se puede eliminar: la categoría tiene productos asociados.";
            return RedirectToAction(nameof(Index));
        }

        _db.Categorias.Remove(categoria);
        await _db.SaveChangesAsync();
        TempData["Ok"] = "Categoría eliminada.";
        return RedirectToAction(nameof(Index));
    }
}
