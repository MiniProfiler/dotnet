using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Samples.Remote.Api.Data;

namespace Samples.Remote.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SamplesController : ControllerBase
    {
        private readonly SampleContext _context;

        public SamplesController(SampleContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Sample>>> GetAll()
        {
            return Ok(await _context.Samples.ToListAsync());
        }

        [HttpGet("odd")]
        public async Task<ActionResult<IEnumerable<Sample>>> GetOdd()
        {
            return Ok(await _context.Samples.Where(s => s.Id % 2 != 0).ToListAsync());
        }
    }
}
