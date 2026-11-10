using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NurseSchedule.Data;
using NurseSchedule.Models;

namespace NurseSchedule.Pages.Admin;

[Authorize(Roles = "Supervisor")]
public class ShiftsModel : PageModel
{
    private readonly ApplicationDbContext _db;
    public ShiftsModel(ApplicationDbContext db) => _db = db;

    public List<ShiftType> Shifts { get; set; } = new();

    [BindProperty] public string Name { get; set; } = "";
    [BindProperty] public string From { get; set; } = "07:00";
    [BindProperty] public string To { get; set; } = "15:00";

    public async Task OnGetAsync()
    {
        Shifts = await _db.ShiftTypes.OrderBy(s => s.Id).ToListAsync();
    }

    public async Task<IActionResult> OnPostAddAsync()
    {
        var name = Name?.Trim() ?? "";
        if (name.Length == 0 || name.Length > 40)
        {
            TempData["Error"] = "اكتب اسم للوردية (حتى 40 حرف)";
            return RedirectToPage();
        }

        var shift = new ShiftType
        {
            Code = "c" + DateTime.UtcNow.Ticks,
            Name = name,
            TimeText = $"{From}–{To}",
            IsCustom = true
        };
        _db.ShiftTypes.Add(shift);
        await _db.SaveChangesAsync();

        var deptIds = await _db.Departments.Select(d => d.Id).ToListAsync();
        foreach (var id in deptIds)
            _db.DepartmentShiftTypes.Add(new DepartmentShiftType
            {
                DepartmentId = id,
                ShiftTypeId = shift.Id
            });
        await _db.SaveChangesAsync();

        TempData["Ok"] = "تمت إضافة الوردية لكل الأقسام";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var shift = await _db.ShiftTypes.FindAsync(id);
        if (shift == null) return NotFound();
        if (!shift.IsCustom) return BadRequest();

        await _db.ShiftAssignments.Where(a => a.ShiftTypeId == id).ExecuteDeleteAsync();
        await _db.DepartmentShiftTypes.Where(x => x.ShiftTypeId == id).ExecuteDeleteAsync();
        _db.ShiftTypes.Remove(shift);
        await _db.SaveChangesAsync();

        TempData["Ok"] = "تم حذف الوردية";
        return RedirectToPage();
    }
}