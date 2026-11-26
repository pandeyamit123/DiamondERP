using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;

namespace VinodKumarWeb.Pages;

public class GradedDiamond
{
    public int GradingID { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string ShapeName { get; set; } = string.Empty;
    public string ColorName { get; set; } = string.Empty;
    public string ClarityName { get; set; } = string.Empty;
    public int PolishPcs { get; set; }
    public decimal PolishCarats { get; set; }
    public decimal RapRate { get; set; }
    public decimal TotalValue { get; set; }
    public string OutwardStatus { get; set; } = string.Empty;
    public string BuyerName { get; set; } = string.Empty;
    public decimal? SaleAmount { get; set; }
}

public class PolishGradingModel : PageModel
{
    private readonly string _connectionString = "Server=localhost;Database=DBKOTHARIMFG;Trusted_Connection=True;TrustServerCertificate=True;";

    public List<SelectListItem> Packets { get; set; } = new();
    public List<SelectListItem> Shapes { get; set; } = new();
    public List<SelectListItem> Colors { get; set; } = new();
    public List<SelectListItem> Clarities { get; set; } = new();
    public List<GradedDiamond> GradedList { get; set; } = new();

    [BindProperty] public int SelectedKPID { get; set; }
    [BindProperty] public int ShapeID { get; set; }
    [BindProperty] public int ColorID { get; set; }
    [BindProperty] public int ClarityID { get; set; }
    [BindProperty] public int PolishPcs { get; set; } = 1;
    [BindProperty] public decimal PolishCarats { get; set; }
    [BindProperty] public decimal RapRate { get; set; }

    [TempData] public string Message { get; set; } = string.Empty;
    [TempData] public string ErrorMessage { get; set; } = string.Empty;

    public void OnGet()
    {
        LoadDropdowns();
        LoadGradedStock();
    }

    public IActionResult OnPostGrade()
    {
        if (SelectedKPID == 0 || PolishCarats <= 0)
        {
            ErrorMessage = "Packet selection and Polish Carats are required.";
            return RedirectToPage("/PolishGrading");
        }

        decimal totalVal = PolishCarats * RapRate;

        try
        {
            using (SqlConnection cnn = new SqlConnection(_connectionString))
            {
                cnn.Open();
                string sql = @"INSERT INTO TabPolishGrading 
                    (GradingDate, KPID, ShapeID, ColorID, ClarityID, PolishPcs, PolishCarats, RapRate, TotalValue, OutwardStatus)
                    VALUES (GETDATE(), @kpid, @shape, @color, @clarity, @pcs, @crt, @rate, @val, 'InStock')";
                using (SqlCommand cmd = new SqlCommand(sql, cnn))
                {
                    cmd.Parameters.AddWithValue("@kpid", SelectedKPID);
                    cmd.Parameters.AddWithValue("@shape", ShapeID);
                    cmd.Parameters.AddWithValue("@color", ColorID);
                    cmd.Parameters.AddWithValue("@clarity", ClarityID);
                    cmd.Parameters.AddWithValue("@pcs", PolishPcs);
                    cmd.Parameters.AddWithValue("@crt", PolishCarats);
                    cmd.Parameters.AddWithValue("@rate", RapRate);
                    cmd.Parameters.AddWithValue("@val", totalVal);
                    cmd.ExecuteNonQuery();
                }

                using (SqlCommand cmd = new SqlCommand("UPDATE TabKapanPacketMaster SET CurrentPcs = @pcs, CurrentCarats = @crt, CurrentStatus = 'Graded' WHERE KPID = @kpid", cnn))
                {
                    cmd.Parameters.AddWithValue("@pcs", PolishPcs);
                    cmd.Parameters.AddWithValue("@crt", PolishCarats);
                    cmd.Parameters.AddWithValue("@kpid", SelectedKPID);
                    cmd.ExecuteNonQuery();
                }

                Message = "Polish Diamond Graded & added to Ready Stock!";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "Error: " + ex.Message;
        }

        return RedirectToPage("/PolishGrading");
    }

    public IActionResult OnPostSell(int gradingId, string buyerName, decimal saleAmount)
    {
        try
        {
            using (SqlConnection cnn = new SqlConnection(_connectionString))
            {
                cnn.Open();
                string sql = "UPDATE TabPolishGrading SET OutwardStatus = 'Sold', BuyerName = @buyer, SaleAmount = @amt, SaleDate = GETDATE() WHERE GradingID = @id";
                using (SqlCommand cmd = new SqlCommand(sql, cnn))
                {
                    cmd.Parameters.AddWithValue("@buyer", buyerName.Trim().ToUpper());
                    cmd.Parameters.AddWithValue("@amt", saleAmount);
                    cmd.Parameters.AddWithValue("@id", gradingId);
                    cmd.ExecuteNonQuery();
                }
                Message = "Polish packet marked as Sold / Outward!";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "Error: " + ex.Message;
        }
        return RedirectToPage("/PolishGrading");
    }

    private void LoadDropdowns()
    {
        using (SqlConnection cnn = new SqlConnection(_connectionString))
        {
            cnn.Open();
            using (SqlCommand cmd = new SqlCommand("SELECT KPID, DisplayName, CurrentCarats FROM TabKapanPacketMaster WHERE CurrentStatus = 'Accept'", cnn))
            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    Packets.Add(new SelectListItem { Value = dr["KPID"].ToString(), Text = $"{dr["DisplayName"]} ({Convert.ToDecimal(dr["CurrentCarats"]):0.000} Crt)" });
                }
            }
            Shapes = LoadList(cnn, "SELECT ShapeID, ShapeName FROM TabShapeMaster");
            Colors = LoadList(cnn, "SELECT ColorID, ColorName FROM TabColorMaster");
            Clarities = LoadList(cnn, "SELECT ClarityID, ClarityName FROM TabClarityMaster");
        }
    }

