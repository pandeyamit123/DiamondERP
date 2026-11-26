using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;

namespace VinodKumarWeb.Pages;

public class KapanPacketItem
{
    public int KPID { get; set; }
    public DateTime PacketDate { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public int CurrentPcs { get; set; }
    public decimal CurrentCarats { get; set; }
    public string CurrentStatus { get; set; } = string.Empty;
}

public class KapanPacketMasterModel : PageModel
{
    private readonly string _connectionString = "Server=localhost;Database=DBKOTHARIMFG;Trusted_Connection=True;TrustServerCertificate=True;";

    public List<KapanPacketItem> PacketList { get; set; } = new();
    public List<SelectListItem> InwardKapans { get; set; } = new();
    public List<SelectListItem> Departments { get; set; } = new();

    [BindProperty]
    public DateTime PacketDate { get; set; } = DateTime.Today;

    [BindProperty]
    public int SelectedInwardID { get; set; }

    [BindProperty]
    public string PacketNo { get; set; } = string.Empty;

    [BindProperty]
    public int SelectedDeptID { get; set; }

    [BindProperty]
    public int Pieces { get; set; }

    [BindProperty]
    public decimal Carats { get; set; }

    [TempData]
    public string Message { get; set; } = string.Empty;

    [TempData]
    public string ErrorMessage { get; set; } = string.Empty;

    public void OnGet()
    {
        LoadDropdowns();
        LoadPackets();
    }

    public IActionResult OnPostAdd()
    {
        if (SelectedInwardID == 0 || string.IsNullOrWhiteSpace(PacketNo) || Carats <= 0)
        {
            ErrorMessage = "Kapan selection, Packet No, and valid Carats are required.";
            return RedirectToPage("/KapanPacketMaster");
        }

        try
        {
            using (SqlConnection cnn = new SqlConnection(_connectionString))
            {
                cnn.Open();

                // 1. Get KapanNo & Total Inward Carats
                string kapanNo = "";
                decimal totalInwardCrt = 0;
                using (SqlCommand cmd = new SqlCommand("SELECT KapanNo, Carats FROM TabStockInward WHERE InwardID = @id", cnn))
                {
                    cmd.Parameters.AddWithValue("@id", SelectedInwardID);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            kapanNo = dr["KapanNo"].ToString() ?? "";
                            totalInwardCrt = Convert.ToDecimal(dr["Carats"]);
                        }
                    }
                }

                // 2. Check already allocated carats for this InwardID
                decimal usedCrt = 0;
                using (SqlCommand cmd = new SqlCommand("SELECT ISNULL(SUM(InitialCarats), 0) FROM TabKapanPacketMaster WHERE InwardID = @id", cnn))
                {
                    cmd.Parameters.AddWithValue("@id", SelectedInwardID);
                    usedCrt = Convert.ToDecimal(cmd.ExecuteScalar());
                }

                if (usedCrt + Carats > totalInwardCrt)
                {
                    decimal remaining = totalInwardCrt - usedCrt;
                    ErrorMessage = $"Insufficient balance in {kapanNo}! Available carats: {remaining:0.000}";
                    return RedirectToPage("/KapanPacketMaster");
                }

                string cleanPkt = PacketNo.Trim().ToUpper();
                string displayName = $"{kapanNo}/{cleanPkt}";

                // 3. Insert Packet
                string insertQuery = @"INSERT INTO TabKapanPacketMaster 
                    (PacketDate, InwardID, KapanNo, PacketNo, DisplayName, CurrentDeptID, InitialPcs, InitialCarats, CurrentPcs, CurrentCarats, CurrentStatus) 
                    VALUES (@date, @inwardId, @kapan, @pkt, @display, @deptId, @pcs, @crt, @pcs, @crt, 'Accept')";

                using (SqlCommand cmd = new SqlCommand(insertQuery, cnn))
                {
                    cmd.Parameters.AddWithValue("@date", PacketDate);
                    cmd.Parameters.AddWithValue("@inwardId", SelectedInwardID);
                    cmd.Parameters.AddWithValue("@kapan", kapanNo);
                    cmd.Parameters.AddWithValue("@pkt", cleanPkt);
                    cmd.Parameters.AddWithValue("@display", displayName);
                    cmd.Parameters.AddWithValue("@deptId", SelectedDeptID);
                    cmd.Parameters.AddWithValue("@pcs", Pieces);
                    cmd.Parameters.AddWithValue("@crt", Carats);
                    cmd.ExecuteNonQuery();
                }

                Message = $"Packet '{displayName}' created successfully!";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "Error creating packet: " + ex.Message;
        }

        return RedirectToPage("/KapanPacketMaster");
    }

    public IActionResult OnPostDelete(int id)
    {
        try
        {
            using (SqlConnection cnn = new SqlConnection(_connectionString))
            {
                cnn.Open();
                string query = "DELETE FROM TabKapanPacketMaster WHERE KPID = @id";
                using (SqlCommand cmd = new SqlCommand(query, cnn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
            Message = "Packet deleted!";
        }
        catch (Exception ex)
        {
            ErrorMessage = "Error deleting: " + ex.Message;
        }

        return RedirectToPage("/KapanPacketMaster");
    }

    private void LoadDropdowns()
    {
        using (SqlConnection cnn = new SqlConnection(_connectionString))
        {
            cnn.Open();

            // Load Kapans with remaining balance info
            string kapanQuery = @"SELECT s.InwardID, s.KapanNo, s.PartyName, s.Carats,
                                  (s.Carats - ISNULL((SELECT SUM(InitialCarats) FROM TabKapanPacketMaster p WHERE p.InwardID = s.InwardID), 0)) AS BalCarats
                                  FROM TabStockInward s ORDER BY s.InwardID DESC";
            using (SqlCommand cmd = new SqlCommand(kapanQuery, cnn))
            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    InwardKapans.Add(new SelectListItem
                    {
                        Value = dr["InwardID"].ToString(),
                        Text = $"{dr["KapanNo"]} ({dr["PartyName"]}) - Bal: {Convert.ToDecimal(dr["BalCarats"]):0.000} Crt"
                    });
                }
            }

            // Load Departments
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

    private void LoadPackets()
    {
        PacketList.Clear();
        using (SqlConnection cnn = new SqlConnection(_connectionString))
        {
            cnn.Open();
            string query = @"SELECT p.KPID, p.PacketDate, p.DisplayName, d.DepartmentName, p.CurrentPcs, p.CurrentCarats, p.CurrentStatus
                             FROM TabKapanPacketMaster p
                             INNER JOIN TabDepartmentMaster d ON p.CurrentDeptID = d.DepartmentID
                             ORDER BY p.KPID DESC";
            using (SqlCommand cmd = new SqlCommand(query, cnn))
            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    PacketList.Add(new KapanPacketItem
                    {
                        KPID = Convert.ToInt32(dr["KPID"]),
                        PacketDate = Convert.ToDateTime(dr["PacketDate"]),
                        DisplayName = dr["DisplayName"].ToString() ?? "",
                        DepartmentName = dr["DepartmentName"].ToString() ?? "",
                        CurrentPcs = Convert.ToInt32(dr["CurrentPcs"]),
                        CurrentCarats = Convert.ToDecimal(dr["CurrentCarats"]),
                        CurrentStatus = dr["CurrentStatus"].ToString() ?? ""
                    });
                }
            }
        }
    }
}