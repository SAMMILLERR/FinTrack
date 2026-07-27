using FinTrack.Data;
using FinTrack.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();

builder.Services.AddMemoryCache();

builder.Services.AddScoped<IDbConnectionFactory, SqlConnectionFactory>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IBudgetRepository, BudgetRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.LogoutPath = "/Account/Logout";
    });

builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

  options.OnRejected = async (context, token) =>
{
    Console.WriteLine("========== RATE LIMIT HIT ==========");

    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
    context.HttpContext.Response.ContentType = "text/plain";

    await context.HttpContext.Response.WriteAsync(
        "Too many login attempts. Wait one minute.",
        token);
};

    options.GlobalLimiter =
    PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        Console.WriteLine(
            $"Limiter: {httpContext.Request.Method} {httpContext.Request.Path}");

        if (httpContext.Request.Path.Equals("/Account/Login", StringComparison.OrdinalIgnoreCase)
            && HttpMethods.IsPost(httpContext.Request.Method))
        {
            Console.WriteLine("LOGIN POST MATCHED");

            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 3,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                });
        }

        return RateLimitPartition.GetNoLimiter("NoLimit");
    });
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseRateLimiter();

app.UseAuthentication();

app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var repository =
            context.RequestServices.GetRequiredService<IUserRepository>();

        var idClaim =
            context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);

        if (idClaim != null &&
            int.TryParse(idClaim.Value, out int userId))
        {
            var user = await repository.GetByIdAsync(userId);

            if (user == null || !user.IsActive)
            {
                await context.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);

                context.Response.Redirect("/Account/Login?message=deactivated");
                return;
            }
        }
    }

    await next();
});

app.UseAuthorization();

app.MapStaticAssets();

app.MapRazorPages()
    .WithStaticAssets();

app.Run();