using Microsoft.EntityFrameworkCore;
using TNA.BLL.Mapping;
using TNA.BLL.Config;
using TNA.DAL.Repositories.Interfaces;
using TNA.DAL.Repositories.Implementations;
using TNA.BLL.Services.Interfaces;
using TNA.BLL.Services.Implementations;
using Microsoft.AspNetCore.Identity;
using TNA.DAL.Entities;
using Microsoft.AspNetCore.Authentication.Cookies;
using TNA.BLL.DTOs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Bind Pubg options from configuration
builder.Services.Configure<PubgOptions>(builder.Configuration.GetSection("Pubg"));

// Bind EmailSettings -> EmailDTO
builder.Services.Configure<EmailDTO>(builder.Configuration.GetSection("EmailSettings"));

// Registrar AutoMapper (escanea el ensamblado del perfil)
builder.Services.AddAutoMapper(typeof(AutoMapperProfile).Assembly);

// Registrar HttpClient factory
builder.Services.AddHttpClient();

// Registrar PasswordHasher para hashear contraseñas en UserService
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// Registrar DbContext usando la cadena de conexión
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<TNA.DAL.DbContext.TNADbContext>(options =>
    options.UseSqlServer(connectionString));

// Registrar repositorios existentes
builder.Services.AddScoped<IClanRepository, ClanRepository>();
builder.Services.AddScoped<IClanMemberRepository, ClanMemberRepository>();
builder.Services.AddScoped<IClanMemberSMRepository, ClanMemberSMRepository>();
builder.Services.AddScoped<IMatchRepository, MatchRepository>();
builder.Services.AddScoped<IPlayerMatchRepository, PlayerMatchRepository>();

// Registrar repositorio y servicio de Usuarios (recientemente añadidos)
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();

// Registrar servicio de correo (tu implementación) — solo IEmailService
builder.Services.AddScoped<IEmailService, EmailService>();

// Registrar servicios de negocio
builder.Services.AddScoped<IClanService, ClanServcice>();
builder.Services.AddScoped<IClanMemberService, ClanMemberService>();
builder.Services.AddScoped<IClanMemberSMService, ClanMemberSMService>();
builder.Services.AddScoped<IPubgService, PubgService>();
builder.Services.AddScoped<IPlayerMatchService, PlayerMatchService>();

// Authentication: establecer esquema por defecto y cookie auth
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/Auth/Index";
    options.LogoutPath = "/Auth/Logout";
    options.Cookie.Name = "TNA.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);

    options.Events = new CookieAuthenticationEvents
    {
        OnRedirectToLogin = ctx =>
        {
            ctx.Response.Redirect("/");
            return Task.CompletedTask;
        },
        OnRedirectToAccessDenied = ctx =>
        {
            ctx.Response.Redirect("/");
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireClaim("roleid", "1"));
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();