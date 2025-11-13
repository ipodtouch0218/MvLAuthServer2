using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MvLAuthServer2.Models.Database;
using System.Linq;
using System.Threading.Tasks;

namespace MvLAuthServer2.Pages.Admin.Users
{
    public class EditModel(MvLDBContext context) : PageModel
    {

        [BindProperty]
        public UserEntry? UserEntry { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            UserEntry = await context.UserEntry.FirstOrDefaultAsync(m => m.Id == id);

            if (UserEntry == null)
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

            if (UserEntry != null)
            {
                context.Attach(UserEntry).State = EntityState.Modified;

                try
                {
                    await context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!Exists(UserEntry.Id))
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
            return context.UserEntry.Any(e => e.Id == id);
        }
    }
}
