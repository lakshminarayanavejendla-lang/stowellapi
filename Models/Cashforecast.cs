namespace StowellCoAPI.Models
{
    public class Cashforecast
    {

    }
    public class CashFlowForecastResponse
    {
        public List<CashFlowForecastRow> BudgetForecast { get; set; } = new();
        public List<CashFlowForecastRow> ContractForecast { get; set; } = new();
    }

    public class CashFlowForecastRow
    {
        public string JobNumber { get; set; }
        public string JobName { get; set; }

        public decimal TotalAmount { get; set; }
        public decimal CurrentAmount { get; set; }
        public decimal Remaining { get; set; }

        public Dictionary<string, decimal> ForecastMonths { get; set; }
            = new Dictionary<string, decimal>();
    }
}
