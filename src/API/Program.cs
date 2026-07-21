// NOTE: Phase 10 (REST API for the future website) fleshes this out with
// controllers, JWT auth, Swagger and CORS (T-64..T-70). Minimal for now.
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => "KitchenwareBot API — see /swagger (Phase 10).");

app.Run();
