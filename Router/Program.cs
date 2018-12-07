using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace net.vieapps.Services.Utility.WAMPRouter
{
	class Program
	{
		static void Main(string[] args)
		{
			// prepare
			Console.OutputEncoding = System.Text.Encoding.UTF8;
			var isUserInteractive = Environment.UserInteractive && args?.FirstOrDefault(a => a.StartsWith("/daemon")) == null;
			var loggerFactory = new ServiceCollection()
				.AddLogging(builder =>
				{
					builder.SetMinimumLevel(LogLevel.Information);
					if (isUserInteractive)
						builder.AddConsole();
				})
				.BuildServiceProvider()
				.GetService<ILoggerFactory>();
			var logger = loggerFactory.CreateLogger<ServiceComponent>();
			ServiceComponent serviceComponent = null;

			void showInfo()
			{
				logger.LogInformation("VIEApps NGX WAMP Router Info" + "\r\n\t" + serviceComponent.RouterInfoString);
			}

			void showCommands()
			{
				logger.LogInformation(
					$"VIEApps NGX WAMP Router commands" + "\r\n\t" +
					$"- info: show the router information" + "\r\n\t" +
					$"- sessions: show all the sessions" + "\r\n\t" +
					$"- help: show the available commands" + "\r\n\t" +
					$"- exit: shutdown and terminate"
				);
			}

			void processCommands()
			{
				var command = Console.ReadLine();
				while (command != null)
				{
					if (command.ToLower().Equals("exit"))
						return;

					else if (command.ToLower().Equals("info"))
						showInfo();

					else if (command.ToLower().Equals("sessions"))
						logger.LogInformation(serviceComponent.SessionsInfoString);

					else
						showCommands();

					command = Console.ReadLine();
				}
			}

			void stop()
			{
				serviceComponent.OnError = null;
				serviceComponent.Stop();
			}

			// setup hooks
			AppDomain.CurrentDomain.ProcessExit += (sender, arguments) => stop();
			Console.CancelKeyPress += (sender, arguments) =>
			{
				stop();
				Environment.Exit(0);
			};

			// start
			serviceComponent = isUserInteractive
				? new ServiceComponent
				{
					OnError = ex => logger.LogError(ex, ex.Message),
					OnStarted = () =>
					{
						logger.LogInformation("VIEApps NGX WAMP Router is ready for serving");
						showInfo();
						showCommands();
					},
					OnStopped = () => logger.LogInformation("VIEApps NGX WAMP Router is stopped"),
					OnSessionCreated = info => logger.LogInformation($"A session is opened - Session ID: {info.SessionID} - Connection Info: {info.ConnectionID} - {info.EndPoint})"),
					OnSessionClosed = info => logger.LogInformation($"A session is closed - Type: {info?.CloseType} ({info?.CloseReason ?? "N/A"}) - Session ID: {info?.SessionID} - Connection Info: {info?.ConnectionID} - {info?.EndPoint})")
				}
				: new ServiceComponent
				{
					OnError = ex => Console.Error.WriteLine(ex.Message + "\r\n" + ex.StackTrace),
					OnStarted = () => Console.WriteLine("VIEApps NGX WAMP Router is ready for serving" + "\r\n\t" + serviceComponent.RouterInfoString + "\r\n\t" + $"- Starting time: {DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}"),
					OnStopped = () => Console.WriteLine("VIEApps NGX WAMP Router is stopped\r\n")
				};

			serviceComponent.Start(args);

			// processing commands util got an exit signal
			if (isUserInteractive)
				processCommands();

			// wait until be killed
			else
				while (true)
					Task.Delay(54321).GetAwaiter().GetResult();

			// stop
			stop();
		}
	}
}