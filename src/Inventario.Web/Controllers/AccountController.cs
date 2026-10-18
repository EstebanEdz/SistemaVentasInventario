using Inventario.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Inventario.Web.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AccountController(SignInManager<ApplicationUser> signInManager) => _signInManager = signInManager;

    [AllowAnonymous, HttpGet]
    public IActionResult Login(string? returnUrl = null) => View(new LoginVm { ReturnUrl = returnUrl });

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVm vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var resultado = await _signInManager.PasswordSignInAsync(vm.Email, vm.Password, vm.Recordarme, lockoutOnFailure: true);
        if (resultado.Succeeded)
        {
            if (!string.IsNullOrEmpty(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
                return Redirect(vm.ReturnUrl);
            return RedirectToAction("Index", "Home");
        }

        ModelState.AddModelError(string.Empty, resultado.IsLockedOut
            ? "Cuenta bloqueada temporalmente por demasiados intentos. Intente de nuevo en unos minutos."
            : "Correo o contraseña incorrectos.");
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();
}
