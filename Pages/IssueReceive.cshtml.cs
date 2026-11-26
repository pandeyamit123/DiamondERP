using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;

namespace VinodKumarWeb.Pages;

public class IssueReceiveRecord
{
    public int IssueID { get; set; }
    public int KPID { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public int IssuePcs { get; set; }
    public decimal IssueCarats { get; set; }
    public DateTime? ReceiveDate { get; set; }
    public int? ReceivePcs { get; set; }
    public decimal? ReceiveCarats { get; set; }
    public decimal? LossCarats { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class IssueReceiveModel : PageModel
{
    private readonly string _connectionString = "Server=localhost;Database=DBKOTHARIMFG;Trusted_Connection=True;TrustServerCertificate=True;";

    public List<SelectListItem> AvailablePackets { get; set; } = new();
    public List<SelectListItem> Employees { get; set; } = new();
    public List<IssueReceiveRecord> ActiveIssues { get; set; } = new();
    public List<IssueReceiveRecord> CompletedHistory { get; set; } = new();

    [BindProperty]
    public int SelectedKPID { get; set; }

    [BindProperty]
    public int SelectedEmployeeID { get; set; }

    [TempData]
    public string Message { get; set; } = string.Empty;

    [TempData]
    public string ErrorMessage { get; set; } = string.Empty;

    public void OnGet()
    {
        LoadDropdowns();
        LoadLists();
    }

    public IActionResult OnPostIssue()
    {
        if (SelectedKPID == 0 || SelectedEmployeeID == 0)
        {
            ErrorMessage = "Please select both Packet and Employee.";
            return RedirectToPage("/IssueReceive");
        }

        try
        {
            using (SqlConnection cnn = new SqlConnection(_connectionString))
            {
                cnn.Open();

                int deptId = 0, pcs = 0;
                decimal carats = 0;
                string displayName = "";

                using (SqlCommand cmd = new SqlCommand("SELECT DisplayName, CurrentDeptID, CurrentPcs, CurrentCarats FROM TabKapanPacketMaster WHERE KPID = @kpid AND CurrentStatus = 'Accept'", cnn))
                {
                    cmd.Parameters.AddWithValue("@kpid", SelectedKPID);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (!dr.Read())
                        {
                            ErrorMessage = "Packet is not available for issue.";
                            return RedirectToPage("/IssueReceive");
                        }
                        displayName = dr["DisplayName"].ToString() ?? "";
                        deptId = Convert.ToInt32(dr["CurrentDeptID"]);
                        pcs = Convert.ToInt32(dr["CurrentPcs"]);
                        carats = Convert.ToDecimal(dr["CurrentCarats"]);
                    }
                }

                // 1. Insert Issue Entry
                string insertSql = @"INSERT INTO TabPacketIssueReceive 
                    (KPID, DepartmentID, EmployeeID, IssueDate, IssuePcs, IssueCarats, Status) 
                    VALUES (@kpid, @deptId, @empId, GETDATE(), @pcs, @crt, 'Issued')";
                using (SqlCommand cmd = new SqlCommand(insertSql, cnn))
                {
                    cmd.Parameters.AddWithValue("@kpid", SelectedKPID);
                    cmd.Parameters.AddWithValue("@deptId", deptId);
                    cmd.Parameters.AddWithValue("@empId", SelectedEmployeeID);
                    cmd.Parameters.AddWithValue("@pcs", pcs);
                    cmd.Parameters.AddWithValue("@crt", carats);
                    cmd.ExecuteNonQuery();
                }

                // 2. Update Packet Status to Issued
                using (SqlCommand cmd = new SqlCommand("UPDATE TabKapanPacketMaster SET CurrentStatus = 'Issued' WHERE KPID = @kpid", cnn))
                {
                    cmd.Parameters.AddWithValue("@kpid", SelectedKPID);
                    cmd.ExecuteNonQuery();
                }

                Message = $"Packet '{displayName}' issued successfully!";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "Error issuing packet: " + ex.Message;
        }

        return RedirectToPage("/IssueReceive");
    }

    public IActionResult OnPostReceive(int issueId, int kpid, int returnPcs, decimal returnCarats)
    {
        if (returnCarats <= 0)
        {
            ErrorMessage = "Return carats must be greater than 0.";
            return RedirectToPage("/IssueReceive");
        }

        try
        {
            using (SqlConnection cnn = new SqlConnection(_connectionString))
            {
                cnn.Open();

                decimal issueCarats = 0;
                using (SqlCommand cmd = new SqlCommand("SELECT IssueCarats FROM TabPacketIssueReceive WHERE IssueID = @id", cnn))
                {
                    cmd.Parameters.AddWithValue("@id", issueId);
                    issueCarats = Convert.ToDecimal(cmd.ExecuteScalar());
                }

                if (returnCarats > issueCarats)
                {
                    ErrorMessage = $"Return Carats ({returnCarats:0.000}) cannot be greater than Issued Carats ({issueCarats:0.000})!";
                    return RedirectToPage("/IssueReceive");
                }

                decimal lossCarats = issueCarats - returnCarats;

                // 1. Update IssueReceive Record
                string updateIssue = @"UPDATE TabPacketIssueReceive 
                    SET ReceiveDate = GETDATE(), ReceivePcs = @pcs, ReceiveCarats = @rcrt, LossCarats = @loss, Status = 'Received' 
                    WHERE IssueID = @id";
                using (SqlCommand cmd = new SqlCommand(updateIssue, cnn))
                {
                    cmd.Parameters.AddWithValue("@pcs", returnPcs);
                    cmd.Parameters.AddWithValue("@rcrt", returnCarats);
                    cmd.Parameters.AddWithValue("@loss", lossCarats);
                    cmd.Parameters.AddWithValue("@id", issueId);
                    cmd.ExecuteNonQuery();
                }

                // 2. Update Packet Master with new Carats and set Status back to 'Accept'
                string updatePkt = @"UPDATE TabKapanPacketMaster 
                    SET CurrentPcs = @pcs, CurrentCarats = @rcrt, CurrentStatus = 'Accept' 
                    WHERE KPID = @kpid";
                using (SqlCommand cmd = new SqlCommand(updatePkt, cnn))
                {
                    cmd.Parameters.AddWithValue("@pcs", returnPcs);
                    cmd.Parameters.AddWithValue("@rcrt", returnCarats);
                    cmd.Parameters.AddWithValue("@kpid", kpid);
                    cmd.ExecuteNonQuery();
                }

                Message = $"Packet received! Ready: {returnCarats:0.000} Crt, Loss: {lossCarats:0.000} Crt.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "Error receiving packet: " + ex.Message;
        }

        return RedirectToPage("/IssueReceive");
    }

    private void LoadDropdowns()
    {
        using (SqlConnection cnn = new SqlConnection(_connectionString))
        {
            cnn.Open();

            // Load Available Packets (Status = 'Accept')
            string pktQuery = @"SELECT p.KPID, p.DisplayName, d.DepartmentName, p.CurrentPcs, p.CurrentCarats 
                                FROM TabKapanPacketMaster p
                                INNER JOIN TabDepartmentMaster d ON p.CurrentDeptID = d.DepartmentID
                                WHERE p.CurrentStatus = 'Accept'
                                ORDER BY p.KPID DESC";
            using (SqlCommand cmd = new SqlCommand(pktQuery, cnn))
            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    AvailablePackets.Add(new SelectListItem
                    {
                        Value = dr["KPID"].ToString(),
                        Text = $"{dr["DisplayName"]} [{dr["DepartmentName"]}] - {dr["CurrentPcs"]} Pcs / {Convert.ToDecimal(dr["CurrentCarats"]):0.000} Crt"
                    });
                }
            }

            // Load Employees
            string empQuery = @"SELECT e.EmployeeID, e.EmployeeName, d.DepartmentName 
                                FROM TabEmployeeMaster e
                                INNER JOIN TabDepartmentMaster d ON e.DepartmentID = d.DepartmentID
                                ORDER BY e.EmployeeName";
            using (SqlCommand cmd = new SqlCommand(empQuery, cnn))
            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    Employees.Add(new SelectListItem
                    {
                        Value = dr["EmployeeID"].ToString(),
                        Text = $"{dr["EmployeeName"]} ({dr["DepartmentName"]})"
                    });
                }
            }
        }
    }

