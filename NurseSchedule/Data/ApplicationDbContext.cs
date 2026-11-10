using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NurseSchedule.Models;

namespace NurseSchedule.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<ShiftType> ShiftTypes => Set<ShiftType>();
    public DbSet<DepartmentShiftType> DepartmentShiftTypes => Set<DepartmentShiftType>();
    public DbSet<ShiftAssignment> ShiftAssignments => Set<ShiftAssignment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<DepartmentShiftType>().HasKey(x => new { x.DepartmentId, x.ShiftTypeId });
        b.Entity<Department>().HasIndex(x => x.Code).IsUnique();
        b.Entity<ShiftType>().HasIndex(x => x.Code).IsUnique();
        b.Entity<ShiftAssignment>()
            .HasIndex(x => new { x.UserId, x.Date, x.ShiftTypeId }).IsUnique();
    }
}