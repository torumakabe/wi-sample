// <ms_docref_import_types>
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Identity.Web;
// </ms_docref_import_types>

// <ms_docref_add_msal>
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
var allowedRoles = builder.Configuration.GetSection("AzureAd:Roles").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddAuthorization(config =>
{
    config.AddPolicy("AuthZPolicy", policyBuilder =>
        policyBuilder.Requirements.Add(new RolesAuthorizationRequirement(allowedRoles)));
});
// </ms_docref_add_msal>

// <ms_docref_enable_authz_capabilities>
WebApplication app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
// </ms_docref_enable_authz_capabilities>

var weatherSummaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

// Public health endpoints (no auth)
app.MapGet("/healthz", (ILoggerFactory lf) =>
{
    var logger = lf.CreateLogger("sampleapi");
    logger.LogInformation("sampleapi healthz: status=ok");
    return Results.Ok(new { status = "ok" });
}).WithName("Healthz");
app.MapGet("/readyz", (ILoggerFactory lf) =>
{
    var logger = lf.CreateLogger("sampleapi");
    logger.LogInformation("sampleapi readyz: status=ready");
    return Results.Ok(new { status = "ready" });
}).WithName("Readyz");

// <ms_docref_protect_endpoint>
app.MapGet("/weatherforecast", [Authorize(Policy = "AuthZPolicy")] (HttpContext ctx, ILoggerFactory lf) =>
{
    var logger = lf.CreateLogger("sampleapi");
    logger.LogInformation("API request: GET /weatherforecast from {RemoteIp}", ctx.Connection.RemoteIpAddress);
    var forecast = Enumerable.Range(1, 5).Select(index =>
       new WeatherForecast
       (
           DateTime.Now.AddDays(index),
           Random.Shared.Next(-20, 55),
           weatherSummaries[Random.Shared.Next(weatherSummaries.Length)]
       ))
        .ToArray();
    logger.LogInformation("API response: /weatherforecast items={Count}", forecast.Length);
    return forecast;
})
.WithName("GetWeatherForecast");
// </ms_docref_protect_endpoint>

app.Run();

record WeatherForecast(DateTime Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
