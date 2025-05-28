using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace RProg.FluxoCaixa.Proxy.Authentication;

public static class JwtAuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("Jwt");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey não configurada");
        var issuer = jwtSettings["Issuer"] ?? "RProg.FluxoCaixa";
        var audience = jwtSettings["Audience"] ?? "RProg.FluxoCaixa.API";

        var key = Encoding.ASCII.GetBytes(secretKey);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false; // Permite HTTP em desenvolvimento
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero, // Remove delay de clock skew
                NameClaimType = ClaimTypes.Name,
                RoleClaimType = ClaimTypes.Role
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogError("Authentication failed: {Error}", context.Exception.Message);
                      if (context.Exception is SecurityTokenExpiredException)
                    {
                        context.Response.Headers["Token-Expired"] = "true";
                    }
                    
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogWarning("JWT Challenge: {Error}", context.Error);
                    
                    context.HandleResponse();
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "application/json";
                    
                    var result = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        error = "Unauthorized",
                        message = "Token de acesso inválido ou expirado"
                    });
                    
                    return context.Response.WriteAsync(result);
                },
                OnTokenValidated = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    var userId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    logger.LogInformation("Token validated for user: {UserId}", userId);
                    
                    // Adicionar claims customizados se necessário
                    var identity = context.Principal?.Identity as ClaimsIdentity;
                    if (identity != null)
                    {
                        // Adicionar claim de timestamp de validação
                        identity.AddClaim(new Claim("token_validated_at", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()));
                    }
                    
                    return Task.CompletedTask;
                }
            };
        });

        // Adicionar autorização
        services.AddAuthorization(options =>
        {
            // Política padrão que requer autenticação
            options.AddPolicy("RequireAuthentication", policy =>
            {
                policy.RequireAuthenticatedUser();
            });

            // Política para administradores
            options.AddPolicy("RequireAdmin", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole("Admin");
            });

            // Política para APIs específicas
            options.AddPolicy("ApiAccess", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim("scope", "api:read", "api:write");
            });

            // Política para lançamentos
            options.AddPolicy("LancamentosAccess", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim("scope", "lancamentos:read", "lancamentos:write");
            });

            // Política para consolidado
            options.AddPolicy("ConsolidadoAccess", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim("scope", "consolidado:read");
            });
        });

        return services;
    }
}

public class JwtTokenService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtTokenService> _logger;

    public JwtTokenService(IConfiguration configuration, ILogger<JwtTokenService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public string GenerateToken(string userId, string email, IList<string> roles, IList<string> scopes)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey não configurada");
        var issuer = jwtSettings["Issuer"] ?? "RProg.FluxoCaixa";
        var audience = jwtSettings["Audience"] ?? "RProg.FluxoCaixa.API";
        var expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"] ?? "60");

        var key = Encoding.ASCII.GetBytes(secretKey);
        var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, email),
            new("jti", Guid.NewGuid().ToString()),
            new("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        // Adicionar roles
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Adicionar scopes
        foreach (var scope in scopes)
        {
            claims.Add(new Claim("scope", scope));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        _logger.LogInformation("JWT token generated for user {UserId}", userId);

        return tokenString;
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey não configurada");
            var issuer = jwtSettings["Issuer"] ?? "RProg.FluxoCaixa";
            var audience = jwtSettings["Audience"] ?? "RProg.FluxoCaixa.API";

            var key = Encoding.ASCII.GetBytes(secretKey);
            var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            return principal;
        }        catch (Exception ex)
        {
            _logger.LogWarning("Token validation failed: {Error}", ex.Message);
            return null;
        }
    }
}
