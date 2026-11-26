using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace VinodKumarWeb.Pages;

public class DeptStockSummary
{
    public string DepartmentName { get; set; } = string.Empty;
    public int PacketCount { get; set; }
    public int TotalPcs { get; set; }
    public decimal TotalCarats { get; set; }
}

public class KapanComparison
{
    public string KapanNo { get; set; } = string.Empty;
    public string PartyName { get; set; } = string.Empty;
    public decimal RoughCarats { get; set; }
    public int PacketsCreated { get; set; }
    public decimal CurrentActiveCarats { get; set; }
    public decimal TotalLossCarats { get; set; }
}

public class ReportsModel : PageModel
{
    private readonly string _connectionString = "Server=localhost;Database=DBKOTHARIMFG;Trusted_Connection=True;TrustServerCertificate=True;";

    public List<DeptStockSummary> DeptSummaries { get; set; } = new();
    public List<KapanComparison> KapanComparisons { get; set; } = new();

    public void OnGet()
    {
        using (SqlConnection cnn = new SqlConnection(_connectionString))
        {
            cnn.Open();

            // 1. Department-wise Live Stock
            string deptSql = @"SELECT d.DepartmentName, COUNT(p.KPID) AS PktCount, ISNULL(SUM(p.CurrentPcs),0) AS Pcs, ISNULL(SUM(p.CurrentCarats),0) AS Crts
                               FROM TabDepartmentMaster d
                               LEFT JOIN TabKapanPacketMaster p ON d.DepartmentID = p.CurrentDeptID
                               GROUP BY d.DepartmentName
                               ORDER BY Crts DESC";
            using (SqlCommand cmd = new SqlCommand(deptSql, cnn))
            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    DeptSummaries.Add(new DeptStockSummary
                    {
                        DepartmentName = dr["DepartmentName"].ToString() ?? "",
                        PacketCount = Convert.ToInt32(dr["PktCount"]),
                        TotalPcs = Convert.ToInt32(dr["Pcs"]),
                        TotalCarats = Convert.ToDecimal(dr["Crts"])
                    });
                }
            }

            // 2. Kapan Rough vs Active Stock & Loss Summary
            string kapanSql = @"SELECT s.KapanNo, s.PartyName, s.Carats AS RoughCrt,
                                (SELECT COUNT(*) FROM TabKapanPacketMaster p WHERE p.InwardID = s.InwardID) AS Pkts,
                                ISNULL((SELECT SUM(CurrentCarats) FROM TabKapanPacketMaster p WHERE p.InwardID = s.InwardID), 0) AS ActiveCrt,
                                ISNULL((SELECT SUM(ir.LossCarats) FROM TabPacketIssueReceive ir INNER JOIN TabKapanPacketMaster p ON ir.KPID = p.KPID WHERE p.InwardID = s.InwardID), 0) AS LossCrt
                                FROM TabStockInward s
                                ORDER BY s.InwardID DESC";
            using (SqlCommand cmd = new SqlCommand(kapanSql, cnn))
            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    KapanComparisons.Add(new KapanComparison
                    {
                        KapanNo = dr["KapanNo"].ToString() ?? "",
                        PartyName = dr["PartyName"].ToString() ?? "",
                        RoughCarats = Convert.ToDecimal(dr["RoughCrt"]),
                        PacketsCreated = Convert.ToInt32(dr["Pkts"]),
                        CurrentActiveCarats = Convert.ToDecimal(dr["ActiveCrt"]),
                        TotalLossCarats = Convert.ToDecimal(dr["LossCrt"])
                    });
                }
            }
        }
    }
}