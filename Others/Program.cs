using System;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace net.vieapps.Services.Utility.WAMPRouter
{
	class Program
	{
		static void Main(string[] args)
		{
			// prepare
			var isUserInteractive = Environment.UserInteractive && args?.FirstOrDefault(a => a.StartsWith("/daemon")) == null;

			var loggerFactory = new ServiceCollection()
				.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Information))
				.BuildServiceProvider()
				.GetService<ILoggerFactory>();

			if (isUserInteractive)
			{
				Console.OutputEncoding = System.Text.Encoding.UTF8;
				loggerFactory.AddConsole(LogLevel.Information);
			}

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
				: new ServiceComponent();

			serviceComponent.Start(args);

			// setup hooks
			AppDomain.CurrentDomain.ProcessExit += (sender, arguments) =>
			{
				serviceComponent.OnError = null;
				serviceComponent.Stop();
			};

			Console.CancelKeyPress += (sender, arguments) =>
			{
				serviceComponent.OnError = null;
				serviceComponent.Stop();
				Environment.Exit(0);
			};

			// processing commands util got an exit signal
			if (isUserInteractive)
				processCommands();

			// wait until be killed
			else
				while (true)
					Task.Delay(4321).GetAwaiter().GetResult();

			// stop
			serviceComponent.OnError = null;
			serviceComponent.Stop();
		}
	}
}