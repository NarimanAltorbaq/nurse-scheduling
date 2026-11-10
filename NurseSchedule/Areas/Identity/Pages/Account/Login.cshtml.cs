using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NurseSchedule.Models;

namespace NurseSchedule.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signIn;
    public LoginModel(SignInManager<ApplicationUser> signIn) => _signIn = signIn;

    [BindProperty] public InputModel Input { get; set; } = new();
    public string ReturnUrl { get; set; } = "/Schedule";
    public string? Error { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "الإيميل مطلوب")]
        [EmailAddress(ErrorMessage = "الإيميل غير صحيح")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "كلمة المرور مطلوبة")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        public bool RememberMe { get; set; }
    }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? "/Schedule";
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? "/Schedule";
        if (!ModelState.IsValid) return Page();

        var result = await _signIn.PasswordSignInAsync(
            Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded) return LocalRedirect(ReturnUrl);

        Error = result.IsLockedOut
            ? "الحساب مقفل مؤقتاً بسبب محاولات كثيرة. حاول لاحقاً."
            : "الإيميل أو كلمة المرور غير صحيحة";
        return Page();
    }
}
