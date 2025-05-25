using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using AuthService;
using AuthService.Data;
using AuthService.Services;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using AuthService.Models;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddScoped<JWTService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AuthService API",
        Version = "v1"
    });

    // Додаємо кнопку авторизації для Swagger UI
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your valid token in the text input below.\nExample: \"Bearer YOUR_JWT_TOKEN\""
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            new List<string>()
        }
    });
});

//builder.Services.AddAuthentication().AddCookie(IdentityConstants.ApplicationScheme);

//builder.Services.AddIdentityCore<ApplicationUser>()
//    .AddEntityFrameworkStores<ApplicationDbContext>()
//    .AddApiEndpoints();

builder.Services.Configure<AuthentificationSettings>(builder.Configuration.GetSection(nameof(AuthentificationSettings)));

builder.Services.AddIdentityCore<ApplicationUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();



builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    
})
.AddCookie(IdentityConstants.ApplicationScheme)
.AddJwtBearer(options =>
{
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = async context =>
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            Console.WriteLine($"OnMessageReceived - Raw header: {authHeader}");

            if (authHeader != null && authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();
                Console.WriteLine($"OnMessageReceived - Extracted token: {token}");
                context.Token = token;
            }
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine("Token was successfully validated");
            var claims = context.Principal?.Claims.Select(c => $"{c.Type}: {c.Value}");
            Console.WriteLine($"Claims: {string.Join(", ", claims ?? Array.Empty<string>())}");
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"Authentication failed: {context.Exception.GetType().Name}");
            Console.WriteLine($"Error details: {context.Exception.Message}");
            if (context.Exception.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {context.Exception.InnerException.Message}");
            }
            return Task.CompletedTask;
        }
        
    };
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration.GetSection(nameof(AuthentificationSettings))["Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration.GetSection(nameof(AuthentificationSettings))["Audience"],
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration.GetSection(nameof(AuthentificationSettings))["SecurityStringKey"])),

        ClockSkew = TimeSpan.FromMinutes(5)
        
    };

});

builder.Services.AddAuthorization();


builder.Services.AddAutoMapper(typeof(MappingConfig));

var app = builder.Build();

//await InitializeDatabaseAsync(app.Services);
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        //await dbContext.Database.EnsureDeletedAsync(); // Delete existing database
        await dbContext.Database.MigrateAsync(); // Reapply migrations
    }
    app.UseSwagger();
    app.UseSwaggerUI();
}
var roleScope = app.Services.CreateScope();
var roleManager = roleScope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
await RoleInitializer.InitializeRoles(roleManager);

/*test migrate*/
//var dataScope = app.Services.CreateScope();
//var dbContext = dataScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
//await dbContext.Database.MigrateAsync();

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.Use(async (context, next) =>
{
    var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
    Console.WriteLine($"Received Authorization header: {authHeader}");
    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
