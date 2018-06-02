using System;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace net.vieapps.Services.Utility.WAMPRouter
{
    class Program
    {
        static void Main(string[] args)
        {
			Console.OutputEncoding = System.Text.Encoding.UTF8;

			var loggerFactory = new ServiceCollection()
				.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Information))
				.BuildServiceProvider()
				.GetService<ILoggerFactory>();

			if (Environment.UserInteractive)
				loggerFactory.AddConsole(LogLevel.Information);

			var logger = loggerFactory.CreateLogger<ServiceComponent>();
			ServiceComponent serviceComponent = null;

			void showInfo()
			{
				logger.LogInformation(
					$"VIEApps NGX WAMP Router Info" + "\r\n\t" + 
					$"- Listening URI: {serviceComponent.Address}{serviceComponent.Realm}" + "\r\n\t" +
					$"- Powered Component: {serviceComponent.ComponentInfo}" + "\r\n\t" +
					$"- Hosted Realm Session ID: {serviceComponent.HostedRealm.SessionId}" + "\r\n\t" +
					$"- Platform: {RuntimeInformation.FrameworkDescription} @ {(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows" : RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "Linux" : "Other OS")} {RuntimeInformation.OSArchitecture} ({RuntimeInformation.OSDescription.Trim()})" + "\r\n\t" +
					$"- Process ID: {Process.GetCurrentProcess().Id}" + "\r\n\t" +
					$"- Connections: {serviceComponent.Connections.Count:###,##0}"
				);
			}

			void showCommands()
			{
				logger.LogInformation(
					$"VIEApps NGX WAMP Router commands" + "\r\n\t" +
					$"- info: show the related information of the router" + "\r\n\t" +
					$"- connections: show the related information of all connections" + "\r\n\t" +
					$"- exit: shutdown and terminate the router"
				);
			}

			serviceComponent = new ServiceComponent
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
			};
			serviceComponent.Start(args);

			var command = Console.ReadLine();
			while (command != "exit")
			{
				if (command.ToLower().Equals("info"))
					showInfo();
				else if (command.ToLower().Equals("connections"))
				{
					var connections = $"Total of connections: {serviceComponent.Connections.Count:#,##0}" + "\r\n" + "Details:";
					serviceComponent.Connections.Values.ToList().ForEach(info => connections += "\r\n\t" + $"Session ID: {info.SessionID} - Connection Info: {info.ConnectionID} - {info.EndPoint})");
					logger.LogInformation(connections);
				}
				else
					showCommands();
				command = Console.ReadLine();
			}

			serviceComponent.Stop();
		}
    }
}