using System;
using System.Diagnostics;

namespace net.vieapps.Services.Utility.WAMPRouter
{
	internal static class Helper
	{
		static EventLog EventLog = null;

		internal static void InitializeLog()
		{
			if (Helper.EventLog == null)
			{
				string logName = "Application";
				string logSource = "VIEApps WAMP Router";

				if (!EventLog.SourceExists(logSource))
					EventLog.CreateEventSource(logSource, logName);

				Helper.EventLog = new EventLog(logSource)
				{
					Source = logSource,
					Log = logName
				};
			}
		}

		internal static void DisposeLog()
		{
			Helper.EventLog.Close();
			Helper.EventLog.Dispose();
		}

		internal static void WriteLog(string log, Exception ex = null)
		{
			string msg = log + (ex != null ? "\r\n\r\n" + "Message: " + ex.Message + " [" + ex.GetType().ToString() + "]\r\n\r\n" + "Details: " + ex.StackTrace : "");
			if (Program.AsService)
				Helper.EventLog.WriteEntry(msg, ex != null ? EventLogEntryType.Error : EventLogEntryType.Information);
			else
				Program.Form.UpdateLogs(msg);
		}
	}
}