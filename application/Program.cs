using Microsoft.EntityFrameworkCore;
using GreenMart.Data;
using GreenMart.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile(
    "appsettings.Maps.json",
    optional: true,
    reloadOnChange: true
);


builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));



builder.Services.AddAuthentication("GreenMartCookie")
    .AddCookie("GreenMartCookie", options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });



builder.Services.AddAuthorization();

builder.Services.AddScoped<IProductSearchService, ProductSearchService>();

builder.Services.AddScoped<ProductSearchService>();

builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("Email")
);

builder.Services.AddScoped<IEmailService, SmtpEmailService>();

builder.Services.AddScoped<IOrderReceiptPdfService, OrderReceiptPdfService>();

builder.Services.AddControllersWithViews();



var app = builder.Build();



if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}



app.UseHttpsRedirection();


app.UseStaticFiles();


app.UseRouting();



app.UseAuthentication();


app.UseAuthorization();



app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);



app.Run();
