using System.ComponentModel.DataAnnotations;

namespace TipBuddyApi.Dtos.Shift
{
    public abstract class BaseShiftDto
    {
        [Required]
        public DateTimeOffset Date { get; set; }

        [Required]
        public double CreditTips { get; set; }

        public double CashTips { get; set; }
        public double Tipout { get; set; }
        public int? HoursWorked { get; set; }
    }
}