    private void LoadLists()
    {
        ActiveIssues.Clear();
        CompletedHistory.Clear();

        using (SqlConnection cnn = new SqlConnection(_connectionString))
        {
            cnn.Open();
            string query = @"SELECT ir.IssueID, ir.KPID, p.DisplayName, d.DepartmentName, e.EmployeeName,
                             ir.IssueDate, ir.IssuePcs, ir.IssueCarats, ir.ReceiveDate, ir.ReceivePcs, ir.ReceiveCarats, ir.LossCarats, ir.Status
                             FROM TabPacketIssueReceive ir
                             INNER JOIN TabKapanPacketMaster p ON ir.KPID = p.KPID
                             INNER JOIN TabDepartmentMaster d ON ir.DepartmentID = d.DepartmentID
                             INNER JOIN TabEmployeeMaster e ON ir.EmployeeID = e.EmployeeID
                             ORDER BY ir.IssueID DESC";

            using (SqlCommand cmd = new SqlCommand(query, cnn))
            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    var rec = new IssueReceiveRecord
                    {
                        IssueID = Convert.ToInt32(dr["IssueID"]),
                        KPID = Convert.ToInt32(dr["KPID"]),
                        DisplayName = dr["DisplayName"].ToString() ?? "",
                        DepartmentName = dr["DepartmentName"].ToString() ?? "",
                        EmployeeName = dr["EmployeeName"].ToString() ?? "",
                        IssueDate = Convert.ToDateTime(dr["IssueDate"]),
                        IssuePcs = Convert.ToInt32(dr["IssuePcs"]),
                        IssueCarats = Convert.ToDecimal(dr["IssueCarats"]),
                        ReceiveDate = dr["ReceiveDate"] != DBNull.Value ? Convert.ToDateTime(dr["ReceiveDate"]) : null,
                        ReceivePcs = dr["ReceivePcs"] != DBNull.Value ? Convert.ToInt32(dr["ReceivePcs"]) : null,
                        ReceiveCarats = dr["ReceiveCarats"] != DBNull.Value ? Convert.ToDecimal(dr["ReceiveCarats"]) : null,
                        LossCarats = dr["LossCarats"] != DBNull.Value ? Convert.ToDecimal(dr["LossCarats"]) : null,
                        Status = dr["Status"].ToString() ?? ""
                    };

                    if (rec.Status == "Issued")
                        ActiveIssues.Add(rec);
                    else
                        CompletedHistory.Add(rec);
                }
            }
        }
    }
}