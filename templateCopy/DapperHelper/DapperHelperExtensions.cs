using System.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Dapper
{
    public static class DapperHelperExtensions
    {
        public static IServiceCollection AddDapperHelper<TConnection>(this IServiceCollection services,
            IConfiguration config) where TConnection : IDbConnection, new()
        {
            services.AddOptions<DapperHelperOptions>()
                .Configure(config.Bind)
                .ValidateDataAnnotations();

            services.AddSingleton<IDapperHelper, DapperHelper<TConnection>>();
            return services;
        }

        // kenghua: 加一個不使用 IConfiguration 的 AddDapperHelper
        public static IServiceCollection AddDapperHelper<TConnection>(this IServiceCollection services,
                string connectionString) where TConnection : IDbConnection, new()
        {
            // 建立 DapperHelperOptions 實例並配置
            services.Configure<DapperHelperOptions>(options =>
            {
                options.ConnectionString = connectionString;
            });

            // 註冊 DapperHelper 為單實例服務
            services.AddSingleton<IDapperHelper, DapperHelper<TConnection>>();
            return services;
        }
    }

    public class DapperHelperFactory
    {
        public IDapperHelper Create<TConnection>(string connectionString) where TConnection : IDbConnection, new()
        {
            return new DapperHelper<TConnection>(connectionString);
        }
    }
}