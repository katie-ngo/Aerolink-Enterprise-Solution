using Aerolink_Enterprise_Solution.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Aerolink_Enterprise_Solution.Pages
{
    public class SignInModel : PageModel
    {

        private readonly IHrService _hrService;

        public SignInModel(IHrService HrService)
        {
            _hrService = HrService;
        }

        [BindProperty]
        public string EmployeeId { get; set; } = string.Empty;

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }

        public void OnGet()
        {

        }
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ErrorMessage = "Please correct the input errors and try again.";
                return Page();
            }
            if (string.IsNullOrWhiteSpace(EmployeeId) || string.IsNullOrWhiteSpace(Email))
            {
                ErrorMessage = "Employee ID and Email are required.";
                return Page();
            }
            var emailChecker = new System.ComponentModel.DataAnnotations.EmailAddressAttribute();
            if (!emailChecker.IsValid(Email))
            {
                ErrorMessage = "Enter a valid email address.";
                return Page();
            }
            bool isEmployeeIdValid = int.TryParse(EmployeeId, out _);
            if (!isEmployeeIdValid)
            {
                ErrorMessage = "Employee ID must be a valid number.";
                return Page();
            }

            var (isSuccess, employee, errorMessage) = await _hrService.ValidateStaffLoginAsync(EmployeeId, Email);
            if (!isSuccess || employee == null)
            {
                ErrorMessage = errorMessage;
                return Page();
            }
            var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, employee.EmployeeId.ToString()),
                    new Claim(ClaimTypes.Name, $"{employee.FirstName} {employee.LastName}"),
                    new Claim(ClaimTypes.Email, employee.Email),
                    new Claim(ClaimTypes.Role, employee.JobTitle)
                };
            var identity = new ClaimsIdentity(claims, "AeroLinkAuth");
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync("AeroLinkAuth", principal);
            // If successful, redirect to the Baggage Operations page
            return RedirectToPage("/Index");
        }
        public async Task<IActionResult> OnPostLogoutAsync()
        {
            // Destroys the "AeroLinkAuth" session cookie
            await HttpContext.SignOutAsync("AeroLinkAuth");

            // Redirect back to the login screen
            return RedirectToPage("/Index");
        }
    }
}