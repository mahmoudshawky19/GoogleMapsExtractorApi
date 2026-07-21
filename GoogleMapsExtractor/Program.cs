using GoogleMapsExtractor.Services;
using OfficeOpenXml;
using Serilog;
using Tools;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
{
    config.ReadFrom.Configuration(context.Configuration)
          .Enrich.FromLogContext()
          .WriteTo.Console()
          .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day);
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Google Maps Extractor API",
        Version = "v1",
        Description = "This API extracts business data from Google Maps including names, phone numbers, websites, ratings, reviews, and addresses. It supports headless mode for background execution and can export results to Excel or CSV formats.",
        Contact = new()
        {
            Name = "Mahmoud Shawky",
            Email = "mahmoudshawky495@gmail.com"
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});

builder.Services.AddScoped<ExtractGoogleMapsTool>();
builder.Services.AddScoped<IExtractionService, ExtractionService>();
builder.Services.AddScoped<ExtractGoogleMapsTool>();
builder.Services.AddScoped<ExtractGoogleReviewsTool>();  // ← جديد
builder.Services.AddScoped<IExtractionService, ExtractionService>();
//OfficeOpenXml.ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
ExcelPackage.License.SetNonCommercialPersonal("Mahmoud Shawky");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();