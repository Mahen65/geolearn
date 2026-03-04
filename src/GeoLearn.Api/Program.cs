using GeoLearn.Api.Data;
using GeoLearn.Api.Services;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Allow large shapefile uploads (e.g. OSM country extracts can be 100–500 MB).
// Kestrel default is 30 MB; this raises the ceiling to 600 MB.
builder.WebHost.ConfigureKestrel(options =>
    options.Limits.MaxRequestBodySize = 600 * 1024 * 1024);

// --- Database ---
// UseNetTopologySuite() enables PostGIS geometry ↔ NTS Geometry object mapping.
// Without it, geometry columns are returned as raw bytes.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Default"),
        npgsql => npgsql.UseNetTopologySuite()
    )
);

// --- Services ---
builder.Services.AddScoped<ShapefileImportService>();

// NsdiImportService uses HttpClient to page through the NSDI ArcGIS REST API.
// Registered with a named HttpClient so timeout can be set independently.
builder.Services.AddHttpClient<NsdiImportService>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(5);
});

// --- CORS ---
builder.Services.AddCors(options =>
    options.AddPolicy("DevFrontend", p =>
        p.WithOrigins("http://localhost:5173")
         .AllowAnyHeader().AllowAnyMethod()));

// --- API ---
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.WriteIndented = true);

builder.Services.AddOpenApi();

var app = builder.Build();

// Auto-apply migrations on startup (convenient for local learning; use explicit migrations in production)
using (var scope = app.Services.CreateScope())
{
    var dbCtx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbCtx.Database.Migrate();
}

// OpenAPI spec + Scalar UI — available in all environments (local and Docker)
app.MapOpenApi();
app.MapScalarApiReference();

app.UseHttpsRedirection();
app.UseCors("DevFrontend");
app.MapControllers();

app.Run();
