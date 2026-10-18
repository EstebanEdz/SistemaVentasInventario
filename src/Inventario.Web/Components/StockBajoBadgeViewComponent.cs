using Inventario.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Web.Components;

public class StockBajoBadgeViewComponent : ViewComponent
{
    private readonly AppDbContext _db;

    public StockBajoBadgeViewComponent(AppDbContext db) => _db = db;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var total = await _db.Productos.CountAsync(p => p.Activo && p.StockActual <= p.StockMinimo);
        return View(total);
    }
}
