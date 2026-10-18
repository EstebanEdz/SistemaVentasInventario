using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Inventario.Web.Models;

public record ProductoOpcion(int Id, string Codigo, string Nombre, decimal PrecioCompra, decimal PrecioVenta, int StockActual);
public record VentaDiaria(string Dia, decimal Total);
public record ProductoVendido(string Nombre, int Cantidad, decimal Total);
public record UsuarioFila(string Nombre, string Email, string Roles);

public class LoginVm
{
    [Required(ErrorMessage = "Ingrese su correo"), EmailAddress(ErrorMessage = "Correo no válido")]
    [Display(Name = "Correo electrónico")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese su contraseña"), DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Recordarme")]
    public bool Recordarme { get; set; }

    public string? ReturnUrl { get; set; }
}

public class LineaVm
{
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
    public decimal Precio { get; set; }
}

public class CompraCrearVm
{
    [Display(Name = "Proveedor")]
    public int ProveedorId { get; set; }

    [Display(Name = "Observaciones")]
    public string? Observacion { get; set; }

    public List<LineaVm> Lineas { get; set; } = new();

    [BindNever] public List<SelectListItem> Proveedores { get; set; } = new();
    [BindNever] public List<ProductoOpcion> Productos { get; set; } = new();
}

public class VentaCrearVm
{
    [Display(Name = "Cliente")]
    public int? ClienteId { get; set; }

    [Display(Name = "Observaciones")]
    public string? Observacion { get; set; }

    public List<LineaVm> Lineas { get; set; } = new();

    [BindNever] public List<SelectListItem> Clientes { get; set; } = new();
    [BindNever] public List<ProductoOpcion> Productos { get; set; } = new();
}

public class AjusteVm
{
    public int ProductoId { get; set; }
    public string ProductoNombre { get; set; } = string.Empty;
    public int StockActual { get; set; }

    [Display(Name = "Nuevo stock"), Range(0, int.MaxValue, ErrorMessage = "Ingrese un valor mayor o igual a 0")]
    public int NuevoStock { get; set; }

    [Required(ErrorMessage = "Indique el motivo del ajuste"), StringLength(300)]
    public string Motivo { get; set; } = string.Empty;
}

public class MovimientosVm
{
    [Display(Name = "Producto")] public int? ProductoId { get; set; }
    public TipoMovimiento? Tipo { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }

    [BindNever] public List<MovimientoInventario> Items { get; set; } = new();
    [BindNever] public List<SelectListItem> Productos { get; set; } = new();
}

public class UsuarioCrearVm
{
    [Required(ErrorMessage = "Ingrese el nombre"), StringLength(150)]
    [Display(Name = "Nombre completo")]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese el correo"), EmailAddress(ErrorMessage = "Correo no válido")]
    [Display(Name = "Correo electrónico")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese una contraseña"), DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mínimo 6 caracteres")]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Seleccione un rol")]
    public string Rol { get; set; } = Roles.Vendedor;
}

public class DashboardVm
{
    public int TotalProductos { get; set; }
    public decimal ValorInventario { get; set; }
    public decimal VentasMes { get; set; }
    public decimal ComprasMes { get; set; }
    public int TotalStockBajo { get; set; }
    public List<Producto> StockBajo { get; set; } = new();
    public List<MovimientoInventario> UltimosMovimientos { get; set; } = new();
}

public class ReporteVentasVm
{
    public DateTime Desde { get; set; }
    public DateTime Hasta { get; set; }
    public int NumeroVentas { get; set; }
    public decimal TotalVentas { get; set; }
    public decimal TicketPromedio { get; set; }
    public List<VentaDiaria> PorDia { get; set; } = new();
    public List<ProductoVendido> TopProductos { get; set; } = new();
}
