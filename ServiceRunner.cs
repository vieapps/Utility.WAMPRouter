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

namespace net.vieapps.Services.WAMPRouter
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
		string _wampEndPoint = null, _wampRealm = null;
		const string _wampVersion = "1.2.5.36-beta";
		#endregion

		#region Start/Stop
		protected override void OnStart(string[] args)
		{
			// initialize log
			string logName = "Application";
			string logSource = "VIEApps NGX WAMP Router";

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

			// open the hosting of the WAMP router
			var useAsync = ConfigurationManager.AppSettings["UseAsync"];
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
				if (this._cts != null)
					this._cts.Cancel();
				else
					this.CloseRouter();
				this._log.WriteEntry("VIEApps NGX WAMP Router is closed....");
			}
			catch (Exception ex)
			{
				this._log.WriteEntry("Error occured while stopping VIEApps NGX WAMP Router" + "\r\n\r\n" + "Message: " + ex.Message + " [" + ex.GetType().ToString() + "\r\n\r\n" + "Details: " + ex.StackTrace, EventLogEntryType.Error);
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
				var hostedRealm = this._wampHost.RealmContainer.GetRealmByName(this._wampRealm);
				this._wampHost.Open();
				this._log.WriteEntry("VIEApps NGX WAMP Router is ready for serving..." + "\r\n" + "- Method: SYNC" + "\r\n" + "- End-point: " + this._wampEndPoint + "\r\n" + "- Realm: " + this._wampRealm + "\r\n" + "- PID: " + Process.GetCurrentProcess().Id.ToString() + "\r\n" + "- WampSharp version: " + _wampVersion);
			}
			catch (Exception ex)
			{
				this._log.WriteEntry("Error occured while starting VIEApps NGX WAMP Router" + "\r\n\r\n" + "Message: " + ex.Message + " [" + ex.GetType().ToString() + "\r\n\r\n" + "Details: " + ex.StackTrace, EventLogEntryType.Error);
			}
		}

		void CloseRouter()
		{
			if (this._wampHost != null)
				this._wampHost.Dispose();
		}

		async Task OpenRouterAsync()
		{
			using (this._wampHost = new DefaultWampHost(this._wampEndPoint))
			{
				try
				{
					var hostedRealm = this._wampHost.RealmContainer.GetRealmByName(this._wampRealm);
					this._wampHost.Open();
					this._log.WriteEntry("VIEApps NGX WAMP Router is ready for serving..." + "\r\n" + "- Method: ASYNC" + "- End-point: " + this._wampEndPoint + "\r\n" + "- Realm: " + this._wampRealm + "\r\n" + "- PID: " + Process.GetCurrentProcess().Id.ToString() + "\r\n" + "- WampSharp version: " + _wampVersion);

					while (true)
						await Task.Delay(456, this._cts.Token);
				}
				catch (OperationCanceledException)
				{
				}
				catch (Exception ex)
				{
					this._log.WriteEntry("Error occured while starting VIEApps NGX WAMP Router" + "\r\n\r\n" + "Message: " + ex.Message + " [" + ex.GetType().ToString() + "\r\n\r\n" + "Details: " + ex.StackTrace, EventLogEntryType.Error);
				}
			}
		}
		#endregion

	}

}