    private List<SelectListItem> LoadList(SqlConnection cnn, string sql)
    {
        var list = new List<SelectListItem>();
        using (SqlCommand cmd = new SqlCommand(sql, cnn))
        using (SqlDataReader dr = cmd.ExecuteReader())
        {
            while (dr.Read()) list.Add(new SelectListItem { Value = dr[0].ToString(), Text = dr[1].ToString() });
        }
        return list;
    }

    private void LoadGradedStock()
    {
        GradedList.Clear();
        using (SqlConnection cnn = new SqlConnection(_connectionString))
        {
            cnn.Open();
            string sql = @"SELECT g.GradingID, p.DisplayName, s.ShapeName, c.ColorName, cl.ClarityName,
                           g.PolishPcs, g.PolishCarats, g.RapRate, g.TotalValue, g.OutwardStatus, g.BuyerName, g.SaleAmount
                           FROM TabPolishGrading g
                           INNER JOIN TabKapanPacketMaster p ON g.KPID = p.KPID
                           INNER JOIN TabShapeMaster s ON g.ShapeID = s.ShapeID
                           INNER JOIN TabColorMaster c ON g.ColorID = c.ColorID
                           INNER JOIN TabClarityMaster cl ON g.ClarityID = cl.ClarityID
                           ORDER BY g.GradingID DESC";
            using (SqlCommand cmd = new SqlCommand(sql, cnn))
            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    GradedList.Add(new GradedDiamond
                    {
                        GradingID = Convert.ToInt32(dr["GradingID"]),
                        DisplayName = dr["DisplayName"].ToString() ?? "",
                        ShapeName = dr["ShapeName"].ToString() ?? "",
                        ColorName = dr["ColorName"].ToString() ?? "",
                        ClarityName = dr["ClarityName"].ToString() ?? "",
                        PolishPcs = Convert.ToInt32(dr["PolishPcs"]),
                        PolishCarats = Convert.ToDecimal(dr["PolishCarats"]),
                        RapRate = Convert.ToDecimal(dr["RapRate"]),
                        TotalValue = Convert.ToDecimal(dr["TotalValue"]),
                        OutwardStatus = dr["OutwardStatus"].ToString() ?? "",
                        BuyerName = dr["BuyerName"] != DBNull.Value ? dr["BuyerName"].ToString() ?? "" : "",
                        SaleAmount = dr["SaleAmount"] != DBNull.Value ? Convert.ToDecimal(dr["SaleAmount"]) : null
                    });
                }
            }
        }
    }
}