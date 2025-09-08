using Microsoft.EntityFrameworkCore;
using TNA.BLL.Mapping;
using TNA.BLL.Config;
using TNA.DAL.Repositories.Interfaces;
using TNA.DAL.Repositories.Implementations;
using TNA.BLL.Services.Interfaces;
using TNA.BLL.Services.Implementations;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Bind Pubg options from configuration
builder.Services.Configure<PubgOptions>(builder.Configuration.GetSection("Pubg"));

// Registrar AutoMapper (escanea el ensamblado del perfil)
builder.Services.AddAutoMapper(typeof(AutoMapperProfile).Assembly);

// Registrar HttpClient factory
builder.Services.AddHttpClient();

// Registrar DbContext usando la cadena de conexión
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<TNA.DAL.DbContext.TNADbContext>(options =>
    options.UseSqlServer(connectionString));

// Registrar repositorios
builder.Services.AddScoped<IClanRepository, ClanRepository>();
builder.Services.AddScoped<IClanMemberRepository, ClanMemberRepository>();
builder.Services.AddScoped<IClanMemberSMRepository, ClanMemberSMRepository>();
builder.Services.AddScoped<IMatchRepository, MatchRepository>();
builder.Services.AddScoped<IPlayerMatchRepository, PlayerMatchRepository>();

// Registrar servicios de negocio
builder.Services.AddScoped<IClanService, ClanServcice>();
builder.Services.AddScoped<IClanMemberService, ClanMemberService>();
builder.Services.AddScoped<IClanMemberSMService, ClanMemberSMService>();
builder.Services.AddScoped<IPubgService, PubgService>();
builder.Services.AddScoped<IPlayerMatchService, PlayerMatchService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();