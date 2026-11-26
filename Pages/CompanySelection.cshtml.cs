using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;

namespace VinodKumarWeb.Pages;

public class CompanySelectionModel : PageModel
{
    private readonly string _connectionString = "Server=localhost;Database=DBKOTHARIMFG;Trusted_Connection=True;TrustServerCertificate=True;";

    [BindProperty]
    public int SelectedCompanyId { get; set; }

    [BindProperty]
    public int SelectedYearId { get; set; }

    public List<SelectListItem> Companies { get; set; } = new();
    public List<SelectListItem> FinancialYears { get; set; } = new();
    public string ErrorMessage { get; set; } = string.Empty;

    public void OnGet()
    {
        LoadDropdowns();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        LoadDropdowns();

        if (SelectedCompanyId == 0 || SelectedYearId == 0)
        {
            ErrorMessage = "Please select both Company and Financial Year.";
            return Page();
        }

        string companyName = Companies.FirstOrDefault(c => c.Value == SelectedCompanyId.ToString())?.Text ?? "";
        string finYear = FinancialYears.FirstOrDefault(y => y.Value == SelectedYearId.ToString())?.Text ?? "";
        string currentUser = User.Identity?.Name ?? "Admin";

        // Claims mein Company aur Financial Year add karke Cookie refresh karein
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, currentUser),
            new Claim("CompanyId", SelectedCompanyId.ToString()),
            new Claim("CompanyName", companyName),
            new Claim("YearId", SelectedYearId.ToString()),
            new Claim("FinancialYear", finYear)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        return RedirectToPage("/Dashboard");
    }

    private void LoadDropdowns()
    {
        Companies.Clear();
        FinancialYears.Clear();

        using SqlConnection cnn = new SqlConnection(_connectionString);
        cnn.Open();

        using (SqlCommand cmd = new SqlCommand("SELECT CompanyID, CompanyName FROM TabCompanyMaster", cnn))
        using (SqlDataReader dr = cmd.ExecuteReader())
        {
            while (dr.Read())
            {
                Companies.Add(new SelectListItem
                {
                    Value = dr["CompanyID"].ToString(),
                    Text = dr["CompanyName"].ToString()
                });
            }
        }

        using (SqlCommand cmd = new SqlCommand("SELECT FinancialYearID, FinancialYear FROM TabFinancialYearMaster", cnn))
        using (SqlDataReader dr = cmd.ExecuteReader())
        {
            while (dr.Read())
            {
                FinancialYears.Add(new SelectListItem
                {
                    Value = dr["FinancialYearID"].ToString(),
                    Text = dr["FinancialYear"].ToString()
                });
            }
        }
    }
}