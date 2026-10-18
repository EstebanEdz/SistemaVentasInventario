using Inventario.Web.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Web.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Compra> Compras => Set<Compra>();
    public DbSet<DetalleCompra> DetallesCompra => Set<DetalleCompra>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<DetalleVenta> DetallesVenta => Set<DetalleVenta>();
    public DbSet<Factura> Facturas => Set<Factura>();
    public DbSet<MovimientoInventario> Movimientos => Set<MovimientoInventario>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Producto>().HasIndex(p => p.Codigo).IsUnique();
        builder.Entity<Categoria>().HasIndex(c => c.Nombre).IsUnique();
        builder.Entity<Factura>().HasIndex(f => f.Numero).IsUnique();

        builder.Entity<Factura>()
            .HasOne(f => f.Venta)
            .WithOne(v => v.Factura)
            .HasForeignKey<Factura>(f => f.VentaId);

        // Integridad: nunca borrar en cascada datos del negocio
        var ns = typeof(Producto).Namespace;
        foreach (var fk in builder.Model.GetEntityTypes()
                     .Where(t => t.ClrType.Namespace == ns)
                     .SelectMany(t => t.GetForeignKeys()))
        {
            fk.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }
}
