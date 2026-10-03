using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Collections.Concurrent;
using System.Data;

namespace StowellCoAPI.Controllers
{
    /// <summary>
    /// Reports > Monthly Financials: runs Reports/MonthlyFinancials.sql (the "Financials ALL" package) for one accounting
    /// period. GetMonthlyFinancials returns the whole package as an Excel workbook, one tab per workbook tab (Balance Sheet,
    /// Inc. Stmt, Over Under, AR Aging, FS Balance Sheet, FS Stmt of Inc &amp; RE) plus a Checks tab. GetReport and
    /// GetReportExcel return a single report from the package (as data for the app, or as its own workbook). Read-only.
    /// Defaults to the prior calendar month. A period's results are kept for 10 minutes, so opening several reports for
    /// the same month runs the package once.
    /// </summary>
    [ApiController]
    public class MonthlyFinancialsController : ControllerBase
    {
        private readonly ILogger<MonthlyFinancialsController> _logger;
        private readonly IConfiguration _configuration;

        public MonthlyFinancialsController(ILogger<MonthlyFinancialsController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        // result-set "report" prefix -> workbook tab
        private static readonly (string Prefix, string Tab)[] TabMap =
        {
            ("1-", "Balance Sheet"),
            ("2", "Inc. Stmt- ALL"),
            ("3-", "Over Under Report"),
            ("4-", "AR Aging"),
            ("5a", "FS Balance Sheet"),
            ("5b", "FS Stmt of Inc & RE"),
            ("6-", "Checks"),
        };

        // order the SELECTs come back in Reports/MonthlyFinancials.sql
        private static readonly string[] ResultSetOrder =
        {
            "1-BalanceSheet", "1-BalanceSheet totals", "2-IncomeStatement", "2b-IncomeStatement summary",
            "3-OverUnder", "3-OverUnder totals vs GL", "4-ARAging", "4-ARAging totals",
            "5a-FS Balance Sheet", "5b-FS Income & RE", "6-Check",
        };

        // The reports the app lists under Reports > Monthly Financials – All, and the result sets each one shows.
        // Backlog isn't in the package yet, so it has no result sets.
        private static readonly Dictionary<string, (string Title, string[] Prefixes)> Reports = new()
        {
            ["statement-of-income"] = ("Statement of Income", new[] { "5b" }),
            ["income-statement-all"] = ("Income Statement - All Departments", new[] { "2-", "2b" }),
            ["over-under"] = ("Over Under", new[] { "3-" }),
            ["ar-aging"] = ("AR Aging", new[] { "4-" }),
            ["balance-sheet"] = ("Monthly Balance Sheet", new[] { "1-", "5a" }),
            ["backlog"] = ("Backlog Report", Array.Empty<string>()),
        };

        private record Section(string Tab, string Title, DataTable Data);

        // period ("2026-9") -> when it was run and its result sets
        private static readonly ConcurrentDictionary<string, (DateTime At, List<Section> Sections)> Cache = new();
        private static readonly TimeSpan CacheFor = TimeSpan.FromMinutes(10);

        [HttpGet("api/Reports/MonthlyFinancials")]
        public async Task<IActionResult> GetMonthlyFinancials([FromQuery] int? year, [FromQuery] int? month)
        {
            try
            {
                if (!TryPeriod(year, month, out int yr, out int mo, out var bad)) return bad!;
                var sections = await RunPackage(yr, mo);
                var bytes = BuildWorkbook(sections.GroupBy(s => s.Tab).Select(g => (g.Key, g.ToList())), yr, mo);
                string fileName = $"{yr} - {new DateTime(yr, mo, 1):MMMM} Financials ALL.xlsx";
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Monthly financials failed");
                return StatusCode(500, new { Message = "Error generating monthly financials.", Details = ex.Message });
            }
        }

        /// <summary>One report from the package, as data: its sections, each with column names and rows.
        /// refresh=true re-pulls the data from Sage instead of using the copy kept for 10 minutes (Refresh Report).</summary>
        [HttpGet("api/Reports/MonthlyFinancials/Report")]
        public async Task<IActionResult> GetReport([FromQuery] string key, [FromQuery] int? year, [FromQuery] int? month, [FromQuery] bool refresh = false)
        {
            try
            {
                if (!Reports.TryGetValue(key ?? "", out var report)) return NotFound(new { Message = $"Unknown report '{key}'." });
                if (!TryPeriod(year, month, out int yr, out int mo, out var bad)) return bad!;
                var asOf = new DateTime(yr, mo, DateTime.DaysInMonth(yr, mo));

                if (report.Prefixes.Length == 0)
                {
                    return Ok(new { key, title = report.Title, year = yr, month = mo, asOf, available = false, sections = Array.Empty<object>() });
                }

                var sections = (await RunPackage(yr, mo, refresh)).Where(s => report.Prefixes.Any(p => s.Title.StartsWith(p))).ToList();
                var data = sections.Select(s =>
                {
                    // the first column is the "report" label itself, so it's left out
                    int skip = s.Data.Columns.Count > 0 && s.Data.Columns[0].ColumnName == "report" ? 1 : 0;
                    var columns = s.Data.Columns.Cast<DataColumn>().Skip(skip).Select(c => c.ColumnName).ToArray();
                    var rows = s.Data.Rows.Cast<DataRow>()
                        .Select(r => s.Data.Columns.Cast<DataColumn>().Skip(skip).Select(c => r[c] == DBNull.Value ? null : r[c]).ToArray())
                        .ToArray();
                    return new { title = s.Title, columns, rows };
                });
                return Ok(new { key, title = report.Title, year = yr, month = mo, asOf, available = true, sections = data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Monthly financials report {Key} failed", key);
                return StatusCode(500, new { Message = "Error generating the report.", Details = ex.Message });
            }
        }

        /// <summary>One report from the package as its own Excel workbook.</summary>
        [HttpGet("api/Reports/MonthlyFinancials/Report/Excel")]
        public async Task<IActionResult> GetReportExcel([FromQuery] string key, [FromQuery] int? year, [FromQuery] int? month)
        {
            try
            {
                if (!Reports.TryGetValue(key ?? "", out var report)) return NotFound(new { Message = $"Unknown report '{key}'." });
                if (report.Prefixes.Length == 0) return NotFound(new { Message = $"{report.Title} isn't in the Financials package yet." });
                if (!TryPeriod(year, month, out int yr, out int mo, out var bad)) return bad!;

                var sections = (await RunPackage(yr, mo)).Where(s => report.Prefixes.Any(p => s.Title.StartsWith(p))).ToList();
                var bytes = BuildWorkbook(new[] { (report.Title, sections) }, yr, mo);
                string fileName = $"{yr} - {new DateTime(yr, mo, 1):MMMM} {report.Title}.xlsx";
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Monthly financials Excel {Key} failed", key);
                return StatusCode(500, new { Message = "Error generating the report.", Details = ex.Message });
            }
        }

        private static bool TryPeriod(int? year, int? month, out int yr, out int mo, out IActionResult? bad)
        {
            var prior = DateTime.Today.AddMonths(-1);
            yr = year ?? prior.Year;
            mo = month ?? prior.Month;
            bad = null;
            if (yr < 2000 || yr > 2100 || mo < 1 || mo > 12)
            {
                bad = new BadRequestObjectResult(new { Message = "year/month out of range." });
                return false;
            }
            return true;
        }

        /// <summary>Runs the package for one period, or returns the copy from the last 10 minutes unless refresh is set.</summary>
        private async Task<List<Section>> RunPackage(int yr, int mo, bool refresh = false)
        {
            string cacheKey = $"{yr}-{mo}";
            if (!refresh && Cache.TryGetValue(cacheKey, out var hit) && DateTime.UtcNow - hit.At < CacheFor) return hit.Sections;

            var asOf = new DateTime(yr, mo, DateTime.DaysInMonth(yr, mo));
            string sql = await System.IO.File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Reports", "MonthlyFinancials.sql"));

            // The Sage database: server and credentials come from the StowellConnection string, and the database
            // is MonthlyFinancials:Database, or StowellCompany when that isn't set. A copy of the data restored
            // as StowellSandbox (see 01-Databases) sets it to StowellSandbox.
            var csb = new SqlConnectionStringBuilder(_configuration.GetConnectionString("StowellConnection"))
            {
                InitialCatalog = _configuration["MonthlyFinancials:Database"] is { Length: > 0 } db ? db : "StowellCompany"
            };

            using var connection = new SqlConnection(csb.ConnectionString);
            await connection.OpenAsync();

            // the package is one batch of SELECTs plus temp tables; @Yr/@Prd/@AsOf are supplied here
            using var cmd = new SqlCommand(sql, connection) { CommandTimeout = 300 };
            cmd.Parameters.Add(new SqlParameter("@Yr", SqlDbType.SmallInt) { Value = (short)yr });
            cmd.Parameters.Add(new SqlParameter("@Prd", SqlDbType.TinyInt) { Value = (byte)mo });
            cmd.Parameters.Add(new SqlParameter("@AsOf", SqlDbType.Date) { Value = asOf });

            var sections = new List<Section>();
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                // DataTable.Load consumes the current result set and moves the reader on to the next one
                int setIndex = 0;
                while (!reader.IsClosed)
                {
                    var table = new DataTable();
                    table.Load(reader);
                    if (table.Columns.Count == 0) continue;
                    // empty result sets carry no "report" value, so fall back to the script's fixed result-set order
                    string title = table.Rows.Count > 0 ? Convert.ToString(table.Rows[0][0]) ?? "" : "";
                    if (title == "" && setIndex < ResultSetOrder.Length) title = ResultSetOrder[setIndex];
                    setIndex++;
                    string key = table.Columns[0].ColumnName == "report" ? title : "6-";
                    var tab = TabMap.FirstOrDefault(m => key.StartsWith(m.Prefix)).Tab ?? "Other";
                    sections.Add(new Section(tab, key == "6-" ? "6-Validation checks" : title, table));
                }
            }

            Cache[cacheKey] = (DateTime.UtcNow, sections);
            return sections;
        }

        /// <summary>One sheet per tab, each listing its sections (a bold title, a header row and the data rows).</summary>
        private static byte[] BuildWorkbook(IEnumerable<(string Tab, List<Section> Sections)> tabs, int yr, int mo)
        {
            var asOf = new DateTime(yr, mo, DateTime.DaysInMonth(yr, mo));
            using var wb = new XSSFWorkbook();
            string monthName = new DateTime(yr, mo, 1).ToString("MMM yyyy");
            var bold = wb.CreateFont(); bold.IsBold = true;
            var boldStyle = wb.CreateCellStyle(); boldStyle.SetFont(bold);
            var money = wb.CreateCellStyle(); money.DataFormat = wb.CreateDataFormat().GetFormat("#,##0.00;[Red]-#,##0.00");

            foreach (var (tabName, group) in tabs)
            {
                string sheetName = $"{monthName} - {tabName}";
                if (sheetName.Length > 31) sheetName = sheetName[..31];
                var sheet = wb.CreateSheet(sheetName);
                int r = 0;
                var t = sheet.CreateRow(r++).CreateCell(0);
                t.SetCellValue($"{tabName} - period ending {asOf:MM/dd/yyyy}"); t.CellStyle = boldStyle;
                r++;
                foreach (var s in group)
                {
                    var data = s.Data;
                    sheet.CreateRow(r++).CreateCell(0).SetCellValue(s.Title);
                    sheet.GetRow(r - 1).GetCell(0).CellStyle = boldStyle;
                    var head = sheet.CreateRow(r++);
                    for (int c = 0; c < data.Columns.Count; c++)
                    {
                        var hc = head.CreateCell(c); hc.SetCellValue(data.Columns[c].ColumnName); hc.CellStyle = boldStyle;
                    }
                    foreach (DataRow dr in data.Rows)
                    {
                        var row = sheet.CreateRow(r++);
                        for (int c = 0; c < data.Columns.Count; c++)
                        {
                            var v = dr[c];
                            var cell = row.CreateCell(c);
                            if (v == DBNull.Value) continue;
                            switch (v)
                            {
                                case decimal d: cell.SetCellValue((double)d); cell.CellStyle = money; break;
                                case double dbl: cell.SetCellValue(dbl); cell.CellStyle = money; break;
                                case float f: cell.SetCellValue(f); cell.CellStyle = money; break;
                                case int or long or short or byte: cell.SetCellValue(Convert.ToDouble(v)); break;
                                case DateTime dt: cell.SetCellValue(dt.ToString("MM/dd/yyyy")); break;
                                default: cell.SetCellValue(Convert.ToString(v)); break;
                            }
                        }
                    }
                    r++;
                }
                int cols = group.Count == 0 ? 0 : group.Max(g => g.Data.Columns.Count);
                for (int c = 0; c < cols; c++) sheet.AutoSizeColumn(c);
            }

            using var ms = new MemoryStream();
            wb.Write(ms, leaveOpen: true);
            return ms.ToArray();
        }
    }
}
