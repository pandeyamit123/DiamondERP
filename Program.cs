using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// 1. Cookie Authentication Configure Karein
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Index"; // Bina login ke yahan bhejega
        options.AccessDeniedPath = "/Index";
        options.ExpireTimeSpan = TimeSpan.FromHours(10); // 10 ghante tak login rahega
        options.SlidingExpiration = true;
    });

// 2. Saare Razor Pages Ko Default Lock Karein (Sirf Login page khula rahega)
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Index");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 3. Authentication & Authorization Middleware (Yeh order zaroori hai)
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();