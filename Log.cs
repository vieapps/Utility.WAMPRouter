using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Diagnostics;

namespace net.vieapps.Services.Utility.WAMPRouter
{
	internal static class Log
	{
		static EventLog EventLog = null;

		internal static void InitializeEventLog()
		{
			if (Log.EventLog == null)
			{
				string logName = "Application";
				string logSource = "VIEApps WAMP Router";

				if (!EventLog.SourceExists(logSource))
					EventLog.CreateEventSource(logSource, logName);

				Log.EventLog = new EventLog(logSource)
				{
					Source = logSource,
					Log = logName
				};
			}
		}

		internal static void DisposeEventLog()
		{
			Log.EventLog.Close();
			Log.EventLog.Dispose();
		}

		internal static void WriteLog(string log, Exception ex = null)
		{
			string msg = log + (ex != null ? "\r\n\r\n" + "Message: " + ex.Message + " [" + ex.GetType().ToString() + "\r\n\r\n" + "Details: " + ex.StackTrace : "");
			if (Program.AsService)
				Log.EventLog.WriteEntry(msg, ex != null ? EventLogEntryType.Error : EventLogEntryType.Information);
			else
				Console.WriteLine(msg);
		}

	}
}