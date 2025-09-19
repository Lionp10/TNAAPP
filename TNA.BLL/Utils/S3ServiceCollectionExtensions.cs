using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TNA.BLL.DTOs;
using TNA.BLL.Services.Implementations;
using TNA.BLL.Services.Interfaces;

namespace TNA.BLL.Utils
{
    public static class S3ServiceCollectionExtensions
    {
        public static IServiceCollection AddAwsS3(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<AwsS3OptionsDTO>(configuration.GetSection("AwsS3"));
            services.AddSingleton<IS3Service, S3Service>();
            return services;
        }
    }
}
