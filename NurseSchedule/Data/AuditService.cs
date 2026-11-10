using NurseSchedule.Models;

namespace NurseSchedule.Data;

public class AuditService
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _http;

    public AuditService(ApplicationDbContext db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }

    public async Task LogAsync(string action, string details)
    {
        var name = _http.HttpContext?.User.Identity?.Name ?? "system";
        _db.AuditLogs.Add(new AuditLog
        {
            ActorName = name,
            Action = action,
            Details = details
        });
        await _db.SaveChangesAsync();
    }
}
