using Microsoft.AspNetCore.Identity;

namespace NurseSchedule.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = "";
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }
}

public class Department
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string NameAr { get; set; } = "";
    public string NameEn { get; set; } = "";
    public List<ApplicationUser> Users { get; set; } = new();
    public List<DepartmentShiftType> ShiftTypes { get; set; } = new();
}

public class ShiftType
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string TimeText { get; set; } = "";
    public bool IsCustom { get; set; }
}

public class DepartmentShiftType
{
    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;
    public int ShiftTypeId { get; set; }
    public ShiftType ShiftType { get; set; } = null!;
}

public class ShiftAssignment
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public ApplicationUser User { get; set; } = null!;
    public DateOnly Date { get; set; }
    public int ShiftTypeId { get; set; }
    public ShiftType ShiftType { get; set; } = null!;
}