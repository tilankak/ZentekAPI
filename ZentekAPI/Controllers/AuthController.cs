using Microsoft.AspNetCore.Mvc;
using ZentekAPI.Auth;

namespace ZentekAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ITokenService _tokenService;

    // In a real system this would be a user store / identity provider
    private static readonly Dictionary<string, (string Password, string Role)> _users = new()
    {
        { "admin", ("admin123", "Admin") },
        { "user",  ("user123",  "User")  }
    };

    public AuthController(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Login([FromBody] LoginRequest request)
    {
       
        if (!_users.TryGetValue(request.Username, out var user) || user.Password != request.Password)
            return Unauthorized(new { Message = "Invalid credentials" });

        var token = _tokenService.GenerateToken(request.Username, user.Role);
        return Ok(new { Token = token, ExpiresIn = 3600 });
    }
}

public record LoginRequest(string Username, string Password);
