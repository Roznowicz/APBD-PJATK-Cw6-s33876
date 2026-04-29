using cw6.DTOs;

namespace cw6.Services;

public interface IAppointmentService
{
    Task<List<AppointmentListDto>> GetAll(
        string? status,
        string? patientLastName);

    Task<AppointmentDetailsDto?> GetById(int id);

    Task<int> Create(CreateAppointmentRequestDto dto);

    Task<bool> Update(int id, UpdateAppointmentRequestDto dto);

    Task<bool> Delete(int id);
}