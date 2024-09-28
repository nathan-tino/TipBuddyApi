using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TipBuddyApi.Contracts;
using TipBuddyApi.Data;
using TipBuddyApi.Dtos.Shift;

namespace TipBuddyApi.Controllers
{
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
        public async Task<ActionResult<IEnumerable<GetShiftDto>>> GetShifts()
        {
            var shifts = await _shiftsRepository.GetAllAsync();
            return _mapper.Map<List<GetShiftDto>>(shifts);
        }

        // GET: api/Shifts/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Shift>> GetShift(int id)
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
        public async Task<IActionResult> PutShift(int id, UpdateShiftDto updateShiftDto)
        {
            if (id != updateShiftDto.Id)
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
        public async Task<ActionResult<Shift>> PostShift(CreateShiftDto createShiftDto)
        {
            var shift = _mapper.Map<Shift>(createShiftDto);
            await _shiftsRepository.AddAsync(shift);

            return CreatedAtAction(nameof(GetShift), new { id = shift.Id }, shift);
        }

        // DELETE: api/Shifts/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteShift(int id)
        {
            if (!await ShiftExists(id))
            {
                return NotFound();
            }

            await _shiftsRepository.DeleteAsync(id);

            return NoContent();
        }

        private Task<bool> ShiftExists(int id)
        {
            return _shiftsRepository.Exists(id);
        }
    }
}
