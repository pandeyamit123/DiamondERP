using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;

namespace VinodKumarWeb.Pages;

public class StockInwardItem
{
    public int InwardID { get; set; }
    public DateTime InwardDate { get; set; }
    public string KapanNo { get; set; } = string.Empty;
    public string PartyName { get; set; } = string.Empty;
    public string RoughTypeName { get; set; } = string.Empty;
    public string RoughSizeName { get; set; } = string.Empty;
    public string RoughColorName { get; set; } = string.Empty;
    public string RoughPurityName { get; set; } = string.Empty;
    public int Pieces { get; set; }
    public decimal Carats { get; set; }
    public decimal Rate { get; set; }
    public decimal TotalAmount { get; set; }
}

public class StockInwardModel : PageModel
{
    private readonly string _connectionString = "Server=localhost;Database=DBKOTHARIMFG;Trusted_Connection=True;TrustServerCertificate=True;";

    public List<StockInwardItem> InwardList { get; set; } = new();
    public List<SelectListItem> RoughTypes { get; set; } = new();
    public List<SelectListItem> RoughSizes { get; set; } = new();
    public List<SelectListItem> RoughColors { get; set; } = new();
    public List<SelectListItem> RoughPurities { get; set; } = new();

    [BindProperty]
    public DateTime InwardDate { get; set; } = DateTime.Today;

    [BindProperty]
    public string KapanNo { get; set; } = string.Empty;

    [BindProperty]
    public string PartyName { get; set; } = string.Empty;

    [BindProperty]
    public int RoughTypeID { get; set; }

    [BindProperty]
    public int RoughSizeID { get; set; }

    [BindProperty]
    public int RoughColorID { get; set; }

    [BindProperty]
    public int RoughPurityID { get; set; }

    [BindProperty]
    public int Pieces { get; set; }

    [BindProperty]
    public decimal Carats { get; set; }

    [BindProperty]
    public decimal Rate { get; set; }

    [TempData]
    public string Message { get; set; } = string.Empty;

    [TempData]
    public string ErrorMessage { get; set; } = string.Empty;

    public void OnGet()
    {
        LoadDropdowns();
        LoadInwardList();
    }

    public IActionResult OnPostAdd()
    {
        if (string.IsNullOrWhiteSpace(KapanNo) || string.IsNullOrWhiteSpace(PartyName) || Carats <= 0)
        {
            ErrorMessage = "Kapan No, Party Name, and valid Carats are required.";
            return RedirectToPage("/StockInward");
        }

        decimal totalAmount = Carats * Rate;

        try
        {
            using (SqlConnection cnn = new SqlConnection(_connectionString))
            {
                cnn.Open();
                string query = @"INSERT INTO TabStockInward 
                    (InwardDate, KapanNo, PartyName, RoughTypeID, RoughSizeID, RoughColorID, RoughPurityID, Pieces, Carats, Rate, TotalAmount) 
                    VALUES (@date, @kapan, @party, @typeId, @sizeId, @colorId, @purityId, @pcs, @crt, @rate, @amt)";

                using (SqlCommand cmd = new SqlCommand(query, cnn))
                {
                    cmd.Parameters.AddWithValue("@date", InwardDate);
                    cmd.Parameters.AddWithValue("@kapan", KapanNo.Trim().ToUpper());
                    cmd.Parameters.AddWithValue("@party", PartyName.Trim().ToUpper());
                    cmd.Parameters.AddWithValue("@typeId", RoughTypeID);
                    cmd.Parameters.AddWithValue("@sizeId", RoughSizeID);
                    cmd.Parameters.AddWithValue("@colorId", RoughColorID);
                    cmd.Parameters.AddWithValue("@purityId", RoughPurityID);
                    cmd.Parameters.AddWithValue("@pcs", Pieces);
                    cmd.Parameters.AddWithValue("@crt", Carats);
                    cmd.Parameters.AddWithValue("@rate", Rate);
                    cmd.Parameters.AddWithValue("@amt", totalAmount);
                    cmd.ExecuteNonQuery();
                }
            }
            Message = "Stock Inward entry saved successfully!";
        }
        catch (Exception ex)
        {
            ErrorMessage = "Error saving entry: " + ex.Message;
        }

        return RedirectToPage("/StockInward");
    }

