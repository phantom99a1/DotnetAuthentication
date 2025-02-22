using AutoMapper;
using Microsoft.AspNetCore.Identity;
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
        public Task DeleteAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task<UserResponse> GetByIdAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task<CurrentUserResponse> GetCurrentUserAsync()
        {
            throw new NotImplementedException();
        }

        public Task<UserResponse> LoginAsync(UserLoginRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<CurrentUserResponse> RefreshTokenAsync(RefreshTokenRequest request)
        {
            throw new NotImplementedException();
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
            var existingUser = userManager.FindByEmailAsync(request.Email);
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

        public Task<RevokeRefreshTokenResponse> RevokeRefreshToken(RefreshTokenRequest refreshTokenRemoveRequest)
        {
            throw new NotImplementedException();
        }

        public Task<UserResponse> UpdateAsync(Guid id, UpdateUserRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
