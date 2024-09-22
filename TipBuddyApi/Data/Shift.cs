namespace TipBuddyApi.Data
{
    public class Shift
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public double CreditTips { get; set; }
        public double CashTips { get; set; }
        public double Tipout { get; set; }
        public int? HoursWorked { get; set; }
    }
}
