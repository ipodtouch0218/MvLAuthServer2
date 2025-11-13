using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MvLAuthServer2.Models.Database;
using System;
using System.Threading.Tasks;

namespace MvLAuthServer2.Pages.Admin.News
{
    public class CreateModel(MvLDBContext context) : PageModel
    {
        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public NewsBoardPost? NewsPost { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (NewsPost != null)
            {
                NewsPost.Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                context.NewsBoardPost.Add(NewsPost);
            }
            await context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
