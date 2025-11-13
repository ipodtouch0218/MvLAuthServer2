using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MvLAuthServer2.Models.Database;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MvLAuthServer2.Pages.Admin.News
{
    public class IndexModel(MvLDBContext context) : PageModel
    {

        public IList<NewsBoardPost>? NewsPosts { get; set; }

        public async Task OnGetAsync()
        {
            NewsPosts = await context.NewsBoardPost.ToListAsync();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var news = await context.NewsBoardPost.FindAsync(id);

            if (news != null)
            {
                context.NewsBoardPost.Remove(news);
                await context.SaveChangesAsync();
            }

            return RedirectToPage();
        }
    }
}
