using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veterinaria.Entidades.Dtos;
using Veterinaria.Negocio;
using Veterinaria.Negocio.Servicios;

namespace Veterinaria.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class PropietariosController : ControllerBase
{
    private readonly IPropietarioServicio _servicio;

    public PropietariosController(IPropietarioServicio servicio) => _servicio = servicio;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PropietarioDto>>> Get(string? buscar)
    {
        return Ok(await _servicio.ListarAsync(buscar));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PropietarioDto>> Get(int id)
    {
        var propietario = await _servicio.ObtenerAsync(id);
        if (propietario == null) return NotFound();
        return Ok(propietario);
    }

    [HttpGet("{id}/mascotas")]
    public async Task<ActionResult<IEnumerable<MascotaDto>>> GetMascotas(int id)
    {
        var mascotas = await _servicio.ListarMascotasAsync(id);
        if (mascotas == null) return NotFound();
        return Ok(mascotas);
    }

    [HttpPost]
    public async Task<ActionResult<PropietarioDto>> Post(PropietarioCrearDto dto)
    {
        try
        {
            var creado = await _servicio.CrearAsync(dto);
            return CreatedAtAction(nameof(Get), new { id = creado.Id }, creado);
        }
        catch (ReglaNegocioException ex)
        {
            return Conflict(new ProblemDetails { Title = "Operación no permitida", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Put(int id, PropietarioActualizarDto dto)
    {
        if (!await _servicio.ActualizarAsync(id, dto)) return NotFound();
        return NoContent();
    }

    [Authorize(Roles = "Recepcionista")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            if (!await _servicio.EliminarAsync(id)) return NotFound();
            return NoContent();
        }
        catch (ReglaNegocioException ex)
        {
            return Conflict(new ProblemDetails { Title = "Operación no permitida", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
        }
    }
}
