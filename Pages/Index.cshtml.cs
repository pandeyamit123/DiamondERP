using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace VinodKumarWeb.Pages;

public class IndexModel : PageModel
{
    private readonly string _connectionString = "Server=localhost;Database=DBKOTHARIMFG;Trusted_Connection=True;TrustServerCertificate=True;";

    [BindProperty]
    public string Username { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    public string ErrorMessage { get; set; } = string.Empty;

    public void OnGet()
    {
        // Agar user pehle se logged in hai toh seedha Dashboard ya CompanySelection bhej sakte hain
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Please enter both Username and Password.";
            return Page();
        }

        try
        {
            await using var cnn = new SqlConnection(_connectionString);
            await cnn.OpenAsync();

            string query = "SELECT UserName FROM tabuserMaster WHERE UserName = @user AND UserPassword = @pass";
            await using var cmd = new SqlCommand(query, cnn);
            cmd.Parameters.AddWithValue("@user", Username.Trim());
            cmd.Parameters.AddWithValue("@pass", Password.Trim());

            var result = await cmd.ExecuteScalarAsync();

            if (result != null)
            {
                string validUser = result.ToString() ?? Username.Trim();

                // 1. Database mein LoggedIn status update karein
                await using (var updateCmd = new SqlCommand("UPDATE tabuserMaster SET UserStatus = 'LoggedIn' WHERE UserName = @user", cnn))
                {
                    updateCmd.Parameters.AddWithValue("@user", validUser);
                    await updateCmd.ExecuteNonQueryAsync();
                }

                // 2. User Claims (Identity) banayein
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, validUser)
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                // 3. Encrypted Auth Cookie issue karein
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                return RedirectToPage("/CompanySelection");
            }
            else
            {
                ErrorMessage = "Invalid Username or Password!";
                return Page();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "Database Error: " + ex.Message;
            return Page();
        }
    }

    // Secure Logout Handler
    public async Task<IActionResult> OnPostLogoutAsync()
    {
        string? loggedUser = User.Identity?.Name;

        if (!string.IsNullOrEmpty(loggedUser))
        {
            try
            {
                await using var cnn = new SqlConnection(_connectionString);
                await cnn.OpenAsync();
                await using var cmd = new SqlCommand("UPDATE tabuserMaster SET Status = 'LoggedOut' WHERE UserName = @user", cnn);
                cmd.Parameters.AddWithValue("@user", loggedUser);
                await cmd.ExecuteNonQueryAsync();
            }
            catch
            {
                // Ignore DB error on logout
            }
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("/Index");
    }
}