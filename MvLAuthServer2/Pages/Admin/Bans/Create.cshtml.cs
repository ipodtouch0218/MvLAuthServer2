using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MvLAuthServer2.Models.Database;
using System;
using System.Threading.Tasks;

namespace MvLAuthServer2.Pages.Admin.Bans
{
    public class CreateModel(MvLDBContext context) : PageModel
    {

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public Ban? Ban { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (Ban != null)
            {
                Ban.Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                context.Ban.Add(Ban);
            }
            await context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
