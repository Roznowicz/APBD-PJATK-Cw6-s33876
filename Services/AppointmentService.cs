using cw6.DTOs;
using Microsoft.Data.SqlClient;

namespace cw6.Services;

public class AppointmentService : IAppointmentService
{
    private readonly IConfiguration _configuration;

    public AppointmentService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private string cs => _configuration.GetConnectionString("DefaultConnection")!;

    public async Task<List<AppointmentListDto>> GetAll(string? status, string? patientLastName)
    {
        var list = new List<AppointmentListDto>();

        await using var con = new SqlConnection(cs);
        await con.OpenAsync();

        var sql = """
        SELECT a.IdAppointment,a.AppointmentDate,a.Status,a.Reason,
        p.FirstName + ' ' + p.LastName,
        p.Email
        FROM Appointments a
        JOIN Patients p ON p.IdPatient=a.IdPatient
        WHERE (@status IS NULL OR a.Status=@status)
        AND (@last IS NULL OR p.LastName=@last)
        ORDER BY a.AppointmentDate
        """;

        await using var cmd = new SqlCommand(sql, con);

        cmd.Parameters.AddWithValue("@status", (object?)status ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@last", (object?)patientLastName ?? DBNull.Value);

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            list.Add(new AppointmentListDto
            {
                IdAppointment = reader.GetInt32(0),
                AppointmentDate = reader.GetDateTime(1),
                Status = reader.GetString(2),
                Reason = reader.GetString(3),
                PatientFullName = reader.GetString(4),
                PatientEmail = reader.GetString(5)
            });
        }

        return list;
    }

    public async Task<AppointmentDetailsDto?> GetById(int id)
    {
        await using var con = new SqlConnection(cs);
        await con.OpenAsync();

        var sql = """
        SELECT a.IdAppointment,a.AppointmentDate,a.Status,a.Reason,
        a.InternalNotes,a.CreatedAt,
        p.FirstName + ' ' + p.LastName,
        p.Email,p.PhoneNumber,
        d.FirstName + ' ' + d.LastName,
        d.LicenseNumber,
        s.Name
        FROM Appointments a
        JOIN Patients p ON p.IdPatient=a.IdPatient
        JOIN Doctors d ON d.IdDoctor=a.IdDoctor
        JOIN Specializations s ON s.IdSpecialization=d.IdSpecialization
        WHERE a.IdAppointment=@id
        """;

        await using var cmd = new SqlCommand(sql, con);
        cmd.Parameters.AddWithValue("@id", id);

        await using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return null;

        return new AppointmentDetailsDto
        {
            IdAppointment = reader.GetInt32(0),
            AppointmentDate = reader.GetDateTime(1),
            Status = reader.GetString(2),
            Reason = reader.GetString(3),
            InternalNotes = reader.IsDBNull(4) ? null : reader.GetString(4),
            CreatedAt = reader.GetDateTime(5),
            PatientFullName = reader.GetString(6),
            PatientEmail = reader.GetString(7),
            PatientPhone = reader.GetString(8),
            DoctorFullName = reader.GetString(9),
            LicenseNumber = reader.GetString(10),
            Specialization = reader.GetString(11)
        };
    }

    public async Task<int> Create(CreateAppointmentRequestDto dto)
    {
        if (dto.AppointmentDate < DateTime.Now)
            throw new Exception("Appointment date cannot be in the past");

        if (string.IsNullOrWhiteSpace(dto.Reason) || dto.Reason.Length > 250)
            throw new Exception("Invalid reason");

        await using var con = new SqlConnection(cs);
        await con.OpenAsync();

        var checkPatient = new SqlCommand(
            "SELECT COUNT(*) FROM Patients WHERE IdPatient=@id AND IsActive=1", con);
        checkPatient.Parameters.AddWithValue("@id", dto.IdPatient);

        if ((int)await checkPatient.ExecuteScalarAsync() == 0)
            throw new Exception("Patient not found or inactive");

        var checkDoctor = new SqlCommand(
            "SELECT COUNT(*) FROM Doctors WHERE IdDoctor=@id AND IsActive=1", con);
        checkDoctor.Parameters.AddWithValue("@id", dto.IdDoctor);

        if ((int)await checkDoctor.ExecuteScalarAsync() == 0)
            throw new Exception("Doctor not found or inactive");

        var conflict = new SqlCommand(
            "SELECT COUNT(*) FROM Appointments WHERE IdDoctor=@d AND AppointmentDate=@date", con);

        conflict.Parameters.AddWithValue("@d", dto.IdDoctor);
        conflict.Parameters.AddWithValue("@date", dto.AppointmentDate);

        if ((int)await conflict.ExecuteScalarAsync() > 0)
            throw new Exception("Doctor already has appointment");

        var sql = """
        INSERT INTO Appointments
        (IdPatient,IdDoctor,AppointmentDate,Status,Reason)
        OUTPUT INSERTED.IdAppointment
        VALUES(@p,@d,@date,'Scheduled',@reason)
        """;

        await using var cmd = new SqlCommand(sql, con);

        cmd.Parameters.AddWithValue("@p", dto.IdPatient);
        cmd.Parameters.AddWithValue("@d", dto.IdDoctor);
        cmd.Parameters.AddWithValue("@date", dto.AppointmentDate);
        cmd.Parameters.AddWithValue("@reason", dto.Reason);

        return (int)await cmd.ExecuteScalarAsync();
    }

