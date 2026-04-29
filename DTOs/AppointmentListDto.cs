namespace cw6.DTOs;

public class AppointmentListDto
{
    public int IdAppointment { get; set; }
    public DateTime AppointmentDate { get; set; }
    public string Status { get; set; } = "";
    public string Reason { get; set; } = "";
    public string PatientFullName { get; set; } = String.Empty;
    public string PatientEmail { get; set; } = String.Empty;
}