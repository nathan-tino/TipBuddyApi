using System.ComponentModel.DataAnnotations;

namespace TipBuddyApi.Dtos.Shift
{
    public class GetShiftDto : BaseShiftDto
    {
        [Required]
        public required string Id { get; set; }
    }
}
