#region Related components
using System;
using System.Configuration;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using WampSharp.V2;
using WampSharp.V2.Realm;
#endregion

namespace net.vieapps.Services.Utility.WAMPRouter
{
	public class ServiceComponent
	{
		public ServiceComponent() { }

		IWampHost _wampHost = null;
		IWampHostedRealm _wampHostedRealm = null;

		string _wampAddress = null, _wampRealm = null, _componentVersion = "1.2.7.45b.netstandard-2+rev:2017.11.25";
		int _connectionCounters = 0;

		internal void Start(string[] args)
		{
			// initialize log
			if (Program.AsService)
				Helper.InitializeLog();

			// prepare
			if (args != null && args.Length > 0)
				for (var index = 0; index < args.Length; index++)
				{
					if (args[index].StartsWith("/address:"))
						this._wampAddress = args[index].Substring(args[index].IndexOf(":") + 1);
					else if (args[index].StartsWith("/realm:"))
						this._wampRealm = args[index].Substring(args[index].IndexOf(":") + 1);
				}
			else
			{
				this._wampAddress = ConfigurationManager.AppSettings["Address"];
				this._wampRealm = ConfigurationManager.AppSettings["Realm"];
			}

			// default settings
			if (string.IsNullOrWhiteSpace(this._wampAddress))
				this._wampAddress = "ws://127.0.0.1:16429/";
			else if (!this._wampAddress.EndsWith("/"))
				this._wampAddress += "/";

			if (string.IsNullOrWhiteSpace(this._wampRealm))
				this._wampRealm = "VIEAppsRealm";

			// open the hosting of the WAMP router
			try
			{
				this._wampHost = new DefaultWampHost(this._wampAddress);
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
				Helper.WriteLog(
					$"VIEApps WAMP Router is ready for serving [PID: {Process.GetCurrentProcess().Id}]" + "\r\n" +
					$"- URI: {this._wampAddress}{this._wampRealm}\r\n" + 
					$"- WampSharp: v{this._componentVersion}"
				);
			}
			catch (Exception ex)
			{
				Helper.WriteLog("Error occured while starting VIEApps WAMP Router", ex);
			}
		}

		void OnSessionCreated(object sender, WampSessionCreatedEventArgs args)
		{
			this._connectionCounters++;
			Helper.WriteLog(
				"A session is opened..." + "\r\n" + 
				$"- Session ID: {args.SessionId}\r\n" + 
				$"- Total of connections: {this._connectionCounters.ToString("###,##0")}"
			);
		}

		void OnSessionClosed(object sender, WampSessionCloseEventArgs args)
		{
			this._connectionCounters--;
			if (this._connectionCounters < 0)
				this._connectionCounters = 0;
			Helper.WriteLog(
				"A session is closed..." + "\r\n" +
				$"- Session ID: {args.SessionId}\r\n" +
				$"- Reason: {args.Reason}\r\n" + 
				$"- Type: {args.CloseType}\r\n" +
				$"- Total of connections: {this._connectionCounters.ToString("###,##0")}"
			);
		}

		internal void Stop()
		{
			// stop the hosting of the router
			try
			{
				if (this._wampHostedRealm != null)
				{
#if DEBUG || SESSIONLOGS
				this._wampHostedRealm.SessionCreated -= this.OnSessionCreated;
				this._wampHostedRealm.SessionClosed -= this.OnSessionClosed;
#else
					if (!Program.AsService)
					{
						this._wampHostedRealm.SessionCreated -= this.OnSessionCreated;
						this._wampHostedRealm.SessionClosed -= this.OnSessionClosed;
					}
#endif
					this._wampHostedRealm = null;
				}

				this._wampHost?.Dispose();

				Helper.WriteLog("VIEApps WAMP Router is closed....");
			}
			catch (Exception ex)
			{
				Helper.WriteLog("Error occured while stopping VIEApps WAMP Router", ex);
			}

			// close log
			if (Program.AsService)
				Helper.DisposeLog();
		}
	}
}