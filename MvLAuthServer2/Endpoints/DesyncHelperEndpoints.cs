using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MvLAuthServer2.Endpoints
{
    public static class DesyncHelperEndpoints
    {
        public static IEndpointRouteBuilder MapDesyncHelperEndpoints(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapPost("/desync", Desync);

            return endpoints;
        }

        private static async Task<IResult> Desync(HttpContext context, [FromQuery] string roomId, [FromQuery] int actorId, ILog log)
        {
            try
            {
                using var reader = new StreamReader(context.Request.Body);
                string details = await reader.ReadToEndAsync();
                string path = Path.Combine("desyncs", roomId, actorId + "-" + DateTimeOffset.Now.ToUnixTimeMilliseconds() + ".framedump");
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                await File.WriteAllTextAsync(path, details);
            }
            catch (Exception e)
            {
                log.Error(e);
                throw;
            }

            return Results.Ok();
        }
    }
}
