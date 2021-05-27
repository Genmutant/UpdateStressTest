using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UpdateStressTest.Settings;

namespace UpdateStressTest {
	static class Program {
		static void Main(string[] args) {
			using var host = Host.CreateDefaultBuilder(args)
				.UseConsoleLifetime()
				.ConfigureServices((context, services) =>
				{
					services.Configure<StressTestSettings>(context.Configuration.GetSection("StressTestSettings"));
					var connectionString = context.Configuration.GetConnectionString("Database");
					services.AddTransient(_ => new SqlConnection(connectionString));
					services.AddHostedService<Worker>();
				})
				.Build();
			host.Run();
		}
	}
}