using Amazon.S3;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RecipeService;
using RecipeService.Data;
using RecipeService.Repository;
using RecipeService.Services;
using RecipeService.Validators;
using System;
using System.Security.Claims;
using System.Text;

//var builder = WebApplication.CreateBuilder(args);

//// Додаємо сервіси
//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
//builder.Services.AddStackExchangeRedisCache(options =>
//{
//    options.Configuration = builder.Configuration["Redis:ConnectionString"];
//});
//builder.Services.AddHttpClient();


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

/*include repo*/
builder.Services.AddScoped<IRecipeRepository, RecipeRepository>();
builder.Services.AddScoped<IBreadcrumbRepository, BreadcrumbRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ICollectionRepository, CollectionRepository>();

builder.Services.AddScoped<IMinIOService, MinIOService>();

builder.Services.AddAutoMapper(typeof(MappingConfig)); // Реєстрація AutoMapper
builder.Services.AddAWSService<IAmazonS3>();
builder.Services.AddControllers().AddNewtonsoftJson(); 
builder.Services.AddFluentValidationAutoValidation().AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<RecipeCreateDTOValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<RecipeUpdateDTOValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CollectionCreateValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CollectionUpdateValidator>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RecipeService API",
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

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration.GetSection(nameof(AuthentificationSettings))["Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration.GetSection(nameof(AuthentificationSettings))["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration.GetSection(nameof(AuthentificationSettings))["SecurityStringKey"])),

            ClockSkew = TimeSpan.FromMinutes(5)
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = async context =>
            {
                var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                Console.WriteLine($"RecipeService: OnMessageReceived - Raw header: {authHeader}");
                if (authHeader != null && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    var token = authHeader.Substring("Bearer ".Length).Trim();
                    Console.WriteLine($"RecipeService: OnMessageReceived - Extracted token: {token}");
                    context.Token = token;
                }
                else
                {
                    Console.WriteLine("RecipeService: OnMessageReceived - No valid Bearer token found");
                }
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine("RecipeService: Token was successfully validated");
                var claims = context.Principal?.Claims.Select(c => $"{c.Type}: {c.Value}");
                Console.WriteLine($"RecipeService: Claims: {string.Join(", ", claims ?? Array.Empty<string>())}");
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"RecipeService: Authentication failed: {context.Exception.GetType().Name}");
                Console.WriteLine($"RecipeService: Error details: {context.Exception.Message}");
                if (context.Exception.InnerException != null)
                {
                    Console.WriteLine($"RecipeService: Inner exception: {context.Exception.InnerException.Message}");
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
