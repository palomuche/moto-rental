using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoRentalApi.Data;
using MotoRentalApi.Entities;
using System.Text.RegularExpressions;

namespace MotoRentalApi.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/moto")]
    public class MotoController : ControllerBase
    {
        private readonly ApiDbContext _context;
        private readonly ILogger<MotoController> _logger;

        public MotoController(ApiDbContext context, ILogger<MotoController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult Get()
        {
            var motos = _context.Motos.ToList();

            return Ok(motos);
        }

        [HttpGet("{plate}")]
        [ProducesResponseType(typeof(Moto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult Get(string plate)
        {
            plate = CleanPlate(plate);

            var moto = _context.Motos.FirstOrDefault(m => m.Plate == plate);

            if(moto == null) return NotFound();

            return Ok(moto);
        }

        [HttpPost]
        [ProducesResponseType(typeof(Moto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult Post(Moto moto)
        {
            moto.Plate = CleanPlate(moto.Plate);

            if (!IsPlateValid(moto.Plate))
            {
                _logger.LogError("Failed to create moto. Invalid plate format.");
                return BadRequest("Invalid plate format.");
            }

            if (!IsPlateUnique(moto.Plate))
            {
                _logger.LogError("Failed to create moto. Plate already exists.");
                return BadRequest("Plate already exists.");
            }

            _context.Motos.Add(moto);
            _context.SaveChanges();

            _logger.LogInformation("Moto created successfully.");
            return CreatedAtAction("Get", new { plate = moto.Plate }, moto);
        }

        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult Put(int id, string plate)
        {
            var cleanPlate = CleanPlate(plate);

            if (!IsPlateValid(cleanPlate))
                return BadRequest("Invalid plate format.");

            var moto = _context.Motos.FirstOrDefault(m => m.Id == id);

            if (moto == null) 
                return NotFound();


            if (CleanPlate(moto.Plate) != cleanPlate && !IsPlateUnique(cleanPlate))
                return BadRequest("Plate already exists.");

            moto.Plate = cleanPlate;

            _context.Motos.Update(moto);
            _context.SaveChanges();

            return NoContent();
        }
        

        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult Delete(int id)
        {
            var moto = _context.Motos.Find(id);

            if (moto == null) return NotFound();
            
            // Verifica se a moto esta locada, se estiver locada n�o deixar deletar

            _context.Motos.Remove(moto);
            _context.SaveChanges();

            return NoContent();
        }

        private bool IsPlateValid(string plate)
        {
            if (string.IsNullOrEmpty(plate))
                return false;

            Regex regex = new Regex(@"^[A-Z]{3}\d{1}[A-Z0-9]{1}\d{2}$");
            return regex.IsMatch(plate);
        }


        private string CleanPlate(string plate)
        {
            return Regex.Replace(plate.ToUpper(), "[^a-zA-Z0-9]", "");
        }

        private bool IsPlateUnique(string plate)
        {
            return !_context.Motos.Any(m => m.Plate == plate);
        }

        //[HttpGet]
        //public async Task<ActionResult<IEnumerable<Moto>>> Get()
        //{
        //    return await _context.Motos.ToListAsync();
        //}
    }
}