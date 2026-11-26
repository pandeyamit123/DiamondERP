using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace VinodKumarWeb.Pages;

public class DepartmentItem
{
    public int DepartmentID { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
}

public class DepartmentMasterModel : PageModel
{
    private readonly string _connectionString = "Server=localhost;Database=DBKOTHARIMFG;Trusted_Connection=True;TrustServerCertificate=True;";

    public List<DepartmentItem> Departments { get; set; } = new();

    [BindProperty]
    public string NewDepartmentName { get; set; } = string.Empty;

    [BindProperty]
    public int EditDepartmentId { get; set; }

    [BindProperty]
    public string EditDepartmentName { get; set; } = string.Empty;

    public bool IsEditing { get; set; } = false;

    [TempData]
    public string Message { get; set; } = string.Empty;

    [TempData]
    public string ErrorMessage { get; set; } = string.Empty;

    public void OnGet(int? editId)
    {
        LoadDepartments();

        if (editId.HasValue)
        {
            var dept = Departments.FirstOrDefault(d => d.DepartmentID == editId.Value);
            if (dept != null)
            {
                IsEditing = true;
                EditDepartmentId = dept.DepartmentID;
                EditDepartmentName = dept.DepartmentName;
            }
        }
    }

    public IActionResult OnPostAdd()
    {
        if (string.IsNullOrWhiteSpace(NewDepartmentName))
        {
            ErrorMessage = "Department name cannot be empty.";
            return RedirectToPage("/DepartmentMaster");
        }

        try
        {
            using (SqlConnection cnn = new SqlConnection(_connectionString))
            {
                cnn.Open();
                string query = "INSERT INTO TabDepartmentMaster (DepartmentName) VALUES (@name)";
                using (SqlCommand cmd = new SqlCommand(query, cnn))
                {
                    cmd.Parameters.AddWithValue("@name", NewDepartmentName.Trim().ToUpper());
                    cmd.ExecuteNonQuery();
                }
            }
            Message = "Department added successfully!";
        }
        catch (Exception ex)
        {
            ErrorMessage = "Error: " + ex.Message;
        }

        return RedirectToPage("/DepartmentMaster");
    }

    public IActionResult OnPostUpdate()
    {
        if (string.IsNullOrWhiteSpace(EditDepartmentName))
        {
            ErrorMessage = "Department name cannot be empty.";
            return RedirectToPage("/DepartmentMaster", new { editId = EditDepartmentId });
        }

        try
        {
            using (SqlConnection cnn = new SqlConnection(_connectionString))
            {
                cnn.Open();
                string query = "UPDATE TabDepartmentMaster SET DepartmentName = @name WHERE DepartmentID = @id";
                using (SqlCommand cmd = new SqlCommand(query, cnn))
                {
                    cmd.Parameters.AddWithValue("@name", EditDepartmentName.Trim().ToUpper());
                    cmd.Parameters.AddWithValue("@id", EditDepartmentId);
                    cmd.ExecuteNonQuery();
                }
            }
            Message = "Department updated successfully!";
        }
        catch (Exception ex)
        {
            ErrorMessage = "Error updating: " + ex.Message;
        }

        return RedirectToPage("/DepartmentMaster");
    }

    public IActionResult OnPostDelete(int id)
    {
        try
        {
            using (SqlConnection cnn = new SqlConnection(_connectionString))
            {
                cnn.Open();
                string query = "DELETE FROM TabDepartmentMaster WHERE DepartmentID = @id";
                using (SqlCommand cmd = new SqlCommand(query, cnn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
            Message = "Department deleted successfully!";
        }
        catch (Exception ex)
        {
            ErrorMessage = "Error deleting: " + ex.Message;
        }

        return RedirectToPage("/DepartmentMaster");
    }

    private void LoadDepartments()
    {
        Departments.Clear();
        using (SqlConnection cnn = new SqlConnection(_connectionString))
        {
            cnn.Open();
            string query = "SELECT DepartmentID, DepartmentName FROM TabDepartmentMaster ORDER BY DepartmentID DESC";
            using (SqlCommand cmd = new SqlCommand(query, cnn))
            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    Departments.Add(new DepartmentItem
                    {
                        DepartmentID = Convert.ToInt32(dr["DepartmentID"]),
                        DepartmentName = dr["DepartmentName"].ToString() ?? string.Empty
                    });
                }
            }
        }
    }
}