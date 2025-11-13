using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace MvLAuthServer2.Endpoints
{
    public static class NewsEndpoints
    {
        public static IEndpointRouteBuilder MapNewsEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var newsGroup = endpoints.MapGroup("/news");

            newsGroup.MapGet("/all", GetAll);
            newsGroup.MapGet("/{id}", GetOne);
            newsGroup.MapGet("/", GetAvailableIds);

            return endpoints;
        }

        private static async Task<IResult> GetAll(HttpContext context, MvLDBContext database)
        {
            return Results.Json(await database.NewsBoardPost.AsNoTracking()
                .OrderByDescending(nbe => nbe.Created)
                .ToArrayAsync());
        }

        private static async Task<IResult> GetOne(HttpContext context, int id, MvLDBContext database)
        {
            var result = await database.NewsBoardPost.FindAsync(id);
            if (result != null)
            {
                return Results.Json(result);
            }
            else
            {
                return Results.NotFound();
            }
        }

        private static async Task<IResult> GetAvailableIds(HttpContext context, MvLDBContext database)
        {
            return Results.Json(await database.NewsBoardPost.AsNoTracking()
                .OrderByDescending(nbe => nbe.Created)
                .Select(nbe => nbe.Id)
                .ToArrayAsync());
        }
    }
}
