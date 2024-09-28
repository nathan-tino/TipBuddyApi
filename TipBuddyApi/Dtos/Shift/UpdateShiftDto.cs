using System.ComponentModel.DataAnnotations;

namespace TipBuddyApi.Dtos.Shift
{
    public class UpdateShiftDto : BaseShiftDto
    {
        [Required]
        public int Id { get; set; }
    }
}
