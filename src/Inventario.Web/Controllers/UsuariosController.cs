using Inventario.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Web.Controllers;

[Authorize(Roles = Roles.Admin)]
public class UsuariosController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UsuariosController(UserManager<ApplicationUser> userManager) => _userManager = userManager;

    public async Task<IActionResult> Index()
    {
        var usuarios = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();
        var filas = new List<UsuarioFila>();
        foreach (var u in usuarios)
        {
            var roles = await _userManager.GetRolesAsync(u);
            filas.Add(new UsuarioFila(u.NombreCompleto, u.Email ?? "", string.Join(", ", roles)));
        }
        return View(filas);
    }

    public IActionResult Create() => View(new UsuarioCrearVm());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UsuarioCrearVm vm)
    {
        if (!Roles.Todos.Contains(vm.Rol))
            ModelState.AddModelError(nameof(vm.Rol), "Rol no válido.");
        if (!ModelState.IsValid)
            return View(vm);

        var usuario = new ApplicationUser
        {
            UserName = vm.Email,
            Email = vm.Email,
            NombreCompleto = vm.NombreCompleto,
            EmailConfirmed = true
        };

        var resultado = await _userManager.CreateAsync(usuario, vm.Password);
        if (!resultado.Succeeded)
        {
            foreach (var error in resultado.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(vm);
        }

        await _userManager.AddToRoleAsync(usuario, vm.Rol);
        TempData["Ok"] = "Usuario creado.";
        return RedirectToAction(nameof(Index));
    }
}