    public async Task<bool> Update(int id, UpdateAppointmentRequestDto dto)
{
    await using var con = new SqlConnection(cs);
    await con.OpenAsync();

    var checkPatient = new SqlCommand(
        "SELECT COUNT(*) FROM Patients WHERE IdPatient=@id AND IsActive=1", con);

    checkPatient.Parameters.AddWithValue("@id", dto.IdPatient);

    if ((int)await checkPatient.ExecuteScalarAsync() == 0)
        throw new Exception("Patient not found or inactive");


    var checkDoctor = new SqlCommand(
        "SELECT COUNT(*) FROM Doctors WHERE IdDoctor=@id AND IsActive=1", con);

    checkDoctor.Parameters.AddWithValue("@id", dto.IdDoctor);

    if ((int)await checkDoctor.ExecuteScalarAsync() == 0)
        throw new Exception("Doctor not found or inactive");


    var exists = new SqlCommand(
        "SELECT Status, AppointmentDate FROM Appointments WHERE IdAppointment=@id", con);

    exists.Parameters.AddWithValue("@id", id);

    await using var reader = await exists.ExecuteReaderAsync();

    if (!await reader.ReadAsync())
        return false;

    var oldStatus = reader.GetString(0);
    var oldDate = reader.GetDateTime(1);

    await reader.CloseAsync();

    if (oldStatus == "Completed" && oldDate != dto.AppointmentDate)
        throw new Exception("Completed appointment cannot change date");

    if (dto.Status != "Scheduled" &&
        dto.Status != "Completed" &&
        dto.Status != "Cancelled")
        throw new Exception("Invalid status");


    var conflict = new SqlCommand("""
        SELECT COUNT(*)
        FROM Appointments
        WHERE IdDoctor=@doctor
        AND AppointmentDate=@date
        AND IdAppointment<>@id
        """, con);

    conflict.Parameters.AddWithValue("@doctor", dto.IdDoctor);
    conflict.Parameters.AddWithValue("@date", dto.AppointmentDate);
    conflict.Parameters.AddWithValue("@id", id);

    if ((int)await conflict.ExecuteScalarAsync() > 0)
        throw new Exception("Doctor already busy");


    var sql = """
        UPDATE Appointments SET
        IdPatient=@patient,
        IdDoctor=@doctor,
        AppointmentDate=@date,
        Status=@status,
        Reason=@reason,
        InternalNotes=@notes
        WHERE IdAppointment=@id
        """;

    await using var cmd = new SqlCommand(sql, con);

    cmd.Parameters.AddWithValue("@patient", dto.IdPatient);
    cmd.Parameters.AddWithValue("@doctor", dto.IdDoctor);
    cmd.Parameters.AddWithValue("@date", dto.AppointmentDate);
    cmd.Parameters.AddWithValue("@status", dto.Status);
    cmd.Parameters.AddWithValue("@reason", dto.Reason);
    cmd.Parameters.AddWithValue("@notes",
        (object?)dto.InternalNotes ?? DBNull.Value);
    cmd.Parameters.AddWithValue("@id", id);

    return await cmd.ExecuteNonQueryAsync() > 0;
}

    public async Task<bool> Delete(int id)
    {
        await using var con = new SqlConnection(cs);
        await con.OpenAsync();

        var check = new SqlCommand(
            "SELECT Status FROM Appointments WHERE IdAppointment=@id", con);

        check.Parameters.AddWithValue("@id", id);

        var result = await check.ExecuteScalarAsync();

        if (result == null)
            return false;

        if (result.ToString() == "Completed")
            throw new Exception("Completed appointment cannot be deleted");

        var cmd = new SqlCommand(
            "DELETE FROM Appointments WHERE IdAppointment=@id", con);

        cmd.Parameters.AddWithValue("@id", id);

        await cmd.ExecuteNonQueryAsync();

        return true;
    }
}