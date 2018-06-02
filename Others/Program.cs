using System;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace net.vieapps.Services.Utility.WAMPRouter
{
    class Program
    {
        static void Main(string[] args)
        {
        	var isUserInteractive = Environment.UserInteractive
        		? args?.FirstOrDefault(a => a.StartsWith("/daemon")) == null
        		: false;

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
					$"- info: show the related information of the router" + "\r\n\t" +
					$"- sessions: show the related information of all sessions" + "\r\n\t" +
					$"- exit: shutdown and terminate the router"
				);
			}

			serviceComponent = new ServiceComponent
			{
				OnError = ex => logger.LogError(ex, ex.Message),
				OnStarted = () =>
				{
					logger.LogInformation("VIEApps NGX WAMP Router is ready for serving");
					if (isUserInteractive)
					{
						showInfo();
						showCommands();
					}
				},
				OnStopped = () => logger.LogInformation("VIEApps NGX WAMP Router is stopped"),
				OnSessionCreated = info => logger.LogInformation($"A session is opened - Session ID: {info.SessionID} - Connection Info: {info.ConnectionID} - {info.EndPoint})"),
				OnSessionClosed = info => logger.LogInformation($"A session is closed - Type: {info?.CloseType} ({info?.CloseReason ?? "N/A"}) - Session ID: {info?.SessionID} - Connection Info: {info?.ConnectionID} - {info?.EndPoint})")
			};
			serviceComponent.Start(args);

			if (isUserInteractive)
			{
				var command = Console.ReadLine();
				while (command != "exit")
				{
					if (command.ToLower().Equals("info"))
						showInfo();
					else if (command.ToLower().Equals("sessions"))
						logger.LogInformation(serviceComponent.SessionsInfoString);
					else
						showCommands();
					command = Console.ReadLine();
				}
				serviceComponent.Stop();
			}
			else
				while (true) { }
		}
	}
}