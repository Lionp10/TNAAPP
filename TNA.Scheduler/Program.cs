using Microsoft.EntityFrameworkCore;
using TNA.BLL.Config;
using TNA.BLL.Mapping;
using TNA.BLL.Services.Implementations;
using TNA.BLL.Services.Interfaces;
using TNA.DAL.DbContext;
using TNA.DAL.Repositories;
using TNA.DAL.Repositories.Implementations;
using TNA.DAL.Repositories.Interfaces;
using TNA.Scheduler;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Information);
    })
    .ConfigureServices((context, services) =>
    {
        services.Configure<PubgOptions>(context.Configuration.GetSection("Pubg"));

        services.AddHttpClient();

        services.AddAutoMapper(typeof(AutoMapperProfile).Assembly);

        var connectionString = context.Configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<TNADbContext>(options => options.UseSqlServer(connectionString));

        services.AddScoped<IClanRepository, ClanRepository>();
        services.AddScoped<IClanMemberRepository, ClanMemberRepository>();
        services.AddScoped<IMatchRepository, MatchRepository>();
        services.AddScoped<IPlayerMatchRepository, PlayerMatchRepository>();
        services.AddScoped<IPlayerLifetimeRepository, PlayerLifetimeRepository>();

        services.AddScoped<IClanService, ClanServcice>();
        services.AddScoped<IPubgService, PubgService>();

        services.AddHostedService<DailyWorker>();
    })
    .Build();

await host.RunAsync();
