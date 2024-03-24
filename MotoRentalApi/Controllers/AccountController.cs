using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MotoRentalApi.Data;
using MotoRentalApi.Entities;
using MotoRentalApi.ViewModels;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace MotoRentalApi.Controllers
{
    [ApiController]
    [Route("api/account")]
    public class AccountController : ControllerBase
    {
        private readonly ApiDbContext _context;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly JwtSettings _jwtSettings;
        private readonly LocalStorageService _storageService;

        public AccountController(ApiDbContext context,
                              SignInManager<IdentityUser> signInManager,
                              UserManager<IdentityUser> userManager,
                              RoleManager<IdentityRole> roleManager,
                              IOptions<JwtSettings> jwtSettings,
                              LocalStorageService storageService)
        {
            _context = context;
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _jwtSettings = jwtSettings.Value;
            _storageService = storageService;
        }

        [HttpPost("register-admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> Register(RegisterUserViewModel registerUser)
        {
            var user = new IdentityUser
            {
                UserName = registerUser.Username,
                Email = registerUser.Email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, registerUser.Password);

            if (result.Succeeded)
            {
                if (!await _roleManager.RoleExistsAsync("Admin"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("Admin"));
                }

                await _userManager.AddToRoleAsync(user, "Admin");

                await _signInManager.SignInAsync(user, false);
                return Ok(await GerarJwt(user.UserName));
            }

            return BadRequest("Error occurred while registering the user.");
        }

        [HttpPost("register-deliverer")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> RegisterDeliverer(RegisterDelivererViewModel model)
        {
            // Check if CNPJ is unique
            var existingCnpj = _context.Deliverers.FirstOrDefault(m => m.CNPJ == model.CNPJ);
            if (existingCnpj != null)
            {
                return BadRequest("CNPJ already exists.");
            }
            else
            {
                // Check if CNPJ is valid
                if (!IsCnpj(model.CNPJ))
                    return BadRequest("Invalid CNPJ.");
            }

            // Check if Driver License Number is unique
            var existingLicenseNumberUser = _context.Deliverers.FirstOrDefault(m => m.DriverLicenseNumber == model.DriverLicenseNumber);
            if (existingLicenseNumberUser != null)
            {
                return BadRequest("Driver license number already exists.");
            }

            // Create deliverer object
            var deliverer = new Deliverer
            {
                UserName = model.Username,
                Email = model.Email,
                EmailConfirmed = true,
                Name = model.Name,
                CNPJ = model.CNPJ,
                BirthDate = model.BirthDate,
                DriverLicenseNumber = model.DriverLicenseNumber,
                DriverLicenseType = model.DriverLicenseType,
            };

            // Create deliverer
            var result = await _userManager.CreateAsync(deliverer, model.Password);
            if (!result.Succeeded)
            {
                return BadRequest("Failed to create deliverer.");
            }

            if (result.Succeeded)
            {
                if (!await _roleManager.RoleExistsAsync("Deliverer"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("Deliverer"));
                }

                await _userManager.AddToRoleAsync(deliverer, "Deliverer");

                await _signInManager.SignInAsync(deliverer, false);
                return Ok(await GerarJwt(deliverer.UserName));
            }

            return BadRequest("Error occurred while registering the user.");
        }

        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> Login(LoginUserViewModel loginUser)
        {
            var result = await _signInManager.PasswordSignInAsync(loginUser.Username, loginUser.Password, false, true);

            if (result.Succeeded)
            {
                return Ok(await GerarJwt(loginUser.Username));
            }

            return BadRequest("Incorrect user or password.");
        }

        [Authorize(Roles = "Deliverer")]
        [HttpPost("deliverers/upload-photo")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UploadDriverLicencePhoto(IFormFile file)
        {
            var username = _userManager.GetUserName(User);
            var user = await _userManager.FindByNameAsync(username);

            if (user == null)
            {
                return NotFound("User not found.");
            }
            // Check if the deliverer exists
            var deliverer = await _context.Deliverers.FindAsync(user.Id);
            if (deliverer == null)
            {
                return NotFound("Deliverer not found.");
            }

            // Validate file format
            if (file == null || (file.ContentType != "image/png" && file.ContentType != "image/bmp"))
            {
                return BadRequest("Invalid file format. The driver's license must be a PNG or BMP image.");
            }

            // Store the file in local disk
            var filePath = await _storageService.UploadPhoto(file);

            // Update the deliverer's record to include the reference to the driver's license photo file
            deliverer.DriverLicensePhotoPath = filePath;
            _context.SaveChanges();

            return Ok("Driver's license photo uploaded successfully.");
        }

        private async Task<string> GerarJwt(string username)
        {
            var user = await _userManager.FindByNameAsync(username);
            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName)
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);

            var token = tokenHandler.CreateToken(new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                Expires = DateTime.UtcNow.AddHours(_jwtSettings.ExpirationHours),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            });

            var encodedToken = tokenHandler.WriteToken(token);

            return encodedToken;
        }


        private static bool IsCnpj(string cnpj)
        {
            cnpj = Regex.Replace(cnpj, "[^0-9]", "");

            if (cnpj.Length != 14)
                return false;

            int[] multiplier1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
            int[] multiplier2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

            string tempCnpj = cnpj.Substring(0, 12);
            int sum = 0;

            for (int i = 0; i < 12; i++)
                sum += int.Parse(tempCnpj[i].ToString()) * multiplier1[i];

            int remainder = (sum % 11);
            if (remainder < 2)
                remainder = 0;
            else
                remainder = 11 - remainder;

            string digit = remainder.ToString();
            tempCnpj += digit;
            sum = 0;
            for (int i = 0; i < 13; i++)
                sum += int.Parse(tempCnpj[i].ToString()) * multiplier2[i];

            remainder = (sum % 11);
            if (remainder < 2)
                remainder = 0;
            else
                remainder = 11 - remainder;

            digit += remainder.ToString();

            return cnpj.EndsWith(digit);
        }

    }
}
