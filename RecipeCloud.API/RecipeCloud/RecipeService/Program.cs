using Amazon.S3;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using RecipeService;
using RecipeService.Data;
using RecipeService.Middlewares;
using RecipeService.Repository;
using RecipeService.Services;
using RecipeService.Validators.CollectionValidators;
using RecipeService.Validators.RecipeRatingValidators;
using RecipeService.Validators.RecipeValidators;
using StackExchange.Redis;
using System;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;

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

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    return ConnectionMultiplexer.Connect(builder.Configuration["Redis:ConnectionString"]);
});

/*include repo*/
builder.Services.AddScoped<IRecipeRepository, RecipeRepository>();
builder.Services.AddScoped<IBreadcrumbRepository, BreadcrumbRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ICollectionRepository, CollectionRepository>();
builder.Services.AddScoped<IRecipeRatingRepository, RecipeRatingRepository>();

builder.Services.AddScoped<IRedisCache, RedisCache>();

builder.Services.AddScoped<IMinIOService, MinIOService>();

builder.Services.AddAutoMapper(typeof(MappingConfig)); // Реєстрація AutoMapper
builder.Services.AddAWSService<IAmazonS3>();
builder.Services.AddControllers()
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddFluentValidationAutoValidation().AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<RecipeCreateDTOValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<RecipeUpdateDTOValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CollectionCreateValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CollectionUpdateValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<RecipeRatingValidator>();

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
builder.Services.AddRateLimiter(options => {
    options.AddFixedWindowLimiter("basic", options => {
        options.Window = TimeSpan.FromSeconds(10);
        options.PermitLimit = 5;
    });
});

// OpenTelemetry конфігурація (вбудована в .NET 8+)
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .AddAspNetCoreInstrumentation() // Інструментація для ASP.NET Core
            .AddHttpClientInstrumentation() // Для HttpClient (наприклад, MinIO)
            .AddConsoleExporter() // Експорт до консолі для dev
            .AddOtlpExporter(options => // OTLP для production (наприклад, до Jaeger або Zipkin)
            {
                options.Endpoint = new Uri("http://localhost:4317"); // Приклад ендпоінту
            });
    })
    .WithMetrics(meterProviderBuilder =>
    {
        meterProviderBuilder
            .AddAspNetCoreInstrumentation()
            .AddConsoleExporter();
    });


builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true; 
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

var policyCollection = new HeaderPolicyCollection()
    .AddFrameOptionsDeny()
    .AddContentTypeOptionsNoSniff()
    .AddStrictTransportSecurityMaxAgeIncludeSubDomains(maxAgeInSeconds: 60 * 60 * 24 * 365) // maxage = one year in seconds
    .AddReferrerPolicyStrictOriginWhenCrossOrigin()
    .RemoveServerHeader()
    .AddContentSecurityPolicy(builder =>
    {
        builder.AddObjectSrc().None();
        builder.AddFormAction().Self();
        builder.AddFrameAncestors().None();
    })
    .AddCustomHeader("X-My-Test-Header", "Header value");



var app = builder.Build();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
//app.UseResponseCompression(); // before others middleware
//app.UseSecurityHeaders(policyCollection);

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
app.UseRateLimiter();

app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
