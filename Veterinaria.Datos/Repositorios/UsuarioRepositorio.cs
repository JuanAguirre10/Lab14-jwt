using Microsoft.EntityFrameworkCore;
using Veterinaria.Entidades;

namespace Veterinaria.Datos.Repositorios;

public class UsuarioRepositorio : IUsuarioRepositorio
{
    private readonly VeterinariaContext _context;

    public UsuarioRepositorio(VeterinariaContext context) => _context = context;

    public Task<Usuario?> ObtenerPorCorreoAsync(string correo) =>
        _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Correo == correo);

    public async Task AgregarAsync(Usuario usuario) =>
        await _context.Usuarios.AddAsync(usuario);

    public Task GuardarAsync() => _context.SaveChangesAsync();
}
