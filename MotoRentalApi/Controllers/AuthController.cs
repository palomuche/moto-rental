using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using MotoRentalApi.ViewModels;
using MotoRentalApi.Entities;

namespace MotoRentalApi.Controllers
{
    [ApiController]
    [Route("api/account")]
    public class AuthController : ControllerBase
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly JwtSettings _jwtSettings;

        public AuthController(SignInManager<IdentityUser> signInManager,
                              UserManager<IdentityUser> userManager,
                              RoleManager<IdentityRole> roleManager,
                              IOptions<JwtSettings> jwtSettings)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _jwtSettings = jwtSettings.Value;
        }

        [HttpPost("register-admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> Register(RegisterUserViewModel registerUser)
        {
            var user = new IdentityUser
            {
                UserName = registerUser.Email,
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
                return Ok(await GerarJwt(user.Email));
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
            var existingCnpjUser = await _userManager.FindByNameAsync(model.CNPJ);
            if (existingCnpjUser != null)
            {
                return BadRequest("CNPJ already exists.");
            }

            // Check if Driver License Number is unique
            var existingLicenseNumberUser = await _userManager.FindByEmailAsync(model.DriverLicenseNumber);
            if (existingLicenseNumberUser != null)
            {
                return BadRequest("Driver license number already exists.");
            }

            // Create deliverer object
            var deliverer = new Deliverer
            {
                UserName = model.Email,
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
                return Ok(await GerarJwt(deliverer.Email));
            }

            return BadRequest("Error occurred while registering the user.");
        }

        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> Login(LoginUserViewModel loginUser)
        {
            var result = await _signInManager.PasswordSignInAsync(loginUser.Email, loginUser.Password, false, true);

            if (result.Succeeded)
            {
                return Ok(await GerarJwt(loginUser.Email));
            }

            return BadRequest("Incorrect user or password.");
        }

        private async Task<string> GerarJwt(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
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
    }
}
