using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TipBuddyApi.Data
{
    public class Shift
    {
        [Key]
        public string Id { get; set; }

        [Required]
        public DateTimeOffset Date { get; set; }

        public double CreditTips { get; set; }
        public double CashTips { get; set; }
        public double Tipout { get; set; }
        public int? HoursWorked { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        [Required]
        [ForeignKey(nameof(User))]
        public required string UserId { get; set; }
        public User User { get; set; } = null!;

        public Shift()
        {
            Id = Guid.NewGuid().ToString();
        }
    }
}
