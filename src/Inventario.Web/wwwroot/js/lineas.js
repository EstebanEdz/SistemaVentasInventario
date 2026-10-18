/* Editor de líneas para compras y ventas.
   Mantiene las líneas en memoria y las envía como Lineas[i].ProductoId / Cantidad / Precio. */
function initLineas(cfg) {
    const productos = cfg.productos || [];
    const lineas = (cfg.iniciales || []).map(function (l) {
        const p = productos.find(function (x) { return x.id === l.productoId; });
        return {
            id: l.productoId,
            nombre: p ? p.codigo + ' - ' + p.nombre : 'Producto ' + l.productoId,
            cantidad: l.cantidad,
            precio: l.precio
        };
    });

    const sel = document.getElementById('selProducto');
    const qty = document.getElementById('txtCantidad');
    const price = document.getElementById('txtPrecio');
    const body = document.getElementById('lineasBody');
    const totalEl = document.getElementById('lineasTotal');
    const btn = document.getElementById('btnAgregar');

    const fmt = function (n) {
        return n.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    };
    const esc = function (s) {
        return String(s).replace(/[&<>"']/g, function (c) {
            return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c];
        });
    };
    const productoActual = function () {
        return productos.find(function (x) { return x.id === parseInt(sel.value, 10); });
    };
    const precioDe = function (p) { return cfg.esVenta ? p.precioVenta : p.precioCompra; };

    function actualizarPrecio() {
        const p = productoActual();
        if (p) { price.value = precioDe(p); }
    }

    function render() {
        let total = 0;
        body.innerHTML = lineas.map(function (l, i) {
            const subtotal = l.cantidad * l.precio;
            total += subtotal;
            return '<tr>' +
                '<td>' + esc(l.nombre) + '<input type="hidden" name="Lineas[' + i + '].ProductoId" value="' + l.id + '" /></td>' +
                '<td class="text-end">' + l.cantidad + '<input type="hidden" name="Lineas[' + i + '].Cantidad" value="' + l.cantidad + '" /></td>' +
                '<td class="text-end">' + fmt(l.precio) + '<input type="hidden" name="Lineas[' + i + '].Precio" value="' + l.precio + '" /></td>' +
                '<td class="text-end">' + fmt(subtotal) + '</td>' +
                '<td class="text-end"><button type="button" class="btn btn-sm btn-outline-danger" data-i="' + i + '">Quitar</button></td>' +
                '</tr>';
        }).join('');
        totalEl.textContent = fmt(total);
    }

    sel.addEventListener('change', actualizarPrecio);

    btn.addEventListener('click', function () {
        const p = productoActual();
        const cantidad = parseInt(qty.value, 10);
        const precio = parseFloat(price.value);
        if (!p) { return; }
        if (!(cantidad > 0)) { alert('Ingrese una cantidad mayor que cero.'); return; }
        if (isNaN(precio) || precio < 0) { alert('Ingrese un precio válido.'); return; }

        const existente = lineas.find(function (l) { return l.id === p.id && l.precio === precio; });
        const yaEnLineas = lineas.filter(function (l) { return l.id === p.id; })
            .reduce(function (acc, l) { return acc + l.cantidad; }, 0);

        if (cfg.esVenta && yaEnLineas + cantidad > p.stockActual) {
            alert('Stock insuficiente. Disponible: ' + p.stockActual + ' (ya agregadas: ' + yaEnLineas + ').');
            return;
        }

        if (existente) {
            existente.cantidad += cantidad;
        } else {
            lineas.push({ id: p.id, nombre: p.codigo + ' - ' + p.nombre, cantidad: cantidad, precio: precio });
        }
        qty.value = 1;
        render();
    });

    body.addEventListener('click', function (e) {
        const b = e.target.closest('button[data-i]');
        if (b) {
            lineas.splice(parseInt(b.getAttribute('data-i'), 10), 1);
            render();
        }
    });

    actualizarPrecio();
    render();
}
