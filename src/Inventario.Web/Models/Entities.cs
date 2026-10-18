using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Inventario.Web.Models;

public class ApplicationUser : IdentityUser
{
    [StringLength(150)]
    public string NombreCompleto { get; set; } = string.Empty;
}

public class Categoria
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio"), StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Display(Name = "Descripción"), StringLength(300)]
    public string? Descripcion { get; set; }

    public ICollection<Producto> Productos { get; set; } = new List<Producto>();
}

public class Proveedor
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio"), StringLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [Display(Name = "NIT / Documento"), StringLength(30)]
    public string? Documento { get; set; }

    [Display(Name = "Teléfono"), StringLength(30)]
    public string? Telefono { get; set; }

    [EmailAddress(ErrorMessage = "Correo no válido"), StringLength(150)]
    public string? Email { get; set; }

    [Display(Name = "Dirección"), StringLength(250)]
    public string? Direccion { get; set; }

    public bool Activo { get; set; } = true;
}

public class Cliente
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio"), StringLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [Display(Name = "Documento"), StringLength(30)]
    public string? Documento { get; set; }

    [Display(Name = "Teléfono"), StringLength(30)]
    public string? Telefono { get; set; }

    [EmailAddress(ErrorMessage = "Correo no válido"), StringLength(150)]
    public string? Email { get; set; }

    [Display(Name = "Dirección"), StringLength(250)]
    public string? Direccion { get; set; }

    public bool Activo { get; set; } = true;
}

public class Producto
{
    public int Id { get; set; }

    [Display(Name = "Código"), Required(ErrorMessage = "El código es obligatorio"), StringLength(30)]
    public string Codigo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio"), StringLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [Display(Name = "Descripción"), StringLength(500)]
    public string? Descripcion { get; set; }

    [Display(Name = "Categoría")]
    public int CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }

    [Display(Name = "Proveedor")]
    public int? ProveedorId { get; set; }
    public Proveedor? Proveedor { get; set; }

    [Display(Name = "Precio de compra"), Column(TypeName = "decimal(18,2)"), Range(0.0, 999999999.0)]
    public decimal PrecioCompra { get; set; }

    [Display(Name = "Precio de venta"), Column(TypeName = "decimal(18,2)"), Range(0.0, 999999999.0)]
    public decimal PrecioVenta { get; set; }

    [Display(Name = "Stock actual")]
    public int StockActual { get; set; }

    [Display(Name = "Stock mínimo"), Range(0, 1000000)]
    public int StockMinimo { get; set; }

    public bool Activo { get; set; } = true;

    [NotMapped]
    public bool StockBajo => StockActual <= StockMinimo;
}

public enum TipoMovimiento
{
    Entrada = 1,
    Salida = 2,
    Ajuste = 3
}

public class MovimientoInventario
{
    public int Id { get; set; }

    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }

    public TipoMovimiento Tipo { get; set; }

    /// <summary>Variación con signo: positiva suma stock, negativa lo descuenta.</summary>
    public int Cantidad { get; set; }
    public int StockAnterior { get; set; }
    public int StockNuevo { get; set; }

    public DateTime Fecha { get; set; }

    [StringLength(256)]
    public string Usuario { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Motivo { get; set; }

    public int? CompraId { get; set; }
    public Compra? Compra { get; set; }

    public int? VentaId { get; set; }
    public Venta? Venta { get; set; }
}

public class Compra
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }

    public int ProveedorId { get; set; }
    public Proveedor? Proveedor { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Total { get; set; }

    [StringLength(256)]
    public string Usuario { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Observacion { get; set; }

    public ICollection<DetalleCompra> Detalles { get; set; } = new List<DetalleCompra>();
}

public class DetalleCompra
{
    public int Id { get; set; }

    public int CompraId { get; set; }
    public Compra? Compra { get; set; }

    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }

    public int Cantidad { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal CostoUnitario { get; set; }

    [NotMapped]
    public decimal Subtotal => Cantidad * CostoUnitario;
}

public class Venta
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }

    public int? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Subtotal { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Impuesto { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Total { get; set; }

    [StringLength(256)]
    public string Usuario { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Observacion { get; set; }

    public ICollection<DetalleVenta> Detalles { get; set; } = new List<DetalleVenta>();
    public Factura? Factura { get; set; }
}

public class DetalleVenta
{
    public int Id { get; set; }

    public int VentaId { get; set; }
    public Venta? Venta { get; set; }

    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }

    public int Cantidad { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PrecioUnitario { get; set; }

    [NotMapped]
    public decimal Subtotal => Cantidad * PrecioUnitario;
}

public class Factura
{
    public int Id { get; set; }

    [StringLength(20)]
    public string Numero { get; set; } = string.Empty;

    public int VentaId { get; set; }
    public Venta? Venta { get; set; }

    public DateTime Fecha { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Subtotal { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Impuesto { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Total { get; set; }
}
