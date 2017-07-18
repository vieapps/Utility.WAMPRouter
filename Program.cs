using System;
using System.ServiceProcess;

namespace net.vieapps.Services.Utility.WAMPRouter
{
	static class Program
	{
		internal static bool AsService = !Environment.UserInteractive;

		static void Main(string[] args)
		{
			using (var svc = new ServiceRunner())
			{
				if (Program.AsService)
					ServiceBase.Run(new ServiceBase[] { svc });
				else
				{
					args = args != null && args.Length > 0
						? args
						: new string[] { "/async:true" };

					Log.WriteLog("\r\n\r\n");
					Log.WriteLog("The VIEApps WAMP Router is running as console app, press any key to terminate...");
					Log.WriteLog("\r\n");
					Log.WriteLog("--- Syntax: VIEApps.WAMPRouter.exe /endpoint:<ws://ip:port> /realm:<realm-name> /async:<true|false>");
					Log.WriteLog("\r\n");
					Log.WriteLog("To install as a Windows service, use the InstallUtil in the command prompt as \"InstallUtil /i VIEApps.WAMPRouter.exe\" (with Administrator privileges)");
					Log.WriteLog("\r\n");
					Log.WriteLog("--------------------------------------------------------------------");
					Log.WriteLog("ARGS: " + (args != null && args.Length > 0 ? string.Join(" ", args) : "None"));
					Log.WriteLog("--------------------------------------------------------------------");
					Log.WriteLog("OUTPUT:\r\n");

					svc.DoStart(args);
					Console.ReadKey(true);
					svc.DoStop();
				}
			}
		}
	}
}