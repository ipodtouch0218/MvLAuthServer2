using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MvLAuthServer2.Models.Database;
using System.Linq;
using System.Threading.Tasks;

namespace MvLAuthServer2.Pages.Admin.News
{
    public class EditModel(MvLDBContext context) : PageModel
    {

        [BindProperty]
        public NewsBoardPost? NewsPost { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            NewsPost = await context.NewsBoardPost.FirstOrDefaultAsync(m => m.Id == id);

            if (NewsPost == null)
            {
                return NotFound();
            }
            return Page();
        }

        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (NewsPost != null)
            {
                context.Attach(NewsPost).State = EntityState.Modified;

                try
                {
                    await context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!Exists(NewsPost.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return RedirectToPage("./Index");
        }

        private bool Exists(int id)
        {
            return context.NewsBoardPost.Any(e => e.Id == id);
        }
    }
}
