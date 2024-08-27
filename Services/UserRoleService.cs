using Dapper;
using System.Data;
using Npgsql;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;



public class UserRoleService
{
    private readonly IConfiguration _configuration;

    public UserRoleService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GetRoleFromToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "role");
            return roleClaim?.Value ?? "RoleNotFound";
        }
        catch (Exception)
        {
            return "InvalidToken";
        }
    }

    public async Task<string> GetRoleByUsernameAsync(string username)
    {
        var connectionString = _configuration.GetConnectionString("PostgreSql");
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        var query = "SELECT Role FROM Users WHERE Username = @Username";
        var role = await connection.QuerySingleOrDefaultAsync<string>(query, new { Username = username });
        
        return role ?? "UserNotFound";
    }
}
