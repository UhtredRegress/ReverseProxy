using System.Text;
using System.Threading.RateLimiting;
using AuthorizeServer.Service;
using AuthorizeServer.Service.Interface;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"]))
        };
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine("Authentication failed: " + context.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine("Token validated for " + context.Principal.Identity.Name);
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddControllers();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT token like: Bearer <your-token>"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
    
});

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("perIp", context =>
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        Console.WriteLine($"RateLimiter: IP = {ipAddress}");
        
        return RateLimitPartition.GetFixedWindowLimiter(ipAddress, key => 
            new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = 3,
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            });
    });

    options.OnRejected = async (context, cancellationToken) =>
    {
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILogger<Program>>();
        
        var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var path = context.HttpContext.Request.Path;
        
        logger.LogWarning(
            "Rate limit exceeded for IP: {IpAddress}, Path: {Path}, Endpoint: {Endpoint}", 
            ipAddress, 
            path,
            context.HttpContext.GetEndpoint()?.DisplayName ?? "unknown");
        
        context.HttpContext.Response.StatusCode = 429;
        
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = retryAfter.TotalSeconds.ToString();
        }
        
        await context.HttpContext.Response.WriteAsync(
            "Rate limit exceeded. Please try again later.", 
            cancellationToken);
    };
});

builder.Services.AddHttpClient<OauthService>();

builder.Services.AddHttpClient("ProxyClient", client =>
    {
        client.Timeout = TimeSpan.FromSeconds(100);
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        AllowAutoRedirect = false,
        UseCookies = false
    });
builder.Services.AddScoped<IOauthService, OauthService>();
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = builder.Configuration.GetValue<string>("Redis:Connection");
    return ConnectionMultiplexer.Connect(configuration);
});
var app = builder.Build();



if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
        c.RoutePrefix = string.Empty;
    });
}
app.UseRateLimiter();
app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();
app.MapFallback("/{**path}",[Authorize] async  (
    HttpContext context,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<Program> logger)  =>
{
    
    var path = context.Request.Path + context.Request.QueryString;
    logger.LogInformation("Receive request with the path {path}", path);
    
    var url = configuration["Target:Host"] + path;

    var httpClient = httpClientFactory.CreateClient("ProxyClient");

    var requestMessage = new HttpRequestMessage();
    requestMessage.Method = new HttpMethod(context.Request.Method);
    requestMessage.RequestUri = new Uri(url);

    if (context.Request.ContentLength > 0)
    {
        requestMessage.Content = new StreamContent(context.Request.Body);
        
        if (context.Request.ContentType != null)
        {
            requestMessage.Content.Headers.ContentType = 
                new System.Net.Http.Headers.MediaTypeHeaderValue(context.Request.ContentType);
        }
    }

    var headersToSkip = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Host", "Connection", "Keep-Alive", "Transfer-Encoding",
        "Upgrade", "Proxy-Connection", "Content-Length", "Content-Type"
    };

    foreach (var header in context.Request.Headers)
    {
        if (headersToSkip.Contains(header.Key))
            continue;

        if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
        {
            requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }
    }

    HttpResponseMessage response;
    try
    {
        response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
    }
    catch (HttpRequestException ex)
    {
        context.Response.StatusCode = 502;
        await context.Response.WriteAsync($"Error connecting to target server: {ex.Message}");
        return;
    }
    catch (TaskCanceledException)
    {
        context.Response.StatusCode = 504;
        await context.Response.WriteAsync("Request to target server timed out");
        return;
    }
    
    context.Response.StatusCode = (int)response.StatusCode;

    var responseHeadersToSkip = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Transfer-Encoding", "Connection", "Keep-Alive", "Upgrade", "Proxy-Connection"
    };

    foreach (var header in response.Headers)
    {
        if (!responseHeadersToSkip.Contains(header.Key))
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }
    }
    
    foreach (var header in response.Content.Headers)
    {
        if (!responseHeadersToSkip.Contains(header.Key))
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }
    }

    await response.Content.CopyToAsync(context.Response.Body);
})
.RequireRateLimiting("perIp");

app.Run();