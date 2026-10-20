using Curator.Api.Authentication;
using Curator.Api.Common;
using Curator.Api.Development;
using Curator.Api.OpenApi;
using Curator.Application;
using Curator.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCuratorControllers();

// Every error is ProblemDetails (RFC 9457), also those ASP.NET Core answers itself, so clients handle errors one way.
builder.Services.AddProblemDetails();
builder.Services.AddCuratorOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddSupabaseAuthentication();
builder.Services.AddApplication();
builder.Services.AddInfrastructure();

var app = builder.Build();

// In Development the developer exception page shows the details (as ProblemDetails for API clients) instead.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
}

// Fills empty error responses, such as 404 for an unmatched route or 401 without a token.
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapCuratorApiReference();
    app.MapDevelopmentEndpoints();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseMiddleware<ProfileMiddleware>();
app.UseAuthorization();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers();

app.Run();
