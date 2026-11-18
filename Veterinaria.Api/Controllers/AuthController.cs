using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veterinaria.Entidades.Dtos;
using Veterinaria.Negocio;
using Veterinaria.Negocio.Servicios;

namespace Veterinaria.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthServicio _auth;

    public AuthController(IAuthServicio auth) => _auth = auth;

    [HttpPost("registro")]
    public async Task<IActionResult> Registro(RegistroDto dto)
    {
        try
        {
            await _auth.RegistrarAsync(dto);
            return StatusCode(StatusCodes.Status201Created);
        }
        catch (ReglaNegocioException ex)
        {
            return Conflict(new ProblemDetails { Title = "Operación no permitida", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<TokenDto>> Login(LoginDto dto)
    {
        var token = await _auth.LoginAsync(dto);
        if (token == null) return Unauthorized();
        return Ok(token);
    }

    [Authorize]
    [HttpGet("perfil")]
    public ActionResult<PerfilDto> Perfil()
    {
        return Ok(new PerfilDto
        {
            Correo = User.FindFirstValue(ClaimTypes.Email)!,
            Rol = User.FindFirstValue(ClaimTypes.Role)!
        });
    }
}
