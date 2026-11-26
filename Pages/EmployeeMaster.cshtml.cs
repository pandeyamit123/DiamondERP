using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;

namespace VinodKumarWeb.Pages;

public class EmployeeItem
{
    public int EmployeeID { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int DepartmentID { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string ContactNo { get; set; } = string.Empty;
}

public class EmployeeMasterModel : PageModel
{
    private readonly string _connectionString = "Server=localhost;Database=DBKOTHARIMFG;Trusted_Connection=True;TrustServerCertificate=True;";

    public List<EmployeeItem> Employees { get; set; } = new();
    public List<SelectListItem> DepartmentList { get; set; } = new();

    [BindProperty]
    public string EmployeeName { get; set; } = string.Empty;

    [BindProperty]
    public int DepartmentID { get; set; }

    [BindProperty]
    public string ContactNo { get; set; } = string.Empty;

    [BindProperty]
    public int EditEmployeeId { get; set; }

    public bool IsEditing { get; set; } = false;

    [TempData]
    public string Message { get; set; } = string.Empty;

    [TempData]
    public string ErrorMessage { get; set; } = string.Empty;

    public void OnGet(int? editId)
    {
        LoadDepartments();
        LoadEmployees();

        if (editId.HasValue)
        {
            var emp = Employees.FirstOrDefault(e => e.EmployeeID == editId.Value);
            if (emp != null)
            {
                IsEditing = true;
                EditEmployeeId = emp.EmployeeID;
                EmployeeName = emp.EmployeeName;
                DepartmentID = emp.DepartmentID;
                ContactNo = emp.ContactNo;
            }
        }
    }

    public IActionResult OnPostAdd()
    {
        if (string.IsNullOrWhiteSpace(EmployeeName) || DepartmentID == 0)
        {
            ErrorMessage = "Employee name and Department are required.";
            return RedirectToPage("/EmployeeMaster");
        }

        try
        {
            using (SqlConnection cnn = new SqlConnection(_connectionString))
            {
                cnn.Open();
                string query = "INSERT INTO TabEmployeeMaster (EmployeeName, DepartmentID, ContactNo) VALUES (@name, @deptId, @contact)";
                using (SqlCommand cmd = new SqlCommand(query, cnn))
                {
                    cmd.Parameters.AddWithValue("@name", EmployeeName.Trim().ToUpper());
                    cmd.Parameters.AddWithValue("@deptId", DepartmentID);
                    cmd.Parameters.AddWithValue("@contact", (object?)ContactNo?.Trim() ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
            Message = "Employee added successfully!";
        }
        catch (Exception ex)
        {
            ErrorMessage = "Error: " + ex.Message;
        }

        return RedirectToPage("/EmployeeMaster");
    }

    public IActionResult OnPostUpdate()
    {
        if (string.IsNullOrWhiteSpace(EmployeeName) || DepartmentID == 0)
        {
            ErrorMessage = "Employee name and Department are required.";
            return RedirectToPage("/EmployeeMaster", new { editId = EditEmployeeId });
        }

        try
        {
            using (SqlConnection cnn = new SqlConnection(_connectionString))
            {
                cnn.Open();
                string query = "UPDATE TabEmployeeMaster SET EmployeeName = @name, DepartmentID = @deptId, ContactNo = @contact WHERE EmployeeID = @id";
                using (SqlCommand cmd = new SqlCommand(query, cnn))
                {
                    cmd.Parameters.AddWithValue("@name", EmployeeName.Trim().ToUpper());
                    cmd.Parameters.AddWithValue("@deptId", DepartmentID);
                    cmd.Parameters.AddWithValue("@contact", (object?)ContactNo?.Trim() ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@id", EditEmployeeId);
                    cmd.ExecuteNonQuery();
                }
            }
            Message = "Employee updated successfully!";
        }
        catch (Exception ex)
        {
            ErrorMessage = "Error updating: " + ex.Message;
        }

        return RedirectToPage("/EmployeeMaster");
    }

    public IActionResult OnPostDelete(int id)
    {
        try
        {
            using (SqlConnection cnn = new SqlConnection(_connectionString))
            {
                cnn.Open();
                string query = "DELETE FROM TabEmployeeMaster WHERE EmployeeID = @id";
                using (SqlCommand cmd = new SqlCommand(query, cnn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
            Message = "Employee deleted successfully!";
        }
        catch (Exception ex)
        {
            ErrorMessage = "Error deleting: " + ex.Message;
        }

        return RedirectToPage("/EmployeeMaster");
    }

    private void LoadDepartments()
    {
        DepartmentList.Clear();
        using (SqlConnection cnn = new SqlConnection(_connectionString))
        {
            cnn.Open();
            string query = "SELECT DepartmentID, DepartmentName FROM TabDepartmentMaster ORDER BY DepartmentName";
            using (SqlCommand cmd = new SqlCommand(query, cnn))
            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    DepartmentList.Add(new SelectListItem
                    {
                        Value = dr["DepartmentID"].ToString(),
                        Text = dr["DepartmentName"].ToString()
                    });
                }
            }
        }
    }

    private void LoadEmployees()
    {
        Employees.Clear();
        using (SqlConnection cnn = new SqlConnection(_connectionString))
        {
            cnn.Open();
            string query = @"SELECT e.EmployeeID, e.EmployeeName, e.DepartmentID, d.DepartmentName, e.ContactNo 
                             FROM TabEmployeeMaster e 
                             INNER JOIN TabDepartmentMaster d ON e.DepartmentID = d.DepartmentID 
                             ORDER BY e.EmployeeID DESC";
            using (SqlCommand cmd = new SqlCommand(query, cnn))
            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    Employees.Add(new EmployeeItem
                    {
                        EmployeeID = Convert.ToInt32(dr["EmployeeID"]),
                        EmployeeName = dr["EmployeeName"].ToString() ?? string.Empty,
                        DepartmentID = Convert.ToInt32(dr["DepartmentID"]),
                        DepartmentName = dr["DepartmentName"].ToString() ?? string.Empty,
                        ContactNo = dr["ContactNo"] != DBNull.Value ? dr["ContactNo"].ToString() ?? "" : ""
                    });
                }
            }
        }
    }
}