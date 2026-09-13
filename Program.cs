using FinTrack.Components;
using FinTrack.Data;
using FinTrack.Repositories;
using FinTrack.Services.Interfaces;
using FinTrack.Services.Implementations;
using FinTrack.Services.Background;
using FinTrack.Models.Payments;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;

using System.Security.Claims;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);


// ============================================================
// BLAZOR
// ============================================================

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();

builder.Services.AddMemoryCache();


// ============================================================
// DATABASE
// ============================================================

builder.Services.AddScoped<
    IDbConnectionFactory,
    SqlConnectionFactory>();


// ============================================================
// REPOSITORIES
// ============================================================

builder.Services.AddScoped<
    IUserRepository,
    UserRepository>();

builder.Services.AddScoped<
    ICategoryRepository,
    CategoryRepository>();

builder.Services.AddScoped<
    ITransactionRepository,
    TransactionRepository>();

builder.Services.AddScoped<
    IBudgetRepository,
    BudgetRepository>();

builder.Services.AddScoped<
    IReportRepository,
    ReportRepository>();

builder.Services.AddScoped<
    IPaymentRepository,
    PaymentRepository>();

// Recurring Payments
builder.Services.AddScoped<
    IRecurringPaymentRepository,
    RecurringPaymentRepository>();

builder.Services.AddScoped<
    IBudgetRecommendationService,
    BudgetRecommendationService>();


// ============================================================
// SERVICES
// ============================================================

builder.Services.AddScoped<
    IPaymentService,
    PaymentService>();

builder.Services.AddScoped<
    IDashboardService,
    DashboardService>();

builder.Services.AddScoped<
    ITransactionService,
    TransactionService>();

builder.Services.AddScoped<
    IBudgetService,
    BudgetService>();

builder.Services.AddScoped<
    IReportService,
    ReportService>();

builder.Services.AddScoped<
    IUserService,
    UserService>();

builder.Services.AddScoped<
    ICategoryService,
    CategoryService>();

builder.Services.AddScoped<
    IAuthService,
    AuthService>();

// Recurring Payments
builder.Services.AddScoped<
    IRecurringPaymentService,
    RecurringPaymentService>();


// ============================================================
// BACKGROUND SERVICES
// ============================================================

builder.Services.AddHostedService<
    RecurringPaymentWorker>();


// ============================================================
// AUTHENTICATION
// ============================================================

builder.Services
    .AddAuthentication(
        CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath =
            "/account/login";

        options.AccessDeniedPath =
            "/access-denied";

        options.LogoutPath =
            "/api/auth/logout";
    });

builder.Services.AddAuthorization();


// ============================================================
// RATE LIMITING
// ============================================================

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode =
            StatusCodes.Status429TooManyRequests;

        context.HttpContext.Response.ContentType =
            "text/plain";

        await context.HttpContext.Response.WriteAsync(
            "Too many login attempts. Please wait one minute.",
            token);
    };

    options.GlobalLimiter =
        PartitionedRateLimiter.Create<HttpContext, string>(
            httpContext =>
            {
                if (
                    httpContext.Request.Path.Equals(
                        "/api/auth/login",
                        StringComparison.OrdinalIgnoreCase)
                    &&
                    HttpMethods.IsPost(
                        httpContext.Request.Method)
                )
                {
                    return RateLimitPartition
                        .GetFixedWindowLimiter(
                            httpContext.Connection
                                .RemoteIpAddress?
                                .ToString() ?? "unknown",

                            _ => new FixedWindowRateLimiterOptions
                            {
                                PermitLimit = 3,

                                Window =
                                    TimeSpan.FromMinutes(1),

                                QueueLimit = 0,

                                AutoReplenishment = true
                            });
                }

                return RateLimitPartition
                    .GetNoLimiter("NoLimit");
            });
});


// ============================================================
// BUILD APPLICATION
// ============================================================

var app = builder.Build();


// ============================================================
// ERROR HANDLING
// ============================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");

    app.UseHsts();
}


// ============================================================
// HTTP PIPELINE
// ============================================================

app.UseHttpsRedirection();

app.UseRouting();

app.UseRateLimiter();

app.UseAuthentication();

app.UseAuthorization();

app.UseAntiforgery();


// ============================================================
// ACTIVE USER VALIDATION
// ============================================================

app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var repository =
            context.RequestServices
                .GetRequiredService<IUserRepository>();

        var idClaim =
            context.User.FindFirst(
                ClaimTypes.NameIdentifier);

        if (
            idClaim != null &&
            int.TryParse(
                idClaim.Value,
                out var userId)
        )
        {
            var user =
                await repository.GetByIdAsync(userId);

            if (user == null || !user.IsActive)
            {
                await context.SignOutAsync(
                    CookieAuthenticationDefaults
                        .AuthenticationScheme);

                context.Response.Redirect(
                    "/account/login?message=deactivated");

                return;
            }
        }
    }

    await next();
});


// ============================================================
// LOGIN API
// ============================================================

app.MapPost(
    "/api/auth/login",
    async (
        HttpContext httpContext,
        LoginRequest request,
        IUserRepository userRepository) =>
    {
        var user =
            await userRepository.GetByEmailAsync(
                request.Email);

        if (
            user == null ||
            !BCrypt.Net.BCrypt.Verify(
                request.Password,
                user.PasswordHash)
        )
        {
            return Results.Unauthorized();
        }

        if (!user.IsActive)
        {
            return Results.BadRequest(
                new
                {
                    message =
                        "Your account has been deactivated."
                });
        }

        var claims = new List<Claim>
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                user.UserId.ToString()),

            new Claim(
                ClaimTypes.Name,
                $"{user.FirstName} {user.LastName}"),

            new Claim(
                ClaimTypes.Email,
                user.Email),

            new Claim(
                ClaimTypes.Role,
                user.RoleId == 1
                    ? "Admin"
                    : "User")
        };

        var identity =
            new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults
                    .AuthenticationScheme);

        var principal =
            new ClaimsPrincipal(identity);

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults
                .AuthenticationScheme,
            principal);

        var redirectUrl =
            user.RoleId == 1
                ? "/transactions"
                : "/dashboard";

        return Results.Ok(
            new
            {
                redirectUrl
            });
    });


// ============================================================
// LOGOUT API
// ============================================================

app.MapPost(
    "/api/auth/logout",
    async (
        HttpContext httpContext) =>
    {
        await httpContext.SignOutAsync(
            CookieAuthenticationDefaults
                .AuthenticationScheme);

        return Results.Ok();
    });


// ============================================================
// PAYMENT API - TESTING
// ============================================================

app.MapPost(
    "/api/payments",
    async (
        PaymentRequest request,
        IPaymentService paymentService) =>
    {
        var result =
            await paymentService.ProcessPaymentAsync(request);

        return result.Success
            ? Results.Ok(result)
            : Results.BadRequest(result);
    })
    .RequireAuthorization();


// ============================================================
// STATIC ASSETS
// ============================================================

app.MapStaticAssets();


// ============================================================
// BLAZOR COMPONENTS
// ============================================================

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();


// ============================================================
// RUN
// ============================================================

app.Run();


// ============================================================
// REQUEST MODELS
// ============================================================

public record LoginRequest(
    string Email,
    string Password);