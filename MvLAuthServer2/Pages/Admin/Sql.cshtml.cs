using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MvLAuthServer2.Pages.Admin
{
    public class SqlModel(MvLDBContext context) : PageModel
    {
        [BindProperty]
        public string? Command { get; set; }
        public int? ExecuteResults { get; set; }
        public List<Dictionary<string, object?>> TableResults { get; set; }
        public Exception SqlException { get; set; }

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (Command != null)
            {
                try
                {
                    if (Command.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                    {
                        await using var conn = context.Database.GetDbConnection();
                        await conn.OpenAsync();

                        await using var cmd = conn.CreateCommand();
                        cmd.CommandText = Command;

                        var reader = await cmd.ExecuteReaderAsync();
                        var table = new List<Dictionary<string, object?>>();

                        while (await reader.ReadAsync())
                        {
                            var row = new Dictionary<string, object?>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                            }
                            table.Add(row);
                        }

                        TableResults = table;
                    }
                    else
                    {
                        int rows = await context.Database.ExecuteSqlRawAsync(Command);
                        ExecuteResults = rows;
                    }
                }
                catch (Exception e)
                {
                    SqlException = e;
                }
            }

            return Page();
        }
    }
}
