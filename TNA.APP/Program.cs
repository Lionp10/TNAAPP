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
using TNA.BLL.Utils; 
using TNA.BLL.DTOs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.Configure<PubgOptions>(builder.Configuration.GetSection("Pubg"));

builder.Services.Configure<EmailDTO>(builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddAutoMapper(typeof(AutoMapperProfile).Assembly);

builder.Services.AddHttpClient();

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<TNA.DAL.DbContext.TNADbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<IClanRepository, ClanRepository>();
builder.Services.AddScoped<IClanMemberRepository, ClanMemberRepository>();
builder.Services.AddScoped<IClanMemberSMRepository, ClanMemberSMRepository>();
builder.Services.AddScoped<IMatchRepository, MatchRepository>();
builder.Services.AddScoped<IPlayerMatchRepository, PlayerMatchRepository>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddScoped<IClanService, ClanServcice>();
builder.Services.AddScoped<IClanMemberService, ClanMemberService>();
builder.Services.AddScoped<IClanMemberSMService, ClanMemberSMService>();
builder.Services.AddScoped<IPubgService, PubgService>();
builder.Services.AddScoped<IPlayerMatchService, PlayerMatchService>();

builder.Services.AddAwsS3(builder.Configuration);

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