using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MotoRentalApi.Data;
using MotoRentalApi.ViewModels;

namespace MotoRentalApi.Controllers
{
    [Authorize(Roles = "Deliverer")]
    [ApiController]
    [Route("api/deliverer")]
    public class DelivererController : Controller
    {
        private readonly ApiDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly LocalStorageService _storageService;

        public DelivererController(ApiDbContext context,
                              UserManager<IdentityUser> userManager,
                              LocalStorageService storageService)
        {
            _context = context;
            _userManager = userManager;
            _storageService = storageService;
        }

        [HttpPost("upload-driver-license")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public IActionResult UploadDriverLicencePhoto(IFormFile file)
        {
            // Check if the deliverer exists
            var username = _userManager.GetUserName(User);
            var user = _userManager.FindByNameAsync(username).Result;
            if (user == null) return NotFound("User not found."); 

            var deliverer = _context.Deliverers.Find(user.Id);
            if (deliverer == null) return NotFound("Deliverer not found.");

            var allowedExtensions = new[] { ".png", ".bmp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            // Validate file format
            if (file == null || (file.ContentType != "image/png" && file.ContentType != "image/bmp") || !allowedExtensions.Contains(extension))
            {
                return BadRequest("Invalid file format. The driver's license must be a PNG or BMP image.");
            }

            // Store the file in local disk
            var filePath = _storageService.UploadPhoto(file);

            // Update the deliverer's record to include the reference to the driver's license photo file
            deliverer.DriverLicensePhotoPath = filePath;
            _context.SaveChanges();

            return Ok("Driver's license photo uploaded successfully.");
        }
    }
}
