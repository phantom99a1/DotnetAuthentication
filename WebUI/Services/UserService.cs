using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using WebUI.Domain.Entities;
using WebUI.Domain.Request;
using WebUI.Domain.Response;
using WebUI.Interface;

namespace WebUI.Services
{
    public class UserService(
        ITokenService tokenService,
        ICurrentUserService currentUserService,
        UserManager<ApplicationUser> userManager,
        IMapper mapper,
        ILogger<UserService> logger) : IUserService
    {
        /// <summary>
        /// Deletes a user.
        /// </summary>
        /// <param name="id">The ID of the user to be deleted.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="Exception">Thrown when the user is not found.</exception>
        public async Task DeleteAsync(Guid id)
        {
            var user = await userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                logger.LogError("User not found");
                throw new Exception("User not found");
            }
            await userManager.DeleteAsync(user);
        }

        /// <summary>
        /// Gets a user by ID.
        /// </summary>
        /// <param name="id">The ID of the user.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the user response.</returns>
        /// <exception cref="Exception">Thrown when the user is not found.</exception>
        public async Task<UserResponse> GetByIdAsync(Guid id)
        {
            logger.LogInformation("Getting user by id");
            var user = await userManager.FindByIdAsync(id.ToString());
            if(user == null)
            {
                logger.LogError("User not found");
                throw new Exception("User not found");
            }
            logger.LogInformation("User found");
            var response = mapper.Map<UserResponse>(user);
            return response;
        }

        /// <summary>
        /// Gets the current user.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the current user response.</returns>
        /// <exception cref="Exception">Thrown when the user is not found.</exception>
        public async Task<CurrentUserResponse> GetCurrentUserAsync()
        {
            var user = await userManager.FindByIdAsync(currentUserService.GetUserId() ?? "");
            if (user == null)
            {
                logger.LogError("User not found");
                throw new Exception("User not found");
            }

            var response = mapper.Map<CurrentUserResponse>(user);
            return response;
        }

        /// <summary>
        /// Login a user
        /// </summary>
        /// <param name="request">The user login request.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the user response.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the login request is null.</exception>
        /// <exception cref="Exception">Thrown when the email or password is invalid or user update fails.</exception>
        public async Task<UserResponse> LoginAsync(UserLoginRequest request)
        {
            if (request == null)
            {
                logger.LogError("Login request is null");
                throw new ArgumentNullException(nameof(request));
            }

            var user = await userManager.FindByEmailAsync(request.Email);
            if (user == null || !await userManager.CheckPasswordAsync(user, request.Password))
            {
                logger.LogError("Invalid email or password");
                throw new Exception("Invalid email or password");
            }

            // Generate access token
            var token = await tokenService.GenerateToken(user);

            // Generate refresh token
            var refreshToken = tokenService.GenerateRefreshToken();

            // Hash the refresh token and store it in the database or override the existing refresh token
            var refreshTokenHash = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
            user.RefreshToken = Convert.ToBase64String(refreshTokenHash);
            user.RefreshTokenExpireTime = DateTime.Now.AddDays(2);

            user.CreateDateTime = DateTime.Now;
            user.LastModifiedDateTime = DateTime.Now;

            // Update user information in database
            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                logger.LogError("Failed to update user: {errors}", errors);
                throw new Exception($"Failed to update user: {errors}");
            }

            var userResponse = mapper.Map<ApplicationUser, UserResponse>(user);
            userResponse.AccessToken = token;
            userResponse.RefreshToken = refreshToken;

