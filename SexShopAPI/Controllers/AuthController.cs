using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SexShopAPI.Models;
using SexShopAPI.Models.DTOs;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SexShopAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;

    public AuthController(UserManager<ApplicationUser> userManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _configuration = configuration;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        try
        {
            if (model == null || string.IsNullOrEmpty(model.Username) || string.IsNullOrEmpty(model.Password))
                return BadRequest(new { message = "Username and password are required." });

            var user = await _userManager.FindByNameAsync(model.Username);
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
                return Unauthorized(new { message = "Invalid username or password." });

            var configuredKey = _configuration["Jwt:Key"];
            var jwtKey = (string.IsNullOrEmpty(configuredKey) || configuredKey.Length < 32)
                ? "SexShop@2025#SuperSecretKey$256BitMinimumLengthRequired!"
                : configuredKey;

            var userRoles = await _userManager.GetRolesAsync(user);

            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName!),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            foreach (var role in userRoles)
                authClaims.Add(new Claim(ClaimTypes.Role, role));

            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                expires: DateTime.UtcNow.AddHours(3),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                expiration = token.ValidTo
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Login error: " + ex.Message });
        }
    }

    [HttpGet("diag")]
    public IActionResult Diag()
    {
        var configuredKey = _configuration["Jwt:Key"];
        var jwtKey = (string.IsNullOrEmpty(configuredKey) || configuredKey.Length < 32)
            ? "SexShop@2025#SuperSecretKey$256BitMinimumLengthRequired!"
            : configuredKey;

        return Ok(new { 
            configuredKeyLength = configuredKey?.Length ?? -1,
            finalKeyLength = jwtKey.Length,
            usingFallback = (string.IsNullOrEmpty(configuredKey) || configuredKey.Length < 32)
        });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterModel model)
    {
        var userExists = await _userManager.FindByNameAsync(model.Username);
        if (userExists != null)
            return StatusCode(StatusCodes.Status500InternalServerError, new { Status = "Error", Message = "User already exists!" });

        ApplicationUser user = new()
        {
            Email = model.Email,
            SecurityStamp = Guid.NewGuid().ToString(),
            UserName = model.Username
        };
        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            return StatusCode(StatusCodes.Status500InternalServerError, new { Status = "Error", Message = "User creation failed!", Errors = errors });
        }

        // Default role: Guest
        await _userManager.AddToRoleAsync(user, "Guest");

        return Ok(new { Status = "Success", Message = "User created successfully!" });
    }

    [HttpPost("register-admin")]
    public async Task<IActionResult> RegisterAdmin([FromBody] RegisterModel model)
    {
        // Ideally this should be protected or removed in production
        var userExists = await _userManager.FindByNameAsync(model.Username);
        if (userExists != null)
            return StatusCode(StatusCodes.Status500InternalServerError, new { Status = "Error", Message = "User already exists!" });

        ApplicationUser user = new()
        {
            Email = model.Email,
            SecurityStamp = Guid.NewGuid().ToString(),
            UserName = model.Username
        };
        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
            return StatusCode(StatusCodes.Status500InternalServerError, new { Status = "Error", Message = "User creation failed! Please check user details and try again." });

        await _userManager.AddToRoleAsync(user, "Admin");

        return Ok(new { Status = "Success", Message = "Admin user created successfully!" });
    }
}
