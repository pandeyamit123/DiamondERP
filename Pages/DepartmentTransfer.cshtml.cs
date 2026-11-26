using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;

namespace VinodKumarWeb.Pages;

public class TransferRecord
{
    public int DepTransID { get; set; }
    public int KPID { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string FromDept { get; set; } = string.Empty;
    public string ToDept { get; set; } = string.Empty;
    public int Pieces { get; set; }
    public decimal Carats { get; set; }
    public DateTime TransferDate { get; set; }
    public string TransferStatus { get; set; } = string.Empty;
    public DateTime? AcceptDate { get; set; }
}

public class DepartmentTransferModel : PageModel
{
    private readonly string _connectionString = "Server=localhost;Database=DBKOTHARIMFG;Trusted_Connection=True;TrustServerCertificate=True;";

    public List<SelectListItem> AvailablePackets { get; set; } = new();
    public List<SelectListItem> Departments { get; set; } = new();
    public List<TransferRecord> PendingTransfers { get; set; } = new();
    public List<TransferRecord> TransferHistory { get; set; } = new();

    [BindProperty]
    public int SelectedKPID { get; set; }

    [BindProperty]
    public int ToDepartmentID { get; set; }

    [TempData]
    public string Message { get; set; } = string.Empty;

    [TempData]
    public string ErrorMessage { get; set; } = string.Empty;

    public void OnGet()
    {
        LoadDropdowns();
        LoadTransfers();
    }

    public IActionResult OnPostSend()
    {
        if (SelectedKPID == 0 || ToDepartmentID == 0)
        {
            ErrorMessage = "Select both Packet and Target Department.";
            return RedirectToPage("/DepartmentTransfer");
        }

        try
        {
            using (SqlConnection cnn = new SqlConnection(_connectionString))
            {
                cnn.Open();
                int fromDeptId = 0, pcs = 0;
                decimal carats = 0;
                string displayName = "";

                using (SqlCommand cmd = new SqlCommand("SELECT DisplayName, CurrentDeptID, CurrentPcs, CurrentCarats FROM TabKapanPacketMaster WHERE KPID = @id AND CurrentStatus = 'Accept'", cnn))
                {
                    cmd.Parameters.AddWithValue("@id", SelectedKPID);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (!dr.Read())
                        {
                            ErrorMessage = "Packet is not available for transfer.";
                            return RedirectToPage("/DepartmentTransfer");
                        }
                        displayName = dr["DisplayName"].ToString() ?? "";
                        fromDeptId = Convert.ToInt32(dr["CurrentDeptID"]);
                        pcs = Convert.ToInt32(dr["CurrentPcs"]);
                        carats = Convert.ToDecimal(dr["CurrentCarats"]);
                    }
                }

                if (fromDeptId == ToDepartmentID)
                {
                    ErrorMessage = "Packet is already in the selected department!";
                    return RedirectToPage("/DepartmentTransfer");
                }

                string insertSql = @"INSERT INTO TabDepartmentTransfer 
                    (TransferDate, KPID, FromDepartmentID, ToDepartmentID, Pieces, Carats, TransferStatus) 
                    VALUES (GETDATE(), @kpid, @fromDept, @toDept, @pcs, @crt, 'Pending')";
                using (SqlCommand cmd = new SqlCommand(insertSql, cnn))
                {
                    cmd.Parameters.AddWithValue("@kpid", SelectedKPID);
                    cmd.Parameters.AddWithValue("@fromDept", fromDeptId);
                    cmd.Parameters.AddWithValue("@toDept", ToDepartmentID);
                    cmd.Parameters.AddWithValue("@pcs", pcs);
                    cmd.Parameters.AddWithValue("@crt", carats);
                    cmd.ExecuteNonQuery();
                }

                using (SqlCommand cmd = new SqlCommand("UPDATE TabKapanPacketMaster SET CurrentStatus = 'Pending' WHERE KPID = @kpid", cnn))
                {
                    cmd.Parameters.AddWithValue("@kpid", SelectedKPID);
                    cmd.ExecuteNonQuery();
                }

                Message = $"Packet '{displayName}' sent for transfer (Pending Receive).";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "Error: " + ex.Message;
        }

        return RedirectToPage("/DepartmentTransfer");
    }

