using Inventario.Web.Data;
using Inventario.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Web.Services;

public class ResultadoOperacion
{
    public bool Exito { get; init; }
    public string? Error { get; init; }
    public int? Id { get; init; }

    public static ResultadoOperacion Ok(int? id = null) => new() { Exito = true, Id = id };
    public static ResultadoOperacion Falla(string error) => new() { Exito = false, Error = error };
}

/// <summary>
/// Reglas de negocio del inventario. Toda variación de stock pasa por aquí y deja un
/// movimiento auditable. Cada operación se guarda con un único SaveChanges (atómico).
/// </summary>
public class InventarioService
{
    public const decimal TasaIva = 0.19m;

    private readonly AppDbContext _db;

    public InventarioService(AppDbContext db) => _db = db;

    public async Task<ResultadoOperacion> RegistrarCompraAsync(CompraCrearVm vm, string usuario)
    {
        var lineas = vm.Lineas.Where(l => l.Cantidad > 0).ToList();
        if (lineas.Count == 0)
            return ResultadoOperacion.Falla("Debe agregar al menos un producto a la compra.");
        if (lineas.Any(l => l.Precio < 0))
            return ResultadoOperacion.Falla("El costo unitario no puede ser negativo.");
        if (!await _db.Proveedores.AnyAsync(p => p.Id == vm.ProveedorId && p.Activo))
            return ResultadoOperacion.Falla("Seleccione un proveedor válido.");

        var ids = lineas.Select(l => l.ProductoId).Distinct().ToList();
        var productos = await _db.Productos.Where(p => ids.Contains(p.Id) && p.Activo).ToDictionaryAsync(p => p.Id);
        if (productos.Count != ids.Count)
            return ResultadoOperacion.Falla("Alguno de los productos no existe o está inactivo.");

        var compra = new Compra
        {
            Fecha = DateTime.Now,
            ProveedorId = vm.ProveedorId,
            Usuario = usuario,
            Observacion = vm.Observacion
        };

        foreach (var l in lineas)
        {
            var producto = productos[l.ProductoId];
            compra.Detalles.Add(new DetalleCompra { ProductoId = producto.Id, Cantidad = l.Cantidad, CostoUnitario = l.Precio });
            producto.PrecioCompra = l.Precio;
            _db.Movimientos.Add(CrearMovimiento(producto, TipoMovimiento.Entrada, l.Cantidad, usuario, "Compra", compra, null));
        }

        compra.Total = compra.Detalles.Sum(d => d.Cantidad * d.CostoUnitario);
        _db.Compras.Add(compra);
        await _db.SaveChangesAsync();
        return ResultadoOperacion.Ok(compra.Id);
    }

    public async Task<ResultadoOperacion> RegistrarVentaAsync(VentaCrearVm vm, string usuario)
    {
        var lineas = vm.Lineas.Where(l => l.Cantidad > 0).ToList();
        if (lineas.Count == 0)
            return ResultadoOperacion.Falla("Debe agregar al menos un producto a la venta.");

        if (vm.ClienteId.HasValue)
        {
            var clienteId = vm.ClienteId.Value;
            if (!await _db.Clientes.AnyAsync(c => c.Id == clienteId && c.Activo))
                return ResultadoOperacion.Falla("El cliente seleccionado no es válido.");
        }

        var ids = lineas.Select(l => l.ProductoId).Distinct().ToList();
        var productos = await _db.Productos.Where(p => ids.Contains(p.Id) && p.Activo).ToDictionaryAsync(p => p.Id);
        if (productos.Count != ids.Count)
            return ResultadoOperacion.Falla("Alguno de los productos no existe o está inactivo.");

        var solicitado = lineas.GroupBy(l => l.ProductoId).ToDictionary(g => g.Key, g => g.Sum(x => x.Cantidad));
        foreach (var par in solicitado)
        {
            var producto = productos[par.Key];
            if (producto.StockActual < par.Value)
                return ResultadoOperacion.Falla(
                    $"Stock insuficiente para '{producto.Nombre}'. Disponible: {producto.StockActual}, solicitado: {par.Value}.");
        }

        var venta = new Venta
        {
            Fecha = DateTime.Now,
            ClienteId = vm.ClienteId,
            Usuario = usuario,
            Observacion = vm.Observacion
        };

        foreach (var l in lineas)
        {
            var producto = productos[l.ProductoId];
            // El precio siempre lo define el servidor, nunca el navegador
            venta.Detalles.Add(new DetalleVenta { ProductoId = producto.Id, Cantidad = l.Cantidad, PrecioUnitario = producto.PrecioVenta });
            _db.Movimientos.Add(CrearMovimiento(producto, TipoMovimiento.Salida, -l.Cantidad, usuario, "Venta", null, venta));
        }

        venta.Subtotal = Math.Round(venta.Detalles.Sum(d => d.Cantidad * d.PrecioUnitario), 2);
        venta.Impuesto = Math.Round(venta.Subtotal * TasaIva, 2);
        venta.Total = venta.Subtotal + venta.Impuesto;

        var consecutivo = await _db.Facturas.CountAsync() + 1;
        var factura = new Factura
        {
            Numero = $"F-{consecutivo:D6}",
            Fecha = venta.Fecha,
            Subtotal = venta.Subtotal,
            Impuesto = venta.Impuesto,
            Total = venta.Total
        };
        venta.Factura = factura;

        _db.Ventas.Add(venta);
        await _db.SaveChangesAsync();
        return ResultadoOperacion.Ok(factura.Id);
    }

    public async Task<ResultadoOperacion> AjustarStockAsync(int productoId, int nuevoStock, string motivo, string usuario)
    {
        if (nuevoStock < 0)
            return ResultadoOperacion.Falla("El stock no puede ser negativo.");

        var producto = await _db.Productos.FindAsync(productoId);
        if (producto == null)
            return ResultadoOperacion.Falla("Producto no encontrado.");

        var delta = nuevoStock - producto.StockActual;
        if (delta == 0)
            return ResultadoOperacion.Falla("El nuevo stock es igual al stock actual.");

        _db.Movimientos.Add(CrearMovimiento(producto, TipoMovimiento.Ajuste, delta, usuario, motivo, null, null));
        await _db.SaveChangesAsync();
        return ResultadoOperacion.Ok(producto.Id);
    }

    private static MovimientoInventario CrearMovimiento(
        Producto producto, TipoMovimiento tipo, int delta, string usuario, string? motivo, Compra? compra, Venta? venta)
    {
        var anterior = producto.StockActual;
        producto.StockActual += delta;

        return new MovimientoInventario
        {
            Producto = producto,
            Tipo = tipo,
            Cantidad = delta,
            StockAnterior = anterior,
            StockNuevo = producto.StockActual,
            Fecha = DateTime.Now,
            Usuario = usuario,
            Motivo = motivo,
            Compra = compra,
            Venta = venta
        };
    }
}
