using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace VinodKumarWeb.Pages;

public class MasterRecord
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class RoughMastersModel : PageModel
{
    private readonly string _connectionString = "Server=localhost;Database=DBKOTHARIMFG;Trusted_Connection=True;TrustServerCertificate=True;";

    [BindProperty(SupportsGet = true)]
    public string Category { get; set; } = "Type"; // Type, Size, Color, Purity

    public List<MasterRecord> Records { get; set; } = new();

    [BindProperty]
    public string ItemName { get; set; } = string.Empty;

    [BindProperty]
    public int EditId { get; set; }

    public bool IsEditing { get; set; } = false;

    [TempData]
    public string Message { get; set; } = string.Empty;

    [TempData]
    public string ErrorMessage { get; set; } = string.Empty;

    private (string Table, string IdCol, string NameCol, string Title) GetTableInfo()
    {
        return Category switch
        {
            "Size" => ("TabRoughSizeMaster", "RoughSizeID", "RoughSizeName", "Rough Size Master"),
            "Color" => ("TabRoughColorMaster", "RoughColorID", "RoughColorName", "Rough Color Master"),
            "Purity" => ("TabRoughPurityMaster", "RoughPurityID", "RoughPurityName", "Rough Purity Master"),
            _ => ("TabRoughTypeMaster", "RoughTypeID", "RoughTypeName", "Rough Type Master")
        };
    }

    public string CurrentTitle => GetTableInfo().Title;

    public void OnGet(int? editId)
    {
        LoadRecords();
        if (editId.HasValue)
        {
            var item = Records.FirstOrDefault(r => r.Id == editId.Value);
            if (item != null)
            {
                IsEditing = true;
                EditId = item.Id;
                ItemName = item.Name;
            }
        }
    }

    public IActionResult OnPostAdd()
    {
        if (string.IsNullOrWhiteSpace(ItemName))
        {
            ErrorMessage = "Name cannot be empty.";
            return RedirectToPage("/RoughMasters", new { Category });
        }

        var info = GetTableInfo();
        try
        {
            using (SqlConnection cnn = new SqlConnection(_connectionString))
            {
                cnn.Open();
                string query = $"INSERT INTO {info.Table} ({info.NameCol}) VALUES (@name)";
                using (SqlCommand cmd = new SqlCommand(query, cnn))
                {
                    cmd.Parameters.AddWithValue("@name", ItemName.Trim().ToUpper());
                    cmd.ExecuteNonQuery();
                }
            }
            Message = $"{info.Title} added successfully!";
        }
        catch (Exception ex)
        {
            ErrorMessage = "Error: " + ex.Message;
        }

        return RedirectToPage("/RoughMasters", new { Category });
    }

    public IActionResult OnPostUpdate()
    {
        if (string.IsNullOrWhiteSpace(ItemName))
        {
            ErrorMessage = "Name cannot be empty.";
            return RedirectToPage("/RoughMasters", new { Category, editId = EditId });
        }

        var info = GetTableInfo();
        try
        {
            using (SqlConnection cnn = new SqlConnection(_connectionString))
            {
                cnn.Open();
                string query = $"UPDATE {info.Table} SET {info.NameCol} = @name WHERE {info.IdCol} = @id";
                using (SqlCommand cmd = new SqlCommand(query, cnn))
                {
                    cmd.Parameters.AddWithValue("@name", ItemName.Trim().ToUpper());
                    cmd.Parameters.AddWithValue("@id", EditId);
                    cmd.ExecuteNonQuery();
                }
            }
            Message = $"{info.Title} updated successfully!";
        }
        catch (Exception ex)
        {
            ErrorMessage = "Error updating: " + ex.Message;
        }

        return RedirectToPage("/RoughMasters", new { Category });
    }

    public IActionResult OnPostDelete(int id)
    {
        var info = GetTableInfo();
        try
        {
            using (SqlConnection cnn = new SqlConnection(_connectionString))
            {
                cnn.Open();
                string query = $"DELETE FROM {info.Table} WHERE {info.IdCol} = @id";
                using (SqlCommand cmd = new SqlCommand(query, cnn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
            Message = "Deleted successfully!";
        }
        catch (Exception ex)
        {
            ErrorMessage = "Error deleting: " + ex.Message;
        }

        return RedirectToPage("/RoughMasters", new { Category });
    }

    private void LoadRecords()
    {
        Records.Clear();
        var info = GetTableInfo();
        using (SqlConnection cnn = new SqlConnection(_connectionString))
        {
            cnn.Open();
            string query = $"SELECT {info.IdCol} AS Id, {info.NameCol} AS Name FROM {info.Table} ORDER BY {info.IdCol} DESC";
            using (SqlCommand cmd = new SqlCommand(query, cnn))
            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    Records.Add(new MasterRecord
                    {
                        Id = Convert.ToInt32(dr["Id"]),
                        Name = dr["Name"].ToString() ?? string.Empty
                    });
                }
            }
        }
    }
}