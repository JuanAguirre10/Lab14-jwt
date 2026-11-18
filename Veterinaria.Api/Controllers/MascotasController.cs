using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veterinaria.Entidades.Dtos;
using Veterinaria.Negocio;
using Veterinaria.Negocio.Servicios;

namespace Veterinaria.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class MascotasController : ControllerBase
{
    private readonly IMascotaServicio _servicio;

    public MascotasController(IMascotaServicio servicio) => _servicio = servicio;

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MascotaDto>>> Get(string? especie, int? propietarioId, string? buscar)
    {
        return Ok(await _servicio.ListarAsync(especie, propietarioId, buscar));
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<ActionResult<MascotaDto>> Get(int id)
    {
        var mascota = await _servicio.ObtenerAsync(id);
        if (mascota == null) return NotFound();
        return Ok(mascota);
    }

    [HttpPost]
    public async Task<ActionResult<MascotaDto>> Post(MascotaCrearDto dto)
    {
        try
        {
            var creada = await _servicio.CrearAsync(dto);
            return CreatedAtAction(nameof(Get), new { id = creada.Id }, creada);
        }
        catch (DatoInvalidoException ex)
        {
            ModelState.AddModelError(ex.Campo, ex.Message);
            return ValidationProblem(ModelState);
        }
        catch (ReglaNegocioException ex)
        {
            return Conflict(new ProblemDetails { Title = "Operación no permitida", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
        }
    }

    [Authorize(Roles = "Veterinario")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Put(int id, MascotaActualizarDto dto)
    {
        try
        {
            if (!await _servicio.ActualizarAsync(id, dto)) return NotFound();
            return NoContent();
        }
        catch (DatoInvalidoException ex)
        {
            ModelState.AddModelError(ex.Campo, ex.Message);
            return ValidationProblem(ModelState);
        }
        catch (ReglaNegocioException ex)
        {
            return Conflict(new ProblemDetails { Title = "Operación no permitida", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
        }
    }

    [Authorize(Roles = "Veterinario")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!await _servicio.EliminarAsync(id)) return NotFound();
        return NoContent();
    }
}
