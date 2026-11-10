using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NurseSchedule.Models;

namespace NurseSchedule.Data;

public static class DbInitializer
{
    public static readonly string[] Roles = { "Supervisor", "DepartmentHead", "Nurse" };

    public static async Task SeedAsync(IServiceProvider sp, IConfiguration config)
    {
        var db = sp.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        var roleMgr = sp.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var r in Roles)
            if (!await roleMgr.RoleExistsAsync(r))
                await roleMgr.CreateAsync(new IdentityRole(r));

        if (!await db.ShiftTypes.AnyAsync())
        {
            db.ShiftTypes.AddRange(
                new ShiftType { Code = "m",   Name = "صباحي",       TimeText = "7ص–3م" },
                new ShiftType { Code = "e",   Name = "مسائي",       TimeText = "3م–11م" },
                new ShiftType { Code = "n",   Name = "ليلي",        TimeText = "11م–7ص" },
                new ShiftType { Code = "d12", Name = "نهاري طويل",  TimeText = "7ص–7م" },
                new ShiftType { Code = "d8",  Name = "نهاري",       TimeText = "8ص–4م" },
                new ShiftType { Code = "oc",  Name = "On Call",     TimeText = "عند الحاجة" });
            await db.SaveChangesAsync();
        }

        if (!await db.Departments.AnyAsync())
        {
            var depts = new (string Code, string Ar, string En)[]
            {
                ("A","الباطني","Medical Ward"), ("B","الجراحة","Surgical Ward"),
                ("C","العظام","Orthopedic"), ("D","الأطفال","Pediatric"),
                ("E","النسائية والتوليد","OB/GYN"), ("F","الطوارئ","Emergency"),
                ("G","العناية الحثيثة","ICU"), ("H","عناية القلب","CCU"),
                ("I","الخداج","NICU"), ("J","عناية الأطفال","PICU"),
                ("K","غسيل الكلى","Hemodialysis"), ("L","قسطرة القلب","Cath Lab"),
                ("M","العمليات","Operating Room"), ("N","الإفاقة","Recovery"),
                ("O","غرفة الولادة","Delivery Room"), ("P","الأورام","Oncology"),
                ("Q","الأعصاب","Neurology"), ("R","الحروق","Burn Unit"),
                ("S","العزل","Isolation"), ("T","المناظير","Endoscopy")
            };

            var special = new Dictionary<string, string[]>
            {
                ["K"] = new[] { "d12", "oc" },
                ["L"] = new[] { "d8", "oc" },
                ["M"] = new[] { "m", "oc" },
                ["N"] = new[] { "m", "e", "oc" },
                ["P"] = new[] { "m", "e" },
                ["T"] = new[] { "d8", "oc" }
            };

            var shifts = await db.ShiftTypes.ToDictionaryAsync(s => s.Code);

            foreach (var d in depts)
            {
                var dept = new Department { Code = d.Code, NameAr = d.Ar, NameEn = d.En };
                var codes = special.TryGetValue(d.Code, out var s) ? s : new[] { "m", "e", "n" };
                foreach (var c in codes)
                    dept.ShiftTypes.Add(new DepartmentShiftType { ShiftType = shifts[c] });
                db.Departments.Add(dept);
            }
            await db.SaveChangesAsync();
        }

        var userMgr = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var email = config["Admin:Email"];
        var pass = config["Admin:Password"];

        if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(pass)
            && await userMgr.FindByEmailAsync(email) == null)
        {
            var admin = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = "مسؤولة التمريض"
            };
            var res = await userMgr.CreateAsync(admin, pass);
            if (!res.Succeeded)
                throw new Exception(string.Join(", ", res.Errors.Select(e => e.Description)));
            await userMgr.AddToRoleAsync(admin, "Supervisor");
        }

          if (config["Seed:Demo"] == "true" && !string.IsNullOrEmpty(pass))
        {
            var deptA = await db.Departments.FirstAsync(d => d.Code == "A");
            var demo = new[]
            {
                ("head@example.com", "مسؤول قسم الباطني", "DepartmentHead"),
                ("nurse1@example.com", "ممرض تجريبي 1", "Nurse"),
                ("nurse2@example.com", "ممرضة تجريبية 2", "Nurse")
            };
            foreach (var (mail, name, role) in demo)
            {
                if (await userMgr.FindByEmailAsync(mail) != null) continue;
                var u = new ApplicationUser
                {
                    UserName = mail, Email = mail, EmailConfirmed = true,
                    FullName = name, DepartmentId = deptA.Id
                };
                var r = await userMgr.CreateAsync(u, pass);
                if (r.Succeeded) await userMgr.AddToRoleAsync(u, role);
            }
        }
    }
}