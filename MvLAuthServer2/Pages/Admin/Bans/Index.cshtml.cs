using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MvLAuthServer2.Models.Database;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MvLAuthServer2.Pages.Admin.Bans
{
    public class IndexModel(MvLDBContext context) : PageModel
    {

        public IList<Ban>? Bans { get; set; }

        public async Task OnGetAsync()
        {
            Bans = await context.Ban.ToListAsync();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var ban = await context.Ban.FindAsync(id);

            if (ban != null)
            {
                context.Ban.Remove(ban);
                await context.SaveChangesAsync();
            }

            return RedirectToPage();
        }
    }
}