    public IActionResult OnPostDelete(int id)
    {
        try
        {
            using (SqlConnection cnn = new SqlConnection(_connectionString))
            {
                cnn.Open();
                string query = "DELETE FROM TabStockInward WHERE InwardID = @id";
                using (SqlCommand cmd = new SqlCommand(query, cnn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
            Message = "Stock Inward entry deleted!";
        }
        catch (Exception ex)
        {
            ErrorMessage = "Error deleting: " + ex.Message;
        }

        return RedirectToPage("/StockInward");
    }

    private void LoadDropdowns()
    {
        using (SqlConnection cnn = new SqlConnection(_connectionString))
        {
            cnn.Open();
            RoughTypes = GetSelectList(cnn, "SELECT RoughTypeID, RoughTypeName FROM TabRoughTypeMaster ORDER BY RoughTypeName");
            RoughSizes = GetSelectList(cnn, "SELECT RoughSizeID, RoughSizeName FROM TabRoughSizeMaster ORDER BY RoughSizeName");
            RoughColors = GetSelectList(cnn, "SELECT RoughColorID, RoughColorName FROM TabRoughColorMaster ORDER BY RoughColorName");
            RoughPurities = GetSelectList(cnn, "SELECT RoughPurityID, RoughPurityName FROM TabRoughPurityMaster ORDER BY RoughPurityName");
        }
    }

    private List<SelectListItem> GetSelectList(SqlConnection cnn, string query)
    {
        var list = new List<SelectListItem>();
        using (SqlCommand cmd = new SqlCommand(query, cnn))
        using (SqlDataReader dr = cmd.ExecuteReader())
        {
            while (dr.Read())
            {
                list.Add(new SelectListItem { Value = dr[0].ToString(), Text = dr[1].ToString() });
            }
        }
        return list;
    }

    private void LoadInwardList()
    {
        InwardList.Clear();
        using (SqlConnection cnn = new SqlConnection(_connectionString))
        {
            cnn.Open();
            string query = @"SELECT s.InwardID, s.InwardDate, s.KapanNo, s.PartyName, 
                             t.RoughTypeName, sz.RoughSizeName, c.RoughColorName, p.RoughPurityName,
                             s.Pieces, s.Carats, s.Rate, s.TotalAmount
                             FROM TabStockInward s
                             INNER JOIN TabRoughTypeMaster t ON s.RoughTypeID = t.RoughTypeID
                             INNER JOIN TabRoughSizeMaster sz ON s.RoughSizeID = sz.RoughSizeID
                             INNER JOIN TabRoughColorMaster c ON s.RoughColorID = c.RoughColorID
                             INNER JOIN TabRoughPurityMaster p ON s.RoughPurityID = p.RoughPurityID
                             ORDER BY s.InwardID DESC";

            using (SqlCommand cmd = new SqlCommand(query, cnn))
            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    InwardList.Add(new StockInwardItem
                    {
                        InwardID = Convert.ToInt32(dr["InwardID"]),
                        InwardDate = Convert.ToDateTime(dr["InwardDate"]),
                        KapanNo = dr["KapanNo"].ToString() ?? "",
                        PartyName = dr["PartyName"].ToString() ?? "",
                        RoughTypeName = dr["RoughTypeName"].ToString() ?? "",
                        RoughSizeName = dr["RoughSizeName"].ToString() ?? "",
                        RoughColorName = dr["RoughColorName"].ToString() ?? "",
                        RoughPurityName = dr["RoughPurityName"].ToString() ?? "",
                        Pieces = Convert.ToInt32(dr["Pieces"]),
                        Carats = Convert.ToDecimal(dr["Carats"]),
                        Rate = Convert.ToDecimal(dr["Rate"]),
                        TotalAmount = Convert.ToDecimal(dr["TotalAmount"])
                    });
                }
            }
        }
    }
}