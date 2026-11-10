using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NurseSchedule.Data;
using NurseSchedule.Models;

namespace NurseSchedule.Pages.Admin;

[Authorize(Roles = "Supervisor")]
public class AuditModel : PageModel
{
    private readonly ApplicationDbContext _db;
    public AuditModel(ApplicationDbContext db) => _db = db;

    public List<AuditLog> Logs { get; set; } = new();

    public async Task OnGetAsync()
    {
        Logs = await _db.AuditLogs
            .OrderByDescending(l => l.Id)
            .Take(200)
            .ToListAsync();
    }
}
