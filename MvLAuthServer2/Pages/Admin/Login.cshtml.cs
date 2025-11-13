using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using System;

namespace MvLAuthServer2.Pages.Admin
{
    public class LoginModel(IConfiguration config) : PageModel
    {
        [BindProperty]
        public string Secret { get; set; }

        public void OnGet() { }

        public IActionResult OnPost()
        {
            var expected = config["Secrets:AdminKey"];
            if (Secret == expected)
            {
                Response.Cookies.Append("AdminAuth", "ok", new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddHours(2) // session length
                });
                return Redirect("/admin");
            }

            ModelState.AddModelError("", "Invalid key");
            return Page();
        }
    }
}
