using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TipBuddyApi.Contracts;
using TipBuddyApi.Data;

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
        public async Task<ActionResult<IEnumerable<Shift>>> GetShifts()
        {
            return await _shiftsRepository.GetAllAsync();
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
        public async Task<IActionResult> PutShift(int id, Shift shift)
        {
            if (id != shift.Id)
            {
                return BadRequest();
            }

            if (await _shiftsRepository.GetAsync(id) == null)
            {
                return NotFound();
            }

            var shiftToUpdate = await _shiftsRepository.GetAsync(id);
            shiftToUpdate.Tipout = shift.Tipout;
            shiftToUpdate.HoursWorked = shift.HoursWorked;
            shiftToUpdate.CashTips = shift.CashTips;
            shiftToUpdate.CreditTips = shift.CreditTips;
            shiftToUpdate.Date = shift.Date;

            try
            {
                await _shiftsRepository.UpdateAsync(shiftToUpdate);
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
        public async Task<ActionResult<Shift>> PostShift(Shift shift)
        {
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
