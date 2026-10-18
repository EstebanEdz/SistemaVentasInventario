namespace Inventario.Web.Models;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Vendedor = "Vendedor";
    public const string Almacenista = "Almacenista";

    public const string AdminAlmacen = Admin + "," + Almacenista;
    public const string AdminVentas = Admin + "," + Vendedor;

    public static readonly string[] Todos = { Admin, Vendedor, Almacenista };
}
