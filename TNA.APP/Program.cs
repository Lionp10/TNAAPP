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

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Bind Pubg options from configuration
builder.Services.Configure<PubgOptions>(builder.Configuration.GetSection("Pubg"));

// Registrar AutoMapper (escanea el ensamblado del perfil)
builder.Services.AddAutoMapper(typeof(AutoMapperProfile).Assembly);

// Registrar HttpClient factory
builder.Services.AddHttpClient();

// Registrar PasswordHasher para hashear contrase�as en UserService
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// Registrar DbContext usando la cadena de conexi�n
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<TNA.DAL.DbContext.TNADbContext>(options =>
    options.UseSqlServer(connectionString));

// Registrar repositorios existentes
builder.Services.AddScoped<IClanRepository, ClanRepository>();
builder.Services.AddScoped<IClanMemberRepository, ClanMemberRepository>();
builder.Services.AddScoped<IClanMemberSMRepository, ClanMemberSMRepository>();
builder.Services.AddScoped<IMatchRepository, MatchRepository>();
builder.Services.AddScoped<IPlayerMatchRepository, PlayerMatchRepository>();

// Registrar repositorio y servicio de Usuarios (recientemente a�adidos)
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();

// Registrar servicios de negocio
builder.Services.AddScoped<IClanService, ClanServcice>();
builder.Services.AddScoped<IClanMemberService, ClanMemberService>();
builder.Services.AddScoped<IClanMemberSMService, ClanMemberSMService>();
builder.Services.AddScoped<IPubgService, PubgService>();
builder.Services.AddScoped<IPlayerMatchService, PlayerMatchService>();

// Authentication: establecer esquema por defecto y cookie auth
builder.Services.AddAuthentication(options =>
{
    // Esquema por defecto para Authenticate/Challenge/SignIn
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

    // Evitar que la app "pinche" en entornos donde quieres redirigir a home
    options.Events = new CookieAuthenticationEvents
    {
        // Si se produce un Challenge (no autenticado), redirige a "/"
        OnRedirectToLogin = ctx =>
        {
            // Cambia a la ruta que prefieras; aqu� redirigimos a la p�gina principal
            ctx.Response.Redirect("/");
            return Task.CompletedTask;
        },
        // Si no tiene permiso, redirigimos a home o a una p�gina espec�fica
        OnRedirectToAccessDenied = ctx =>
        {
            ctx.Response.Redirect("/");
            return Task.CompletedTask;
        }
    };
});

// Authorization: policy para administradores usando la claim "roleid"
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireClaim("roleid", "1"));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// IMPORTANT: activar autenticaci�n antes de autorizaci�n
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();