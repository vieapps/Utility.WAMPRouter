#region Related components
using System;
using System.ServiceProcess;
using System.Configuration;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using WampSharp.V2;
using WampSharp.V2.Realm;
#endregion

namespace net.vieapps.Services.Utility.WAMPRouter
{
	public partial class ServiceRunner : ServiceBase
	{

		#region Constructors & Properties
		public ServiceRunner()
		{
			this.InitializeComponent();
		}

		EventLog _log = null;
		CancellationTokenSource _cts = null;
		IWampHost _wampHost = null;
		IWampHostedRealm _wampHostedRealm = null;
		string _wampEndPoint = null, _wampRealm = null;
		const string _wampVersion = "1.2.5.36-beta";
		int _counters = 0;
		#endregion

		#region Start/Stop
		protected override void OnStart(string[] args)
		{
			// initialize log
			string logName = "Application";
			string logSource = "VIEApps WAMP Router";

			if (!EventLog.SourceExists(logSource))
				EventLog.CreateEventSource(logSource, logName);

			this._log = new EventLog(logSource)
			{
				Source = logSource,
				Log = logName
			};

			// prepare
			this._wampEndPoint = ConfigurationManager.AppSettings["EndPoint"];
			if (string.IsNullOrEmpty(this._wampEndPoint))
				this._wampEndPoint = "ws://127.0.0.1:26429/";

			this._wampRealm = ConfigurationManager.AppSettings["Realm"];
			if (string.IsNullOrEmpty(this._wampRealm))
				this._wampRealm = "VIEAppsRealm";

			var useAsync = ConfigurationManager.AppSettings["UseAsync"];

			// open the hosting of the WAMP router
			if (!string.IsNullOrWhiteSpace(useAsync) && useAsync.ToLower().Equals("true"))
			{
				this._cts = new CancellationTokenSource();
				Task.Run(async () =>
				{
					await this.OpenRouterAsync();
				}).ConfigureAwait(false);
			}
			else
				this.OpenRouter();
		}

		protected override void OnStop()
		{
			// stop the hosting of the router
			try
			{
				this.CloseRouter();
				this._log.WriteEntry("VIEApps WAMP Router is closed....");
			}
			catch (Exception ex)
			{
				this._log.WriteEntry("Error occured while stopping VIEApps WAMP Router" + "\r\n\r\n" + "Message: " + ex.Message + " [" + ex.GetType().ToString() + "\r\n\r\n" + "Details: " + ex.StackTrace, EventLogEntryType.Error);
			}

			// close log
			this._log.Close();
			this._log.Dispose();
		}
		#endregion

		#region Open/Close the hosting of the router
		void OpenRouter()
		{
			try
			{
				this._wampHost = new DefaultWampHost(this._wampEndPoint);
				this._wampHostedRealm = this._wampHost.RealmContainer.GetRealmByName(this._wampRealm);
#if DEBUG || SESSIONLOGS
				this._wampHostedRealm.SessionCreated += this.OnSessionCreated;
				this._wampHostedRealm.SessionClosed += this.OnSessionClosed;
#endif
				this._wampHost.Open();
				this._log.WriteEntry("VIEApps WAMP Router is ready for serving..." + "\r\n" + "- Method: SYNC" + "\r\n" + "- End-point: " + this._wampEndPoint + "\r\n" + "- Realm: " + this._wampRealm + "\r\n" + "- PID: " + Process.GetCurrentProcess().Id.ToString() + "\r\n" + "- WampSharp version: " + _wampVersion);
			}
			catch (Exception ex)
			{
				this._log.WriteEntry("Error occured while starting VIEApps WAMP Router" + "\r\n\r\n" + "Message: " + ex.Message + " [" + ex.GetType().ToString() + "\r\n\r\n" + "Details: " + ex.StackTrace, EventLogEntryType.Error);
			}
		}

		async Task OpenRouterAsync()
		{
			using (this._wampHost = new DefaultWampHost(this._wampEndPoint))
			{
				try
				{
					this._wampHostedRealm = this._wampHost.RealmContainer.GetRealmByName(this._wampRealm);
#if DEBUG || SESSIONLOGS
					this._wampHostedRealm.SessionCreated += this.OnSessionCreated;
					this._wampHostedRealm.SessionClosed += this.OnSessionClosed;
#endif
					this._wampHost.Open();
					this._log.WriteEntry("VIEApps WAMP Router is ready for serving..." + "\r\n" + "- Method: ASYNC" + "- End-point: " + this._wampEndPoint + "\r\n" + "- Realm: " + this._wampRealm + "\r\n" + "- PID: " + Process.GetCurrentProcess().Id.ToString() + "\r\n" + "- WampSharp version: " + _wampVersion);

					while (true)
						await Task.Delay(12345, this._cts.Token);
				}
				catch (OperationCanceledException)
				{
				}
				catch (Exception ex)
				{
					this._log.WriteEntry("Error occured while starting VIEApps WAMP Router" + "\r\n\r\n" + "Message: " + ex.Message + " [" + ex.GetType().ToString() + "\r\n\r\n" + "Details: " + ex.StackTrace, EventLogEntryType.Error);
				}
			}
		}

		void CloseRouter()
		{
			if (this._wampHostedRealm != null)
			{
#if DEBUG || SESSIONLOGS
				this._wampHostedRealm.SessionCreated -= this.OnSessionCreated;
				this._wampHostedRealm.SessionClosed -= this.OnSessionClosed;
#endif
				this._wampHostedRealm = null;
			}

			if (this._cts != null)
				this._cts.Cancel();
			else if (this._wampHost != null)
				this._wampHost.Dispose();
		}

#if DEBUG || SESSIONLOGS
		void OnSessionCreated(object sender, WampSessionCreatedEventArgs args)
		{
			this._counters++;
			this._log.WriteEntry("A session is opened..." + "\r\n" + "- Session ID: " + args.SessionId.ToString() + "\r\n" + "- Total of opened sessions: " + this._counters.ToString());
		}

		void OnSessionClosed(object sender, WampSessionCloseEventArgs args)
		{
			this._counters--;
			this._log.WriteEntry("A session is closed..." + "\r\n" + "- Session ID: " + args.SessionId.ToString() + "\r\n" + "- Reason: " + args.Reason + "\r\n" + "- Type: " + args.CloseType.ToString() + "\r\n" + "- Total of opened sessions: " + this._counters.ToString());
		}
#endif
		#endregion

	}

}