namespace cw6.DTOs;

public class AppointmentDetailsDto
{
    public int IdAppointment { get; set; }
    public DateTime AppointmentDate { get; set; }
    public string Status { get; set; } = String.Empty;
    public string Reason { get; set; } = String.Empty;
    public string? InternalNotes { get; set; }
    public DateTime CreatedAt { get; set; }

    public string PatientFullName { get; set; } = String.Empty;
    public string PatientEmail { get; set; } = String.Empty;
    public string PatientPhone { get; set; } = String.Empty;

    public string DoctorFullName { get; set; } = String.Empty;
    public string LicenseNumber { get; set; } = String.Empty;
    public string Specialization { get; set; } = String.Empty;
}