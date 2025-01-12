using Microsoft.AspNetCore.Localization;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddLocalization(opt => opt.ResourcesPath = "Resources");


builder.Services
        .AddApplication(builder.Configuration)
        .AddInfrastructure(builder.Configuration)
        .AddPersistence(builder.Configuration)
        .AddResources();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAnyOrigin", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyHeader()
               .AllowAnyMethod();
    });
});

builder.Services
        .ConfigureHttpClients(builder.Configuration)
        .ConfigureAuth(builder.Configuration)
        .AddSwaggerGen()
        ;

var app = builder.Build();
app.UseRequestLocalization(opt =>
{
    var supportedCultures = new List<CultureInfo>
    {
        new CultureInfo("kk"),
        new CultureInfo("en"),
        new CultureInfo("ru")

    };
    opt.DefaultRequestCulture = new RequestCulture("ru");
    opt.SupportedCultures = supportedCultures;
    opt.SupportedUICultures = supportedCultures;
});

app.UseSwagger();
app.UseSwaggerUI(opt =>
{
    opt.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
});

app.UseCustomExceptionHandler();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowAnyOrigin");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "api/*");



app.Run();


