using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MvLAuthServer2.Models.Database;
using System.Threading.Tasks;

namespace MvLAuthServer2.Pages.Admin.Users
{
    public class IndexModel(MvLDBContext context) : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; }

        [BindProperty(SupportsGet = true)]
        public string CurrentFilter { get; set; }

        public PaginatedList<UserEntry> UserEntries { get; set; }

        public async Task OnGetAsync(int? pageSize)
        {
            UserEntries = await PaginatedList<UserEntry>.CreateAsync(context.UserEntry.AsNoTracking().Include(ue => ue.AllNicknames).Include(ue => ue.AllIps), CurrentPage, pageSize ?? 25);
        }
    }
}
