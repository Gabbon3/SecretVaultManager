using Microsoft.AspNetCore.Mvc;
using SecretVaultManager.DTOs.Secret;
using SecretVaultManager.Services;

namespace SecretVaultManager.Controllers
{
    /// <summary>
    /// API controller for managing secrets
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    public class SecretsController : ControllerBase
    {
        private readonly ISecretService _secretService;
        private readonly ILogger<SecretsController> _logger;

        public SecretsController(
            ISecretService secretService,
            ILogger<SecretsController> logger)
        {
            _secretService = secretService ?? throw new ArgumentNullException(nameof(secretService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates a new secret
        /// </summary>
        /// <param name="dto">Secret creation data</param>
        /// <returns>The created secret</returns>
        /// <response code="201">Returns the newly created secret</response>
        /// <response code="400">If the request data is invalid</response>
        /// <response code="409">If a secret with the same name already exists</response>
        [HttpPost]
        [ProducesResponseType(typeof(SecretDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateSecret([FromBody] CreateSecretDto dto)
        {
            try
            {
                _logger.LogInformation("Creating new secret with name {SecretName}", dto.Name);
                var created = await _secretService.CreateSecretAsync(dto);

                _logger.LogInformation("Secret {SecretId} created successfully", created.Id);
                return CreatedAtAction(
                    nameof(GetSecretById),
                    new { id = created.Id },
                    created);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Conflict creating secret: {Message}", ex.Message);
                return Conflict(new ProblemDetails
                {
                    Title = "Conflict",
                    Detail = ex.Message,
                    Status = StatusCodes.Status409Conflict
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Bad request creating secret: {Message}", ex.Message);
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid request",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating secret");
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new ProblemDetails
                    {
                        Title = "Server Error",
                        Detail = "An unexpected error occurred while creating the secret",
                        Status = StatusCodes.Status500InternalServerError
                    });
            }
        }

        /// <summary>
        /// Updates a secret
        /// </summary>
        /// <param name="id">ID of the secret to update</param>
        /// <param name="dto">Update data</param>
        /// <returns>The updated secret</returns>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(SecretDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateSecret(Guid id, [FromBody] UpdateSecretDto dto)
        {
            try
            {
                _logger.LogInformation("Updating secret with ID {SecretId}", id);
                var updated = await _secretService.UpdateSecretAsync(id, dto);
                return Ok(updated);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Secret not found for update");
                return NotFound(new ProblemDetails
                {
                    Title = "Not Found",
                    Detail = ex.Message,
                    Status = StatusCodes.Status404NotFound
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Conflict updating secret");
                return Conflict(new ProblemDetails
                {
                    Title = "Conflict",
                    Detail = ex.Message,
                    Status = StatusCodes.Status409Conflict
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Bad request updating secret");
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid request",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating secret");
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new ProblemDetails
                    {
                        Title = "Server Error",
                        Detail = "An unexpected error occurred while updating the secret",
                        Status = StatusCodes.Status500InternalServerError
                    });
            }
        }

        /// <summary>
        /// Gets a secret by its ID (encrypted)
        /// </summary>
        /// <param name="id">The secret ID</param>
        /// <returns>The requested secret</returns>
        /// <response code="200">Returns the requested secret</response>
        /// <response code="404">If the secret is not found</response>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(SecretDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSecretById(Guid id)
        {
            _logger.LogDebug("Fetching secret with ID {SecretId}", id);
            var secret = await _secretService.GetSecretByIdAsync(id);

            if (secret == null)
            {
                _logger.LogWarning("Secret {SecretId} not found", id);
                return NotFound(new ProblemDetails
                {
                    Title = "Not Found",
                    Detail = $"Secret with ID {id} not found",
                    Status = StatusCodes.Status404NotFound
                });
            }

            return Ok(secret);
        }

        /// <summary>
        /// Gets a decrypted secret by its ID
        /// </summary>
        /// <param name="id">The secret ID</param>
        /// <returns>The decrypted secret</returns>
        /// <response code="200">Returns the decrypted secret</response>
        /// <response code="404">If the secret is not found</response>
        /// <response code="500">If decryption fails</response>
        [HttpGet("{id:guid}/decrypted")]
        [ProducesResponseType(typeof(DecryptedSecretDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDecryptedSecretById(Guid id)
        {
            try
            {
                _logger.LogDebug("Fetching and decrypting secret with ID {SecretId}", id);
                var secret = await _secretService.GetDecryptedSecretByIdAsync(id);

                if (secret == null)
                {
                    _logger.LogWarning("Secret {SecretId} not found for decryption", id);
                    return NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = $"Secret with ID {id} not found",
                        Status = StatusCodes.Status404NotFound
                    });
                }

                return Ok(secret);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Decryption failed for secret {SecretId}", id);
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new ProblemDetails
                    {
                        Title = "Decryption Failed",
                        Detail = "Failed to decrypt the secret",
                        Status = StatusCodes.Status500InternalServerError
                    });
            }
        }

        /// <summary>
        /// Gets a secret by its name (encrypted)
        /// </summary>
        /// <param name="name">The secret name</param>
        /// <returns>The requested secret</returns>
        /// <response code="200">Returns the requested secret</response>
        /// <response code="404">If the secret is not found</response>
        [HttpGet("by-name/{name}")]
        [ProducesResponseType(typeof(SecretDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSecretByName(string name)
        {
            _logger.LogDebug("Fetching secret with name {SecretName}", name);
            var secret = await _secretService.GetSecretByNameAsync(name);

            if (secret == null)
            {
                _logger.LogWarning("Secret with name {SecretName} not found", name);
                return NotFound(new ProblemDetails
                {
                    Title = "Not Found",
                    Detail = $"Secret with name '{name}' not found",
                    Status = StatusCodes.Status404NotFound
                });
            }

            return Ok(secret);
        }

        /// <summary>
        /// Gets all secrets (encrypted)
        /// </summary>
        /// <returns>List of all secrets</returns>
        /// <response code="200">Returns all secrets</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<SecretDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllSecrets()
        {
            _logger.LogDebug("Fetching all secrets");
            var secrets = await _secretService.GetAllSecretsAsync();
            return Ok(secrets);
        }

        /// <summary>
    /// Deletes a secret
    /// </summary>
    /// <param name="id">ID of the secret to delete</param>
    /// <returns>No content if successful</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSecret(Guid id)
    {
        var result = await _secretService.DeleteSecretAsync(id);
        if (!result)
        {
            _logger.LogWarning("Secret {SecretId} not found for deletion", id);
            return NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Detail = $"Secret with ID {id} not found",
                Status = StatusCodes.Status404NotFound
            });
        }

        _logger.LogInformation("Successfully deleted secret {SecretId}", id);
        return NoContent();
    }
    }
}