using Inventario.Web.Models;
using Inventario.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Web.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider sp)
    {
        var db = sp.GetRequiredService<AppDbContext>();
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var inventario = sp.GetRequiredService<InventarioService>();

        foreach (var rol in Roles.Todos)
        {
            if (!await roleManager.RoleExistsAsync(rol))
                await roleManager.CreateAsync(new IdentityRole(rol));
        }

        await CrearUsuarioAsync(userManager, "admin@demo.com", "Administrador Demo", "Admin123!", Roles.Admin);
        await CrearUsuarioAsync(userManager, "vendedor@demo.com", "Vendedor Demo", "Vendedor123!", Roles.Vendedor);
        await CrearUsuarioAsync(userManager, "almacen@demo.com", "Almacenista Demo", "Almacen123!", Roles.Almacenista);

        if (await db.Productos.AnyAsync())
            return;

        var cats = new[]
        {
            new Categoria { Nombre = "Bebidas", Descripcion = "Agua, gaseosas y jugos" },
            new Categoria { Nombre = "Snacks", Descripcion = "Pasabocas y galletas" },
            new Categoria { Nombre = "Aseo", Descripcion = "Productos de limpieza" },
            new Categoria { Nombre = "Papelería", Descripcion = "Útiles y papelería" }
        };
        db.Categorias.AddRange(cats);

        var provs = new[]
        {
            new Proveedor { Nombre = "Distribuidora Andina S.A.S.", Documento = "900123456-1", Telefono = "6015551010", Email = "ventas@andina.example.com", Direccion = "Calle 10 # 20-30, Bogotá" },
            new Proveedor { Nombre = "Papelería Central Ltda.", Documento = "800987654-3", Telefono = "6015552020", Email = "pedidos@papeleriacentral.example.com", Direccion = "Carrera 7 # 45-12, Bogotá" }
        };
        db.Proveedores.AddRange(provs);

        var clientes = new[]
        {
            new Cliente { Nombre = "Tienda La Esquina", Documento = "901234567-8", Telefono = "3001112233", Email = "contacto@laesquina.example.com", Direccion = "Cra 15 # 80-10" },
            new Cliente { Nombre = "Supermercado El Ahorro", Documento = "900765432-1", Telefono = "3004445566", Email = "compras@elahorro.example.com", Direccion = "Av. Boyacá # 60-25" },
            new Cliente { Nombre = "María Fernanda Rojas", Documento = "1012345678", Telefono = "3157778899", Email = "mfrojas@example.com", Direccion = "Calle 100 # 15-20" }
        };
        db.Clientes.AddRange(clientes);
        await db.SaveChangesAsync();

        Producto P(string cod, string nom, Categoria c, Proveedor pv, decimal compra, decimal venta, int min) => new()
        {
            Codigo = cod, Nombre = nom, CategoriaId = c.Id, ProveedorId = pv.Id,
            PrecioCompra = compra, PrecioVenta = venta, StockMinimo = min
        };

        var productos = new[]
        {
            P("BEB-001", "Agua mineral 600 ml", cats[0], provs[0], 900, 1500, 20),
            P("BEB-002", "Gaseosa cola 1.5 L", cats[0], provs[0], 3500, 5200, 15),
            P("BEB-003", "Jugo de naranja 1 L", cats[0], provs[0], 4200, 6500, 10),
            P("SNK-001", "Papas fritas 150 g", cats[1], provs[0], 2800, 4500, 15),
            P("SNK-002", "Galletas de vainilla", cats[1], provs[0], 1800, 3000, 20),
            P("ASE-001", "Jabón de manos 500 ml", cats[2], provs[0], 5000, 8500, 8),
            P("ASE-002", "Detergente 1 kg", cats[2], provs[0], 7500, 12000, 10),
            P("PAP-001", "Cuaderno rayado 100 hojas", cats[3], provs[1], 3000, 5500, 12),
            P("PAP-002", "Bolígrafo azul (caja x12)", cats[3], provs[1], 9000, 15000, 6)
        };
        db.Productos.AddRange(productos);
        await db.SaveChangesAsync();

        var id = productos.ToDictionary(p => p.Codigo, p => p.Id);
        static LineaVm L(int producto, int cantidad, decimal precio) =>
            new() { ProductoId = producto, Cantidad = cantidad, Precio = precio };

        // Compras iniciales (generan los movimientos de entrada)
        await inventario.RegistrarCompraAsync(new CompraCrearVm
        {
            ProveedorId = provs[0].Id,
            Observacion = "Compra inicial",
            Lineas =
            {
                L(id["BEB-001"], 100, 900), L(id["BEB-002"], 60, 3500), L(id["BEB-003"], 40, 4200),
                L(id["SNK-001"], 50, 2800), L(id["SNK-002"], 8, 1800),
                L(id["ASE-001"], 30, 5000), L(id["ASE-002"], 5, 7500)
            }
        }, "sistema");

        await inventario.RegistrarCompraAsync(new CompraCrearVm
        {
            ProveedorId = provs[1].Id,
            Observacion = "Compra inicial de papelería",
            Lineas = { L(id["PAP-001"], 40, 3000), L(id["PAP-002"], 20, 9000) }
        }, "sistema");

        // Ventas de ejemplo (generan facturas y movimientos de salida)
        await inventario.RegistrarVentaAsync(new VentaCrearVm
        {
            ClienteId = clientes[0].Id,
            Lineas = { L(id["BEB-001"], 10, 0), L(id["BEB-002"], 6, 0), L(id["SNK-001"], 5, 0) }
        }, "vendedor@demo.com");

        await inventario.RegistrarVentaAsync(new VentaCrearVm
        {
            ClienteId = clientes[1].Id,
            Lineas = { L(id["SNK-002"], 3, 0), L(id["PAP-001"], 10, 0) }
        }, "vendedor@demo.com");

        await inventario.RegistrarVentaAsync(new VentaCrearVm
        {
            Lineas = { L(id["BEB-003"], 4, 0), L(id["ASE-001"], 2, 0) }
        }, "vendedor@demo.com");
    }

    private static async Task CrearUsuarioAsync(UserManager<ApplicationUser> um, string email, string nombre, string password, string rol)
    {
        if (await um.FindByEmailAsync(email) != null)
            return;

        var usuario = new ApplicationUser { UserName = email, Email = email, NombreCompleto = nombre, EmailConfirmed = true };
        var resultado = await um.CreateAsync(usuario, password);
        if (resultado.Succeeded)
            await um.AddToRoleAsync(usuario, rol);
    }
}
