using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using SecretVaultManager.DTOs.Auth;
using SecretVaultManager.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SecretVaultManager.Services
{
    /// <summary>
    /// Service for handling user authentication and JWT generation
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Registers a new user in the system
        /// </summary>
        /// <param name="email">User email address</param>
        /// <param name="password">User password</param>
        /// <returns>Authentication response with JWT token</returns>
        Task<SignUpResponse> SignUpAsync(string email, string password);

        /// <summary>
        /// Authenticates an existing user
        /// </summary>
        /// <param name="email">User email address</param>
        /// <param name="password">User password</param>
        /// <returns>Authentication response with JWT token</returns>
        Task<SignInResponse> SignInAsync(string email, string password);
    }

    public class AuthService : IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            UserManager<User> userManager,
            IConfiguration configuration,
            ILogger<AuthService> logger)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<SignUpResponse> SignUpAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be empty", nameof(email));

            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be empty", nameof(password));

            _logger.LogInformation("Attempting to register new user with email {Email}", email);

            try
            {
                var user = new User { UserName = email, Email = email };
                var result = await _userManager.CreateAsync(user, password);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogWarning("User registration failed for {Email}. Errors: {Errors}", email, errors);
                    throw new InvalidOperationException($"Registration failed: {errors}");
                }

                _logger.LogInformation("Successfully registered user with ID {UserId}", user.Id);

                return await GenerateJwtToken(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration for {Email}", email);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<SignInResponse> SignInAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be empty", nameof(email));

            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be empty", nameof(password));

            _logger.LogInformation("Attempting login for user {Email}", email);

            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    _logger.LogWarning("Login failed - user {Email} not found", email);
                    throw new InvalidOperationException("Invalid credentials");
                }

                if (!await _userManager.CheckPasswordAsync(user, password))
                {
                    _logger.LogWarning("Login failed - invalid password for user {Email}", email);
                    throw new InvalidOperationException("Invalid credentials");
                }

                _logger.LogInformation("Successful login for user {Email}", email);
                return await GenerateJwtToken(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for {Email}", email);
                throw;
            }
        }

        /// <summary>
        /// Generates a JWT token for the specified user
        /// </summary>
        /// <param name="user">The user to generate token for</param>
        /// <returns>Authentication response with token</returns>
        private async Task<SignInResponse> GenerateJwtToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // Add user roles if needed
            var roles = await _userManager.GetRolesAsync(user);
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration["JWT:Secret"] ?? throw new InvalidOperationException("JWT Secret not configured")));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
                Issuer = _configuration["JWT:ValidIssuer"],
                Audience = _configuration["JWT:ValidAudience"]
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return new SignInResponse
            {
                Token = tokenHandler.WriteToken(token),
                Email = user.Email,
                ExpiresAt = tokenDescriptor.Expires ?? DateTime.UtcNow.AddHours(1)
            };
        }
    }
}
