using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace LYBT.Tests.Desktop.ApiIntegration.Infrastructure;

/// <summary>
/// Factory for generating test data for API integration tests
/// 
/// Provides utilities for creating:
/// - JWT tokens for authentication testing
/// - User objects with various roles
/// - API response objects
/// - Test data collections
/// </summary>
public class TestDataFactory
{
    private const string SecretKey = "your-test-secret-key-at-least-32-characters-long-for-testing";
    private const string Issuer = "LYBT.WebAPI";
    private const string Audience = "LYBT.Desktop";

    private static readonly Random _random = new Random();

    #region JWT Token Generation

    /// <summary>
    /// Generate a valid JWT token for testing
    /// </summary>
    public string GenerateJwtToken(Guid userId, string userName, string role, 
                                  DateTime? expires = null)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(SecretKey);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, userName),
            new Claim(ClaimTypes.Role, role),
            new Claim("user_type", "user")
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expires ?? DateTime.UtcNow.AddHours(8),
            Issuer = Issuer,
            Audience = Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Generate an expired JWT token
    /// </summary>
    public string GenerateExpiredJwtToken(Guid userId, string userName, string role)
    {
        return GenerateJwtToken(userId, userName, role, DateTime.UtcNow.AddMinutes(-30));
    }

    /// <summary>
    /// Generate a JWT token that expires soon (for refresh testing)
    /// </summary>
    public string GenerateExpiringSoonJwtToken(Guid userId, string userName, string role)
    {
        return GenerateJwtToken(userId, userName, role, DateTime.UtcNow.AddMinutes(3));
    }

    /// <summary>
    /// Generate a refresh token
    /// </summary>
    public string GenerateRefreshToken()
    {
        return $"refresh_token_{Guid.NewGuid():N}";
    }

    #endregion

    #region User Data Generation

    /// <summary>
    /// Create a test user with specified parameters
    /// </summary>
    public UserDetailDto CreateUser(
        string username = "testuser",
        UserRole role = UserRole.Doctor,
        Guid? id = null)
    {
        return new UserDetailDto
        {
            Id = id ?? Guid.NewGuid(),
            UserName = username,
            Role = role
        };
    }

    /// <summary>
    /// Create a doctor user
    /// </summary>
    public UserDetailDto CreateDoctor(string username = "doctor")
    {
        return CreateUser(username, UserRole.Doctor);
    }

    /// <summary>
    /// Create an admin user
    /// </summary>
    public UserDetailDto CreateAdmin(string username = "admin")
    {
        return CreateUser(username, UserRole.Admin);
    }

    /// <summary>
    /// Create a super admin user
    /// </summary>
    public UserDetailDto CreateSuperAdmin(string username = "sysadmin")
    {
        return CreateUser(username, UserRole.SuperAdmin);
    }

    #endregion

    #region API Response Generation

    /// <summary>
    /// Create a successful API response
    /// </summary>
    public ApiResponse<T> CreateSuccessResponse<T>(T data, string message = "操作成功")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message
        };
    }

    /// <summary>
    /// Create an error API response
    /// </summary>
    public ApiResponse<T> CreateErrorResponse<T>(string message = "操作失败")
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message
        };
    }

    /// <summary>
    /// Create a login response
    /// </summary>
    public LoginResponse CreateLoginResponse(UserDetailDto user, string? token = null, string? refreshToken = null)
    {
        return new LoginResponse
        {
            Token = token ?? GenerateJwtToken(user.Id, user.UserName, user.Role.ToString()),
            RefreshToken = refreshToken ?? GenerateRefreshToken(),
            User = user,
            ExpiresAt = DateTime.UtcNow.AddHours(8)
        };
    }

    #endregion

    #region Collection Data Generation

    /// <summary>
    /// Create a list of test users
    /// </summary>
    public List<UserDetailDto> CreateUserList(int count = 5)
    {
        var users = new List<UserDetailDto>();
        for (int i = 0; i < count; i++)
        {
            users.Add(CreateUser($"user{i}", (UserRole)_random.Next(0, 3)));
        }
        return users;
    }

    /// <summary>
    /// Create a paged result
    /// </summary>
    public PagedResult<T> CreatePagedResult<T>(IEnumerable<T> items, int totalCount, 
                                              int pageNumber = 1, int pageSize = 10)
    {
        return new PagedResult<T>
        {
            Items = items.ToList(),
            TotalCount = totalCount,
            CurrentPage = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    #endregion

    #region Random Data Generation

    /// <summary>
    /// Generate a random string
    /// </summary>
    public string RandomString(int length = 10)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[_random.Next(s.Length)]).ToArray());
    }

    /// <summary>
    /// Generate a random GUID
    /// </summary>
    public Guid RandomGuid()
    {
        return Guid.NewGuid();
    }

    /// <summary>
    /// Generate a random integer
    /// </summary>
    public int RandomInt(int min = 0, int max = 100)
    {
        return _random.Next(min, max);
    }

    /// <summary>
    /// Generate a random boolean
    /// </summary>
    public bool RandomBool()
    {
        return _random.Next(2) == 1;
    }

    #endregion

    #region Specialized Test Data

    /// <summary>
    /// Create test data for authentication scenarios
    /// </summary>
    public class AuthTestData
    {
        public UserDetailDto ValidUser { get; }
        public UserDetailDto InvalidUser { get; }
        public string ValidToken { get; }
        public string ExpiredToken { get; }
        public string ExpiringSoonToken { get; }
        public string RefreshToken { get; }
        public LoginResponse LoginResponse { get; }

        public AuthTestData(TestDataFactory factory)
        {
            ValidUser = factory.CreateDoctor("validuser");
            InvalidUser = factory.CreateUser("invaliduser", UserRole.Doctor, Guid.NewGuid());
            
            ValidToken = factory.GenerateJwtToken(ValidUser.Id, ValidUser.UserName, ValidUser.Role.ToString());
            ExpiredToken = factory.GenerateExpiredJwtToken(ValidUser.Id, ValidUser.UserName, ValidUser.Role.ToString());
            ExpiringSoonToken = factory.GenerateExpiringSoonJwtToken(ValidUser.Id, ValidUser.UserName, ValidUser.Role.ToString());
            RefreshToken = factory.GenerateRefreshToken();
            
            LoginResponse = factory.CreateLoginResponse(ValidUser, ValidToken, RefreshToken);
        }
    }

    /// <summary>
    /// Get authentication test data
    /// </summary>
    public AuthTestData Auth => new AuthTestData(this);

    #endregion
}
