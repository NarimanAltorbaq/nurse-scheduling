using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NurseSchedule.Data;
using NurseSchedule.Models;

namespace NurseSchedule.Pages.Admin;

[Authorize(Roles = "Supervisor")]
public class UsersModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _um;

    public UsersModel(ApplicationDbContext db, UserManager<ApplicationUser> um)
    {
        _db = db;
        _um = um;
    }

    public record UserRow(string Id, string FullName, string Email, string Role, string DeptName);

    public List<UserRow> Users { get; set; } = new();
    public List<Department> Departments { get; set; } = new();

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "الاسم مطلوب")]
        public string FullName { get; set; } = "";

        [Required(ErrorMessage = "الإيميل مطلوب")]
        [EmailAddress(ErrorMessage = "الإيميل غير صحيح")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "كلمة المرور مطلوبة")]
        public string Password { get; set; } = "";

        [Required]
        public string Role { get; set; } = "Nurse";

        public int? DepartmentId { get; set; }
    }

    public async Task OnGetAsync() => await LoadAsync();

    public async Task<IActionResult> OnPostAddAsync()
    {
        if (!DbInitializer.Roles.Contains(Input.Role))
            ModelState.AddModelError("", "دور غير صحيح");

        if (Input.Role != "Supervisor" && Input.DepartmentId == null)
            ModelState.AddModelError("", "اختار القسم لهالدور");

        if (ModelState.IsValid)
        {
            var user = new ApplicationUser
            {
                UserName = Input.Email,
                Email = Input.Email,
                EmailConfirmed = true,
                FullName = Input.FullName.Trim(),
                DepartmentId = Input.Role == "Supervisor" ? null : Input.DepartmentId
            };

            var result = await _um.CreateAsync(user, Input.Password);
            if (result.Succeeded)
            {
                await _um.AddToRoleAsync(user, Input.Role);
                TempData["Ok"] = "تمت إضافة المستخدم";
                return RedirectToPage();
            }

            foreach (var e in result.Errors)
                ModelState.AddModelError("", e.Description);
        }

        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string id)
    {
        var me = await _um.GetUserAsync(User);
        if (me != null && me.Id == id)
        {
            TempData["Error"] = "ما بتقدر تحذف حسابك";
            return RedirectToPage();
        }

        var user = await _um.FindByIdAsync(id);
        if (user == null) return NotFound();

        await _db.ShiftAssignments.Where(a => a.UserId == id).ExecuteDeleteAsync();
        await _um.DeleteAsync(user);

        TempData["Ok"] = "تم حذف المستخدم";
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        Departments = await _db.Departments.OrderBy(d => d.Code).ToListAsync();

        var users = await _db.Users.Include(u => u.Department)
            .OrderBy(u => u.FullName).ToListAsync();

        Users = new List<UserRow>();
        foreach (var u in users)
        {
            var roles = await _um.GetRolesAsync(u);
            Users.Add(new UserRow(
                u.Id,
                u.FullName,
                u.Email ?? "",
                roles.FirstOrDefault() ?? "",
                u.Department?.NameAr ?? "الكل"));
        }
    }
}