using System.ComponentModel.DataAnnotations;

namespace TipBuddyApi.Dtos.Shift
{
    public class UpdateShiftDto : BaseShiftDto
    {
        [Required]
        public required string Id { get; set; }
    }
}