            return userResponse;
        }

        /// <summary>
        /// Refreshes the access token using the refresh token.
        /// </summary>
        /// <param name="request">The refresh token request.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the current user response.</returns>
        /// <exception cref="Exception">Thrown when the refresh token is invalid or expired.</exception>
        public async Task<CurrentUserResponse> RefreshTokenAsync(RefreshTokenRequest request)
        {
            logger.LogInformation("RefreshToken");

            // Hash the incoming RefreshToken and compare it with the one stored in the database
            var refreshTokenHash = SHA256.HashData(Encoding.UTF8.GetBytes(request.RefreshToken!));
            var hashedRefreshToken = Convert.ToBase64String(refreshTokenHash);

            // Find user based on the refresh token
            var user = await userManager.Users.FirstOrDefaultAsync(u => u.RefreshToken == hashedRefreshToken);
            if (user == null)
            {
                logger.LogError("Invalid refresh token");
                throw new Exception("Invalid refresh token");
            }

            // Validate the refresh token expiry time
            if (user.RefreshTokenExpireTime < DateTime.Now)
            {
                logger.LogWarning("Refresh token expired for user ID: {UserId}", user.Id);
                throw new Exception("Refresh token expired");
            }

            // Generate a new access token
            var newAccessToken = await tokenService.GenerateToken(user);
            logger.LogInformation("Access token generated successfully");
            var currentUserResponse = mapper.Map<CurrentUserResponse>(user);
            currentUserResponse.AccessToken = newAccessToken;
            return currentUserResponse;
        }

        /// <summary>
        /// Register a new user
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<UserResponse> RegisterAsync(UserRegisterRequest request)
        {
            logger.LogInformation("Register User");
            var existingUser = await userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                logger.LogError("Email already exists!");
                throw new Exception("Email alrerady exists!");
            }

            var newUser = mapper.Map<ApplicationUser>(request);

            // Generate a unique username
            newUser.UserName = GenerateUserName(request.FirstName, request.LastName);
            var result = await userManager.CreateAsync(newUser, request.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                logger.LogError("Failed to create user: {errors}", errors);
                throw new Exception($"Failed to create user: {errors}");
            }
            logger.LogInformation("User created successfully");
            await tokenService.GenerateToken(newUser);
            newUser.CreateDateTime = DateTime.Now;
            newUser.LastModifiedDateTime = DateTime.Now;
            return mapper.Map<UserResponse>(newUser);
        }

        /// <summary>
        /// Generates a unique username by concatenating the first name and last name.
        /// </summary>
        /// <param name="firstName"></param>
        /// <param name="lastName"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private string? GenerateUserName(string firstName, string lastName)
        {
            var baseUsername = $"{firstName}{lastName}".ToLower();

            // Check if the username already exists
            var username = baseUsername;
            var count = 1;
            while (userManager.Users.Any(u => u.UserName == username))
            {
                username = $"{baseUsername}{count}";
                count++;
            }
            return username;
        }

        /// <summary>
        /// Revokes the refresh token.
        /// </summary>
        /// <param name="refreshTokenRemoveRequest">The refresh token request to be revoked.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the revoke refresh token response.</returns>
        /// <exception cref="Exception">Thrown when the refresh token is invalid or expired.</exception>
        public async Task<RevokeRefreshTokenResponse> RevokeRefreshToken(RefreshTokenRequest refreshTokenRemoveRequest)
        {
            logger.LogInformation("Revoking refresh token");

            try
            {
                // Hash the refresh token
                var refreshTokenHash = SHA256.HashData(Encoding.UTF8.GetBytes(refreshTokenRemoveRequest.RefreshToken!));
                var hashedRefreshToken = Convert.ToBase64String(refreshTokenHash);

                // Find the user based on the refresh token
                var user = await userManager.Users.FirstOrDefaultAsync(u => u.RefreshToken == hashedRefreshToken);
                if (user == null)
                {
                    logger.LogError("Invalid refresh token");
                    throw new Exception("Invalid refresh token");
                }

                // Validate the refresh token expiry time
                if (user.RefreshTokenExpireTime < DateTime.Now)
                {
                    logger.LogWarning("Refresh token expired for user ID: {UserId}", user.Id);
                    throw new Exception("Refresh token expired");
                }

                // Remove the refresh token
                user.RefreshToken = null;
                user.RefreshTokenExpireTime = null;

                // Update user information in database
                var result = await userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    logger.LogError("Failed to update user");
                    return new RevokeRefreshTokenResponse
                    {
                        Message = "Failed to revoke refresh token"
                    };
                }
                logger.LogInformation("Refresh token revoked successfully");
                return new RevokeRefreshTokenResponse
                {
                    Message = "Refresh token revoked successfully"
                };
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to revoke refresh token: {ex}", ex.Message);
                throw new Exception("Failed to revoke refresh token");
            }
        }

        /// <summary>
        /// Updates a user.
        /// </summary>
        /// <param name="id">The ID of the user to be updated.</param>
        /// <param name="request">The update user request.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the user response.</returns>
        /// <exception cref="Exception">Thrown when the user is not found.</exception>
        public async Task<UserResponse> UpdateAsync(Guid id, UpdateUserRequest request)
        {
            var user = await userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                logger.LogError("User not found");
                throw new Exception("User not found");
            }

            user.LastModifiedDateTime = DateTime.Now;
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.Email = request.Email;
            user.Gender = request.Gender;

            await userManager.UpdateAsync(user);
            var response = mapper.Map<UserResponse>(user);
            return response;
        }
    }
}
