using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UpdateStressTest.Settings;

namespace UpdateStressTest {
	public class Worker : BackgroundService {
		private readonly ILogger<Worker> _logger;
		private readonly IServiceProvider _serviceProvider;
		private readonly IOptionsMonitor<StressTestSettings> _stressTestSettingsOptions;
		
		private int _slowCounter;
		private int _updateCounter;

		public Worker(IOptionsMonitor<StressTestSettings> stressTestSettingsOptions, IServiceProvider serviceProvider, ILogger<Worker> logger) {
			_stressTestSettingsOptions = stressTestSettingsOptions;
			_serviceProvider = serviceProvider;
			_logger = logger;
			_stressTestSettingsOptions.OnChange(settings =>
				logger.LogInformation("Using new StressTestSettings: {StressTestSettings}", settings));
		}

		private StressTestSettings StressTestSettings => _stressTestSettingsOptions.CurrentValue;

		private static async Task<string> AddNewRowEntry(SqlConnection con, CancellationToken cancellationToken) {
			var testContainer = Guid.NewGuid().ToString();
			await using var insertCommand = con.CreateCommand();
			insertCommand.CommandText = "INSERT INTO ContainerAutoTest (Name) VALUES(@TestContainer)";
			insertCommand.Parameters.Add(new SqlParameter("@TestContainer", SqlDbType.NVarChar, 100) {Value = testContainer});
			await insertCommand.ExecuteNonQueryAsync(cancellationToken);
			return testContainer;
		}

		private static async Task<SqlCommand> BuildUpdateCommand(SqlConnection con, string testContainer,
			CancellationToken cancellationToken) {
			var updateCommand = con.CreateCommand();
			updateCommand.CommandText = "UPDATE ContainerAutoTest SET LastComment = 'TestEntry' WHERE Name = @TestContainer";
			updateCommand.Parameters.Add(new SqlParameter("@TestContainer", SqlDbType.NVarChar, 100) {Value = testContainer});
			await updateCommand.PrepareAsync(cancellationToken);
			return updateCommand;
		}

		private async Task RunUpdates(IServiceProvider serviceProvider, CancellationToken cancellationToken) {
			try {
				await using var con = serviceProvider.GetRequiredService<SqlConnection>();
				_logger.LogInformation("Running against {DataSource} : {Database} ", con.DataSource, con.Database);
				if (con.State == ConnectionState.Closed) {
					await con.OpenAsync(cancellationToken);
				}
				// Create unique row to update
				var testContainer = await AddNewRowEntry(con, cancellationToken);
				// Build update command to use from now on
				await using var updateCommand = await BuildUpdateCommand(con, testContainer, cancellationToken);
				var random = new Random();
				var stopwatch = new Stopwatch();
				while (!cancellationToken.IsCancellationRequested) {
					stopwatch.Restart();
					await updateCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
					HandleElapsedTime(stopwatch.Elapsed);
					await SleepAfterUpdate(random, cancellationToken);
				}
			}
			catch (Exception ex) {
				_logger.LogDebug(ex, "Error during RunUpdates");
			}
		}

		private async Task SleepAfterUpdate(Random random, CancellationToken cancellationToken) {
			var sleep = StressTestSettings.SleepBetweenTasks + TimeSpan.FromTicks(random.Next(1000));
			await Task.Delay(sleep, cancellationToken).ConfigureAwait(false);
		}

		private void HandleElapsedTime(TimeSpan elapsed) {
			Interlocked.Increment(ref _updateCounter);
			// Slower than configured
			if (elapsed > StressTestSettings.SlowThreshold) {
				Interlocked.Increment(ref _slowCounter);
				_logger.LogWarning("Slow Update took: {SlowElapsed}", elapsed);
			} else {
				if (_updateCounter % 1000 == 0) {
					_logger.LogInformation("Normal Update took: {NormalElapsed}", elapsed);
				}
			}
		}

		private async Task PrintStats(CancellationToken cancellationToken) {
			while (!cancellationToken.IsCancellationRequested) {
				_logger.LogInformation("Updates: {UpdateCount}, Slows: {SlowCount}", _updateCounter, _slowCounter);
				await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
			}
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
			_logger.LogInformation("Using Settings: {StressTestSettings}", StressTestSettings);
			var concurrentTasks = StressTestSettings.ConcurrentTasks;
			var tasks = new List<Task>();
			try {
				tasks.Add(Task.Factory.StartNew(() => PrintStats(stoppingToken), stoppingToken));
				var random = new Random();
				for (var i = 0; i < concurrentTasks; i++) {
					tasks.Add(Task.Factory.StartNew(() => RunUpdates(_serviceProvider, stoppingToken), stoppingToken));
					// Don't start all tasks at the same time
					await Task.Delay(TimeSpan.FromTicks(random.Next(10000)), stoppingToken);
				}
				await Task.WhenAll(tasks);
			}
			catch (Exception ex) {
				_logger.LogDebug(ex, "Error during execution");
			}
		}
	}
}