﻿using Microsoft.AspNetCore.Identity;
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
        private readonly ILogger<AccountController> _logger;

        public AccountController(ApiDbContext context,
                              SignInManager<IdentityUser> signInManager,
                              UserManager<IdentityUser> userManager,
                              RoleManager<IdentityRole> roleManager,
                              IOptions<JwtSettings> jwtSettings,
                              ILogger<AccountController> logger)
        {
            _context = context;
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _jwtSettings = jwtSettings.Value;
            _logger = logger;
        }

        [HttpPost("register-admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> Register(RegisterUserViewModel registerUser)
        {
            try
            {
                // Create a new user with the provided data
                var user = new IdentityUser
                {
                    UserName = registerUser.Username,
                    Email = registerUser.Email,
                    EmailConfirmed = true
                };

                // Try to create the user in the database
                var result = await _userManager.CreateAsync(user, registerUser.Password);

                if (result.Succeeded)
                {
                    // If the user was successfully created, check if the "Admin" role exists
                    if (!await _roleManager.RoleExistsAsync("Admin"))
                    {
                        // If the role doesn't exist, create it
                        await _roleManager.CreateAsync(new IdentityRole("Admin"));
                    }

                    // Add the user to the "Admin" role
                    await _userManager.AddToRoleAsync(user, "Admin");

                    // Sign in with the newly created user
                    await _signInManager.SignInAsync(user, false);

                    // Return a JWT token for the user
                    return Ok(await GenerateJwt(user.UserName));
                }
                else
                {
                    // If there was a failure in creating the user, return a BadRequest response with error details
                    return BadRequest("Failed to create user. Error: " + result.Errors?.FirstOrDefault()?.Description);
                }
            }
            catch (Exception ex)
            {
                // In case of unhandled exception, log the error and return a StatusCode 500 (Internal Server Error) response
                _logger.LogError(ex, "An exception occurred while trying to register a new user.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An internal error occurred while processing the request.");
            }
        }

        [HttpPost("register-deliverer")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RegisterDeliverer(RegisterDelivererViewModel model)
        {
            // Clean CNPJ by removing non-numeric characters
            model.CNPJ = Regex.Replace(model.CNPJ, "[^0-9]", "");

            // Check if CNPJ is unique
            var existingCnpj = _context.Deliverers.FirstOrDefault(m => m.CNPJ == model.CNPJ);
            if (existingCnpj != null)
            {
                return BadRequest("CNPJ already exists.");
            }

            // Validate CNPJ format
            if (!IsCnpj(model.CNPJ))
            {
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

            // Attempt to create the deliverer account
            var result = await _userManager.CreateAsync(deliverer, model.Password);
            if (result.Succeeded)
            {
                // Ensure the "Deliverer" role exists
                if (!await _roleManager.RoleExistsAsync("Deliverer"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("Deliverer"));
                }

                // Assign the "Deliverer" role to the newly created user
                await _userManager.AddToRoleAsync(deliverer, "Deliverer");

                // Sign in the deliverer
                await _signInManager.SignInAsync(deliverer, false);

                // Generate and return a JWT token
                return Ok(await GenerateJwt(deliverer.UserName));
            }
            else
            {
                // If creation failed, return an error with the first issue encountered
                return BadRequest("Failed to create deliverer. Error: " + result.Errors?.FirstOrDefault()?.Description);
            }
        }

        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> Login(LoginUserViewModel loginUser)
        {
            // Attempt to sign in the user with the provided credentials
            var result = await _signInManager.PasswordSignInAsync(loginUser.Username, loginUser.Password, false, true);

            // If sign-in succeeded, return a JWT token
            if (result.Succeeded)
            {
                return Ok(await GenerateJwt(loginUser.Username));
            }
            else
            {
                // If sign-in failed, return a BadRequest response indicating incorrect user or password
                return BadRequest("Incorrect user or password.");
            }

        }

        /// <summary>
        /// Generates a JSON Web Token (JWT) for a specified username.
        /// </summary>
        private async Task<string> GenerateJwt(string username)
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

        /// <summary>
        /// Checks whether a given string represents a valid CNPJ
        /// </summary>
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
