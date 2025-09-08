using Microsoft.EntityFrameworkCore;
using TNA.BLL.Config;
using TNA.BLL.Mapping;
using TNA.BLL.Services.Implementations;
using TNA.BLL.Services.Interfaces;
using TNA.DAL.DbContext;
using TNA.DAL.Repositories.Implementations;
using TNA.DAL.Repositories.Interfaces;
using TNA.Scheduler;

var host = Host.CreateDefaultBuilder(args)
    // Forzar logging a consola y nivel mínimo
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Information);
    })
    .ConfigureServices((context, services) =>
    {
        // Config
        services.Configure<PubgOptions>(context.Configuration.GetSection("Pubg"));

        // HttpClient factory
        services.AddHttpClient();

        // AutoMapper
        services.AddAutoMapper(typeof(AutoMapperProfile).Assembly);

        // DbContext - usa la misma connection string que la app web
        var connectionString = context.Configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<TNADbContext>(options => options.UseSqlServer(connectionString));

        // Repositorios + servicios (mismas implementaciones que en la web)
        services.AddScoped<IClanRepository, ClanRepository>();
        services.AddScoped<IClanMemberRepository, ClanMemberRepository>();
        services.AddScoped<IMatchRepository, MatchRepository>();
        services.AddScoped<IPlayerMatchRepository, PlayerMatchRepository>();

        services.AddScoped<IClanService, ClanServcice>();
        services.AddScoped<IPubgService, PubgService>();

        // Worker que ejecuta la tarea diaria
        services.AddHostedService<DailyWorker>();
    })
    .Build();

await host.RunAsync();
