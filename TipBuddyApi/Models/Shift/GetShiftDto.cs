using System.ComponentModel.DataAnnotations;

namespace TipBuddyApi.Models.Shift
{
    public class GetShiftDto : BaseShiftDto
    {
        [Required]
        public int Id { get; set; }
    }
}
