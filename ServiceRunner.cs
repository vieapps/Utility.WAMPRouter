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

		CancellationTokenSource _cts = null;
		IWampHost _wampHost = null;
		IWampHostedRealm _wampHostedRealm = null;
		string _wampEndPoint = null, _wampRealm = null;
		const string _wampVersion = "1.2.5.39-beta";
		int _counters = 0;
		#endregion

		#region Start/Stop
		protected override void OnStart(string[] args)
		{
			// initialize log
			if (Program.AsService)
				Log.InitializeEventLog();

			// prepare
			var useAsync = "false";
			if (args != null && args.Length > 0)
				for (var index = 0; index < args.Length; index++)
				{
					if (args[index].StartsWith("/endpoint:"))
						this._wampEndPoint = args[index].Substring(args[index].IndexOf(":") + 1);
					else if (args[index].StartsWith("/realm:"))
						this._wampRealm = args[index].Substring(args[index].IndexOf(":") + 1);
					else if (args[index].StartsWith("/async:"))
						useAsync = args[index].Substring(args[index].IndexOf(":") + 1);
				}
			else
			{
				this._wampEndPoint = ConfigurationManager.AppSettings["EndPoint"];
				this._wampRealm = ConfigurationManager.AppSettings["Realm"];
				useAsync = ConfigurationManager.AppSettings["UseAsync"];
			}

			// default settings
			if (string.IsNullOrEmpty(this._wampEndPoint))
				this._wampEndPoint = "ws://127.0.0.1:26429/";

			if (string.IsNullOrEmpty(this._wampRealm))
				this._wampRealm = "VIEAppsRealm";

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
				Log.WriteLog("VIEApps WAMP Router is closed....");
			}
			catch (Exception ex)
			{
				Log.WriteLog("Error occured while stopping VIEApps WAMP Router", ex);
			}

			// close log
			if (Program.AsService)
				Log.DisposeEventLog();
		}

		internal void DoStart(string[] args)
		{
			this.OnStart(args);
		}

		internal void DoStop()
		{
			this.OnStop();
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
#else
				if (!Program.AsService)
				{
					this._wampHostedRealm.SessionCreated += this.OnSessionCreated;
					this._wampHostedRealm.SessionClosed += this.OnSessionClosed;
				}
#endif
				this._wampHost.Open();
				Log.WriteLog("VIEApps WAMP Router is ready for serving..." + (Program.AsService ? "\r\n" + "- Service name: " + this.ServiceName : "") + "\r\n" + "- Method: SYNC" + "\r\n" + "- End-point: " + this._wampEndPoint + "\r\n" + "- Realm: " + this._wampRealm + "\r\n" + "- PID: " + Process.GetCurrentProcess().Id.ToString() + "\r\n" + "- WampSharp version: " + _wampVersion);
			}
			catch (Exception ex)
			{
				Log.WriteLog("Error occured while starting VIEApps WAMP Router", ex);
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
#else
					if (!Program.AsService)
					{
						this._wampHostedRealm.SessionCreated += this.OnSessionCreated;
						this._wampHostedRealm.SessionClosed += this.OnSessionClosed;
					}
#endif
					this._wampHost.Open();
					Log.WriteLog("VIEApps WAMP Router is ready for serving..." + (Program.AsService ? "\r\n" + "- Service name: " + this.ServiceName : "") + "\r\n" + "- Method: ASYNC" + "\r\n" + "- End-point: " + this._wampEndPoint + "\r\n" + "- Realm: " + this._wampRealm + "\r\n" + "- PID: " + Process.GetCurrentProcess().Id.ToString() + "\r\n" + "- WampSharp version: " + _wampVersion);

					while (true)
						await Task.Delay(12345, this._cts.Token);
				}
				catch (OperationCanceledException)
				{
				}
				catch (Exception ex)
				{
					Log.WriteLog("Error occured while starting VIEApps WAMP Router", ex);
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

		void OnSessionCreated(object sender, WampSessionCreatedEventArgs args)
		{
			this._counters++;
			Log.WriteLog("\r\n" + "A session is opened..." + "\r\n" + "- Session ID: " + args.SessionId.ToString() + "\r\n" + "- Total of opened sessions: " + this._counters.ToString());
		}

		void OnSessionClosed(object sender, WampSessionCloseEventArgs args)
		{
			this._counters--;
			Log.WriteLog("\r\n" + "A session is closed..." + "\r\n" + "- Session ID: " + args.SessionId.ToString() + "\r\n" + "- Reason: " + args.Reason + "\r\n" + "- Type: " + args.CloseType.ToString() + "\r\n" + "- Total of opened sessions: " + this._counters.ToString());
		}
		#endregion

	}

}