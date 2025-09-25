using Microsoft.AspNetCore.Mvc;
using TipBuddyApi.Contracts;
using TipBuddyApi.Services;

namespace TipBuddyApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DemoDataController : ControllerBase
    {
        private readonly IDemoDataSeeder _demoDataSeeder;

        public DemoDataController(IDemoDataSeeder demoDataSeeder)
        {
            _demoDataSeeder = demoDataSeeder;
        }

        [HttpPost("reset")]
        public async Task<IActionResult> ResetDemoData()
        {
            await _demoDataSeeder.ResetDemoUserAsync();
            return Ok(new { message = "Demo data has been reset." });
        }

        [HttpPost("reset-shifts")]
        public async Task<IActionResult> ResetDemoShifts()
        {
            await _demoDataSeeder.ResetDemoUserShiftsAsync();
            return Ok(new { message = "Demo user shifts have been reset." });
        }
    }
}
