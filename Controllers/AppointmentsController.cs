using cw6.DTOs;
using cw6.Services;
using Microsoft.AspNetCore.Mvc;

namespace cw6.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _service;

    public AppointmentsController(IAppointmentService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Get(string? status, string? patientLastName)
    {
        var result = await _service.GetAll(status, patientLastName);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetById(id);

        if (result == null)
            return NotFound(new ErrorResponseDto
            {
                Message = "Appointment not found"
            });

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateAppointmentRequestDto dto)
    {
        try
        {
            var id = await _service.Create(dto);

            return Created($"/api/appointments/{id}", new
            {
                Id = id
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ErrorResponseDto
            {
                Message = ex.Message
            });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateAppointmentRequestDto dto)
    {
        try
        {
            var ok = await _service.Update(id, dto);

            if (!ok)
                return NotFound(new ErrorResponseDto
                {
                    Message = "Appointment not found"
                });

            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(new ErrorResponseDto
            {
                Message = ex.Message
            });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var ok = await _service.Delete(id);

            if (!ok)
                return NotFound(new ErrorResponseDto
                {
                    Message = "Appointment not found"
                });

            return NoContent();
        }
        catch (Exception ex)
        {
            return Conflict(new ErrorResponseDto
            {
                Message = ex.Message
            });
        }
    }
}