using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NurseSchedule.Data;
using NurseSchedule.Models;

namespace NurseSchedule.Pages.Schedule;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _um;
    private readonly AuditService _audit;

    public IndexModel(ApplicationDbContext db, UserManager<ApplicationUser> um, AuditService audit)
    {
        _db = db;
        _um = um;
        _audit = audit;
    }

    public List<Department> Departments { get; set; } = new();
    public Department? Dept { get; set; }
    public List<ApplicationUser> AllMembers { get; set; } = new();
    public List<ApplicationUser> Rows { get; set; } = new();
    public List<ShiftType> Shifts { get; set; } = new();
    public List<DateOnly> Days { get; set; } = new();
    public HashSet<(string UserId, DateOnly Date, int ShiftId)> Selected { get; set; } = new();
    public bool IsSupervisor { get; set; }
    public bool ShowCoverage { get; set; }
    public DateOnly WeekStart { get; set; }
    public string? Message { get; set; }

    public async Task<IActionResult> OnGetAsync(int? dept, string? week)
    {
        var me = await _um.GetUserAsync(User);
        if (me == null) return Challenge();

        IsSupervisor = User.IsInRole("Supervisor");
        ShowCoverage = IsSupervisor || User.IsInRole("DepartmentHead");

        WeekStart = GetWeekStart(week);
        Days = Enumerable.Range(0, 7).Select(i => WeekStart.AddDays(i)).ToList();

        int? deptId;
        if (IsSupervisor)
        {
            Departments = await _db.Departments.OrderBy(d => d.Code).ToListAsync();
            deptId = dept ?? Departments.FirstOrDefault()?.Id;
        }
        else
        {
            deptId = me.DepartmentId;
        }

        if (deptId == null)
        {
            Message = "ما انربطت بأي قسم بعد. تواصل مع مسؤولة التمريض.";
            return Page();
        }

        Dept = await _db.Departments.FindAsync(deptId);
        if (Dept == null) return NotFound();

        Shifts = await _db.DepartmentShiftTypes
            .Where(x => x.DepartmentId == Dept.Id)
            .Select(x => x.ShiftType)
            .OrderBy(s => s.Id)
            .ToListAsync();

        AllMembers = await _db.Users
            .Where(u => u.DepartmentId == Dept.Id)
            .OrderBy(u => u.FullName)
            .ToListAsync();

        Rows = ShowCoverage ? AllMembers : AllMembers.Where(u => u.Id == me.Id).ToList();

        var ids = AllMembers.Select(u => u.Id).ToList();
        var end = WeekStart.AddDays(6);
        var list = await _db.ShiftAssignments
            .Where(a => ids.Contains(a.UserId) && a.Date >= WeekStart && a.Date <= end)
            .Select(a => new { a.UserId, a.Date, a.ShiftTypeId })
            .ToListAsync();
        Selected = list.Select(a => (a.UserId, a.Date, a.ShiftTypeId)).ToHashSet();

        return Page();
    }

    public async Task<IActionResult> OnPostToggleAsync(
        string userId, string date, int shiftId, int? dept, string? week)
    {
        var me = await _um.GetUserAsync(User);
        if (me == null) return Challenge();

        var target = await _db.Users.FindAsync(userId);
        if (target == null || !DateOnly.TryParseExact(date, "yyyy-MM-dd", out var d))
            return BadRequest();

        if (!CanEdit(me, target)) return Forbid();

        var allowed = await _db.DepartmentShiftTypes.AnyAsync(x =>
            x.DepartmentId == target.DepartmentId && x.ShiftTypeId == shiftId);
        if (!allowed) return BadRequest();

        var shift = await _db.ShiftTypes.FindAsync(shiftId);

        var existing = await _db.ShiftAssignments.FirstOrDefaultAsync(a =>
            a.UserId == userId && a.Date == d && a.ShiftTypeId == shiftId);

        string action;
        if (existing != null)
        {
            _db.ShiftAssignments.Remove(existing);
            action = "إلغاء وردية";
        }
        else
        {
            _db.ShiftAssignments.Add(new ShiftAssignment
            {
                UserId = userId, Date = d, ShiftTypeId = shiftId
            });
            action = "إضافة وردية";
        }

        await _db.SaveChangesAsync();
        await _audit.LogAsync(action,
            $"{shift?.Name} للموظف {target.FullName} بتاريخ {d:yyyy-MM-dd}");

        return RedirectToPage(new { dept, week });
    }

    private bool CanEdit(ApplicationUser me, ApplicationUser target)
    {
        if (User.IsInRole("Supervisor")) return true;
        if (User.IsInRole("DepartmentHead")
            && me.DepartmentId != null
            && me.DepartmentId == target.DepartmentId) return true;
        return me.Id == target.Id;
    }

    private static DateOnly GetWeekStart(string? week)
    {
        var d = DateOnly.TryParseExact(week, "yyyy-MM-dd", out var p)
            ? p
            : DateOnly.FromDateTime(DateTime.Today);
        return d.AddDays(-(int)d.DayOfWeek);
    }
}
