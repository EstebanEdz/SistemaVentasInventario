using Inventario.Web.Data;
using Inventario.Web.Models;
using Inventario.Web.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Inventario.Tests;

public class InventarioServiceTests
{
    private static (AppDbContext Db, InventarioService Servicio) Crear(int stockInicial = 10)
    {
        var opciones = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new AppDbContext(opciones);
        db.Categorias.Add(new Categoria { Id = 1, Nombre = "General" });
        db.Proveedores.Add(new Proveedor { Id = 1, Nombre = "Proveedor Test" });
        db.Clientes.Add(new Cliente { Id = 1, Nombre = "Cliente Test" });
        db.Productos.Add(new Producto
        {
            Id = 1, Codigo = "P-001", Nombre = "Producto de prueba", CategoriaId = 1, ProveedorId = 1,
            PrecioCompra = 600, PrecioVenta = 1000, StockActual = stockInicial, StockMinimo = 3
        });
        db.SaveChanges();

        return (db, new InventarioService(db));
    }

    private static List<LineaVm> Lineas(int productoId, int cantidad, decimal precio = 0) =>
        new() { new LineaVm { ProductoId = productoId, Cantidad = cantidad, Precio = precio } };

    [Fact]
    public async Task Compra_AumentaStock_Y_RegistraMovimientoDeEntrada()
    {
        var (db, servicio) = Crear(10);

        var resultado = await servicio.RegistrarCompraAsync(
            new CompraCrearVm { ProveedorId = 1, Lineas = Lineas(1, 5, 650) }, "tester");

        Assert.True(resultado.Exito);
        var producto = await db.Productos.SingleAsync();
        Assert.Equal(15, producto.StockActual);
        Assert.Equal(650, producto.PrecioCompra);

        var movimiento = await db.Movimientos.SingleAsync();
        Assert.Equal(TipoMovimiento.Entrada, movimiento.Tipo);
        Assert.Equal(5, movimiento.Cantidad);
        Assert.Equal(10, movimiento.StockAnterior);
        Assert.Equal(15, movimiento.StockNuevo);
    }

    [Fact]
    public async Task Venta_DescuentaStock_CalculaIva_Y_GeneraFactura()
    {
        var (db, servicio) = Crear(10);

        var resultado = await servicio.RegistrarVentaAsync(
            new VentaCrearVm { ClienteId = 1, Lineas = Lineas(1, 2) }, "tester");

        Assert.True(resultado.Exito);
        Assert.Equal(8, (await db.Productos.SingleAsync()).StockActual);

        var venta = await db.Ventas.SingleAsync();
        Assert.Equal(2000m, venta.Subtotal);
        Assert.Equal(380m, venta.Impuesto);   // 19 %
        Assert.Equal(2380m, venta.Total);

        var factura = await db.Facturas.SingleAsync();
        Assert.Equal("F-000001", factura.Numero);

        var movimiento = await db.Movimientos.SingleAsync();
        Assert.Equal(TipoMovimiento.Salida, movimiento.Tipo);
        Assert.Equal(-2, movimiento.Cantidad);
    }

    [Fact]
    public async Task Venta_ConStockInsuficiente_Falla_Y_NoModificaNada()
    {
        var (db, servicio) = Crear(3);

        var resultado = await servicio.RegistrarVentaAsync(
            new VentaCrearVm { Lineas = Lineas(1, 5) }, "tester");

        Assert.False(resultado.Exito);
        Assert.Contains("Stock insuficiente", resultado.Error);
        Assert.Equal(3, (await db.Productos.SingleAsync()).StockActual);
        Assert.Empty(db.Ventas);
        Assert.Empty(db.Movimientos);
    }

    [Fact]
    public async Task Venta_ConMismoProductoEnVariasLineas_ValidaLaSumaTotal()
    {
        var (_, servicio) = Crear(5);

        var vm = new VentaCrearVm
        {
            Lineas =
            {
                new LineaVm { ProductoId = 1, Cantidad = 3 },
                new LineaVm { ProductoId = 1, Cantidad = 3 }
            }
        };

        var resultado = await servicio.RegistrarVentaAsync(vm, "tester");

        Assert.False(resultado.Exito);
    }

    [Fact]
    public async Task Facturas_TienenNumeracionConsecutiva()
    {
        var (db, servicio) = Crear(10);

        await servicio.RegistrarVentaAsync(new VentaCrearVm { Lineas = Lineas(1, 1) }, "tester");
        await servicio.RegistrarVentaAsync(new VentaCrearVm { Lineas = Lineas(1, 1) }, "tester");

        var numeros = await db.Facturas.OrderBy(f => f.Id).Select(f => f.Numero).ToListAsync();
        Assert.Equal(new[] { "F-000001", "F-000002" }, numeros);
    }

    [Fact]
    public async Task Ajuste_RegistraMovimientoConLaDiferencia()
    {
        var (db, servicio) = Crear(10);

        var resultado = await servicio.AjustarStockAsync(1, 7, "Conteo físico", "tester");

        Assert.True(resultado.Exito);
        Assert.Equal(7, (await db.Productos.SingleAsync()).StockActual);

        var movimiento = await db.Movimientos.SingleAsync();
        Assert.Equal(TipoMovimiento.Ajuste, movimiento.Tipo);
        Assert.Equal(-3, movimiento.Cantidad);
        Assert.Equal("Conteo físico", movimiento.Motivo);
    }

    [Fact]
    public async Task Ajuste_ConStockNegativo_Falla()
    {
        var (_, servicio) = Crear(10);

        var resultado = await servicio.AjustarStockAsync(1, -1, "Error", "tester");

        Assert.False(resultado.Exito);
    }

    [Fact]
    public async Task Compra_SinLineas_Falla()
    {
        var (_, servicio) = Crear(10);

        var resultado = await servicio.RegistrarCompraAsync(new CompraCrearVm { ProveedorId = 1 }, "tester");

        Assert.False(resultado.Exito);
    }
}
