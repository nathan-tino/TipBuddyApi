using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TipBuddyApi.Contracts;
using TipBuddyApi.Data;
using TipBuddyApi.Dtos.Shift;

namespace TipBuddyApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ShiftsController : ControllerBase
    {
        private readonly IShiftsRepository _shiftsRepository;
        private readonly IMapper _mapper;

        public ShiftsController(IShiftsRepository shiftsRepository, IMapper mapper)
        {
            _shiftsRepository = shiftsRepository;
            _mapper = mapper;
        }

        // GET: api/Shifts
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GetShiftDto>>> GetShifts(DateTime? startDate = null, DateTime? endDate = null)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var shifts = await _shiftsRepository.GetShiftsAsync(userId, startDate, endDate);
            return _mapper.Map<List<GetShiftDto>>(shifts);
        }

        // GET: api/Shifts/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Shift>> GetShift(string id)
        {
            var shift = await _shiftsRepository.GetAsync(id);

            if (shift == null)
            {
                return NotFound();
            }

            return shift;
        }

        // PUT: api/Shifts/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutShift(string id, UpdateShiftDto updateShiftDto)
        {
            if (!id.Equals(updateShiftDto.Id))
            {
                return BadRequest();
            }

            var shift = await _shiftsRepository.GetAsync(id);
            if (shift == null)
            {
                return NotFound();
            }

            _mapper.Map(updateShiftDto, shift);

            try
            {
                await _shiftsRepository.UpdateAsync(shift);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await ShiftExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Shifts
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<GetShiftDto>> PostShift(CreateShiftDto createShiftDto)
        {
            var shift = _mapper.Map<Shift>(createShiftDto);
            shift.UserId = GetUserId() ?? throw new UnauthorizedAccessException();

            await _shiftsRepository.AddAsync(shift);

            return CreatedAtAction(nameof(GetShift), new { id = shift.Id }, _mapper.Map<GetShiftDto>(shift));
        }

        // DELETE: api/Shifts/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteShift(string id)
        {
            if (!await ShiftExists(id))
            {
                return NotFound();
            }

            await _shiftsRepository.DeleteAsync(id);

            return NoContent();
        }

        private Task<bool> ShiftExists(string id)
        {
            return _shiftsRepository.Exists(id);
        }

        private string? GetUserId()
        {
            return User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}
