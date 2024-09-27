using System.ComponentModel.DataAnnotations;

namespace TipBuddyApi.Models.Shift
{
    public class UpdateShiftDto : BaseShiftDto
    {
        [Required]
        public int Id { get; set; }
    }
}
