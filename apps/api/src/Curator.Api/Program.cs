using System.Text.Json.Serialization;
using Curator.Api.Authentication;
using Curator.Api.Development;
using Curator.Application;
using Curator.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// The OpenAPI document reads these options, not the controllers' above, so enums are described by name there too.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());
builder.Services.AddHealthChecks();
builder.Services.AddSupabaseAuthentication();
builder.Services.AddApplication();
builder.Services.AddInfrastructure();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference(options => options
            .AddPreferredSecuritySchemes(BearerSecuritySchemeTransformer.SchemeName)
            .EnablePersistentAuthentication())
        .AllowAnonymous();
    app.MapDevelopmentEndpoints();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseMiddleware<ProfileMiddleware>();
app.UseAuthorization();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers();

app.Run();
