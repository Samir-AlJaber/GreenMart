using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using GreenMart.Data;
using GreenMart.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile(
    "appsettings.Maps.json",
    optional: true,
    reloadOnChange: true
);

builder.Configuration.AddJsonFile(
    "appsettings.Payment.json",
    optional: true,
    reloadOnChange: true
);

builder.Configuration.AddJsonFile(
    "appsettings.Payout.json",
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

builder.Services.Configure<SslCommerzSettings>(
    builder.Configuration.GetSection("SslCommerz")
);

builder.Services.AddHttpClient<ISslCommerzService, SslCommerzService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.Configure<PayoutSettings>(
    builder.Configuration.GetSection("SellerPayouts")
);

builder.Services.AddScoped<ISellerPayoutService, SellerPayoutService>();
builder.Services.AddHostedService<SellerPayoutWorker>();

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

// Admin access is checked against the database on every request so removing an
// administrator invalidates an existing privileged cookie immediately.
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true && context.User.IsInRole("Admin"))
    {
        var userIdValue = context.User.FindFirst("UserId")?.Value;
        var remainsAdmin = int.TryParse(userIdValue, out var userId);

        if (remainsAdmin)
        {
            using var scope = context.RequestServices.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            remainsAdmin = await dbContext.Users
                .AsNoTracking()
                .AnyAsync(x => x.UserId == userId && x.Role == "Admin" && x.IsActive);
        }

        if (!remainsAdmin)
        {
            await context.SignOutAsync("GreenMartCookie");
            context.Response.Redirect("/Account/Login");
            return;
        }
    }

    await next();
});


app.UseAuthorization();



app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);



app.Run();
