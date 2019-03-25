using System;
using System.ServiceProcess;
using System.Windows.Forms;
using System.Diagnostics;

namespace net.vieapps.Services.Utility.WAMPRouter
{
	static class Program
	{
		internal static ServiceComponent ServiceComponent { get; set; } = null;
		internal static EventLog EventLog { get; set; } = null;
		internal static ServicePresenter Form { get; set; } = null;

		static void Main(string[] args)
		{
			if (!Environment.UserInteractive)
				ServiceBase.Run(new ServiceRunner());
			else
			{
				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);

				Program.Form = new ServicePresenter();
				Application.Run(Program.Form);
			}
		}

		internal static void Start(string[] args)
		{
			if (!Environment.UserInteractive)
			{
				var name = "Application";
				var source = "VIEApps-WAMP-Router";

				if (!EventLog.SourceExists(source))
					EventLog.CreateEventSource(source, name);

				Program.EventLog = new EventLog()
				{
					Source = source,
					Log = name
				};
			}

			Program.ServiceComponent = new ServiceComponent
			{
				OnError = ex => Program.WriteLog(ex.Message, ex),
				OnStarted = () => Program.WriteLog(Program.ServiceComponent.RouterInfoString.Replace("\t", "")),
				OnStopped = () => Program.WriteLog("VIEApps NGX WAMP Router is stopped"),
				OnSessionCreated = info =>
				{
					if (Environment.UserInteractive)
						Program.WriteLog("\r\n" + $"A session is opened - Session ID: {info.SessionID} - Connection Info: {info.ConnectionID} - {info.EndPoint})");
				},
				OnSessionClosed = info =>
				{
					if (Environment.UserInteractive)
						Program.WriteLog("\r\n" + $"A session is closed - Type: {info?.CloseType} ({info?.CloseReason ?? "N/A"}) - Session ID: {info?.SessionID} - Connection Info: {info?.ConnectionID} - {info?.EndPoint})");
				}
			};
			Program.ServiceComponent.Start(args);
		}

		internal static void Stop()
		{
			Program.ServiceComponent.Stop();
			if (!Environment.UserInteractive)
				Program.EventLog.Dispose();
		}

		internal static void WriteLog(string log, Exception ex = null)
		{
			var msg = $"{log}{(ex != null ? $"\r\n\r\n{ex.StackTrace}" : "")}";
			if (Environment.UserInteractive)
				Program.Form.UpdateLogs(msg);
			else
				Program.EventLog.WriteEntry(msg, ex != null ? EventLogEntryType.Error : EventLogEntryType.Information);
		}
	}
}