    public IActionResult OnPostReceive(int transId, int kpid)
    {
        try
        {
            using (SqlConnection cnn = new SqlConnection(_connectionString))
            {
                cnn.Open();
                int toDeptId = 0;
                using (SqlCommand cmd = new SqlCommand("SELECT ToDepartmentID FROM TabDepartmentTransfer WHERE DepTransID = @id", cnn))
                {
                    cmd.Parameters.AddWithValue("@id", transId);
                    toDeptId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                using (SqlCommand cmd = new SqlCommand("UPDATE TabDepartmentTransfer SET TransferStatus = 'Accepted', AcceptDate = GETDATE() WHERE DepTransID = @id", cnn))
                {
                    cmd.Parameters.AddWithValue("@id", transId);
                    cmd.ExecuteNonQuery();
                }

                using (SqlCommand cmd = new SqlCommand("UPDATE TabKapanPacketMaster SET CurrentDeptID = @toDept, CurrentStatus = 'Accept' WHERE KPID = @kpid", cnn))
                {
                    cmd.Parameters.AddWithValue("@toDept", toDeptId);
                    cmd.Parameters.AddWithValue("@kpid", kpid);
                    cmd.ExecuteNonQuery();
                }

                Message = "Packet received into new department successfully!";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "Error receiving: " + ex.Message;
        }

        return RedirectToPage("/DepartmentTransfer");
    }

    private void LoadDropdowns()
    {
        using (SqlConnection cnn = new SqlConnection(_connectionString))
        {
            cnn.Open();
            string pktSql = @"SELECT p.KPID, p.DisplayName, d.DepartmentName, p.CurrentPcs, p.CurrentCarats 
                              FROM TabKapanPacketMaster p
                              INNER JOIN TabDepartmentMaster d ON p.CurrentDeptID = d.DepartmentID
                              WHERE p.CurrentStatus = 'Accept' ORDER BY p.KPID DESC";
            using (SqlCommand cmd = new SqlCommand(pktSql, cnn))
            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    AvailablePackets.Add(new SelectListItem
                    {
                        Value = dr["KPID"].ToString(),
                        Text = $"{dr["DisplayName"]} (Current: {dr["DepartmentName"]}) - {Convert.ToDecimal(dr["CurrentCarats"]):0.000} Crt"
                    });
                }
            }

            using (SqlCommand cmd = new SqlCommand("SELECT DepartmentID, DepartmentName FROM TabDepartmentMaster ORDER BY DepartmentName", cnn))
            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    Departments.Add(new SelectListItem
                    {
                        Value = dr["DepartmentID"].ToString(),
                        Text = dr["DepartmentName"].ToString()
                    });
                }
            }
        }
    }

    private void LoadTransfers()
    {
        PendingTransfers.Clear();
        TransferHistory.Clear();
        using (SqlConnection cnn = new SqlConnection(_connectionString))
        {
            cnn.Open();
            string sql = @"SELECT t.DepTransID, t.KPID, p.DisplayName, d1.DepartmentName AS FromDept, d2.DepartmentName AS ToDept,
                           t.Pieces, t.Carats, t.TransferDate, t.TransferStatus, t.AcceptDate
                           FROM TabDepartmentTransfer t
                           INNER JOIN TabKapanPacketMaster p ON t.KPID = p.KPID
                           INNER JOIN TabDepartmentMaster d1 ON t.FromDepartmentID = d1.DepartmentID
                           INNER JOIN TabDepartmentMaster d2 ON t.ToDepartmentID = d2.DepartmentID
                           ORDER BY t.DepTransID DESC";
            using (SqlCommand cmd = new SqlCommand(sql, cnn))
            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    var item = new TransferRecord
                    {
                        DepTransID = Convert.ToInt32(dr["DepTransID"]),
                        KPID = Convert.ToInt32(dr["KPID"]),
                        DisplayName = dr["DisplayName"].ToString() ?? "",
                        FromDept = dr["FromDept"].ToString() ?? "",
                        ToDept = dr["ToDept"].ToString() ?? "",
                        Pieces = Convert.ToInt32(dr["Pieces"]),
                        Carats = Convert.ToDecimal(dr["Carats"]),
                        TransferDate = Convert.ToDateTime(dr["TransferDate"]),
                        TransferStatus = dr["TransferStatus"].ToString() ?? "",
                        AcceptDate = dr["AcceptDate"] != DBNull.Value ? Convert.ToDateTime(dr["AcceptDate"]) : null
                    };

                    if (item.TransferStatus == "Pending")
                        PendingTransfers.Add(item);
                    else
                        TransferHistory.Add(item);
                }
            }
        }
    }
}