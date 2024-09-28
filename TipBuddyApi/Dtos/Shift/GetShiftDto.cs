using System.ComponentModel.DataAnnotations;

namespace TipBuddyApi.Dtos.Shift
{
    public class GetShiftDto : BaseShiftDto
    {
        [Required]
        public int Id { get; set; }
    }
}
