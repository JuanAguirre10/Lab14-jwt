using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Entidades.Dtos;

public class RegistroDto
{
    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no es válido.")]
    [StringLength(100, ErrorMessage = "El correo no puede tener más de 100 caracteres.")]
    public string Correo { get; set; } = null!;

    [Required(ErrorMessage = "La clave es obligatoria.")]
    [StringLength(50, MinimumLength = 6, ErrorMessage = "La clave debe tener entre 6 y 50 caracteres.")]
    public string Clave { get; set; } = null!;
}
