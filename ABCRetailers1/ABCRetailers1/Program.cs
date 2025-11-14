// Program.cs
using ABCRetailers.Services;
using System.Globalization;

namespace ABCRetailers;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        

        // Add this line to your main project's Program.cs
        builder.Services.AddHttpClient<IFunctionsService, FunctionsService>();

        // In your main project's Program.cs
        builder.Services.AddScoped<IFunctionsService, FunctionsService>();

        // Add services to the container.
        builder.Services.AddControllersWithViews();

        // Register Azure Storage Service.
        builder.Services.AddScoped<IAzureStorageService,AzureStorageServices>();

        // Add logging.
        builder.Services.AddLogging();

        var app = builder.Build();

        // Set culture for decimal handling (FIXES PRICE ISSUE)
        var culture = new CultureInfo("en-US");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthorization();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        app.Run();
    }
}