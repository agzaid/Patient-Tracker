using Microsoft.AspNetCore.Mvc;
using PatientTracker.Application.DTOs;
using PatientTracker.Application.Services;
using FluentValidation;

namespace PatientTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<RefreshTokenRequest> _refreshTokenValidator;

    public AuthController(
        IAuthService authService,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator,
        IValidator<RefreshTokenRequest> refreshTokenValidator)
    {
        _authService = authService;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _refreshTokenValidator = refreshTokenValidator;
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    /// <param name="request">Registration details</param>
    /// <returns>Authentication response with tokens</returns>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        var validationResult = await _registerValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { errors = validationResult.Errors });
        }

        try
        {
            var response = await _authService.RegisterAsync(request);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred during registration" });
        }
    }

    /// <summary>
    /// Login user
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>Authentication response with tokens</returns>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var validationResult = await _loginValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { errors = validationResult.Errors });
        }

        try
        {
            var response = await _authService.LoginAsync(request);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred during login" });
        }
    }

    /// <summary>
    /// Refresh access token
    /// </summary>
    /// <param name="request">Refresh token request</param>
    /// <returns>New authentication response</returns>
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var validationResult = await _refreshTokenValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { errors = validationResult.Errors });
        }

        try
        {
            var response = await _authService.RefreshTokenAsync(request);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred during token refresh" });
        }
    }

    /// <summary>
    /// Logout user
    /// </summary>
    /// <param name="request">Refresh token to revoke</param>
    /// <returns>Logout result</returns>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        try
        {
            await _authService.LogoutAsync(request.RefreshToken);
            return Ok(new { message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred during logout" });
        }
    }
}
