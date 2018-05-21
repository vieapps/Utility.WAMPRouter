#region Related components
using System;
using System.Configuration;
using System.Diagnostics;
using WampSharp.V2;
using WampSharp.V2.Realm;
#endregion

namespace net.vieapps.Services.Utility.WAMPRouter
{
	public class ServiceComponent
	{
		readonly string ComponentVersion = "18.5.1.netstandard-2+rev:2018.05.21";

		IWampHost WampHost { get; set; } = null;

		IWampHostedRealm WampHostedRealm { get; set; } = null;

		string WampAddress { get; set; } = null;

		string WampRealm { get; set; } = null;

		int ConnectionCounters { get; set; } = 0;

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
						this.WampAddress = args[index].Substring(args[index].IndexOf(":") + 1).Trim();
					else if (args[index].StartsWith("/realm:"))
						this.WampRealm = args[index].Substring(args[index].IndexOf(":") + 1).Trim();
				}
			else
			{
				this.WampAddress = ConfigurationManager.AppSettings["Address"];
				this.WampRealm = ConfigurationManager.AppSettings["Realm"];
			}

			// default settings
			if (string.IsNullOrWhiteSpace(this.WampAddress))
				this.WampAddress = "ws://0.0.0.0:16429/";
			else if (!this.WampAddress.EndsWith("/"))
				this.WampAddress += "/";

			if (string.IsNullOrWhiteSpace(this.WampRealm))
				this.WampRealm = "VIEAppsRealm";

			// open the hosting of the WAMP router
			try
			{
				this.WampHost = new DefaultWampHost(this.WampAddress);
				this.WampHostedRealm = this.WampHost.RealmContainer.GetRealmByName(this.WampRealm);

#if DEBUG || SESSIONLOGS
				this.WampHostedRealm.SessionCreated += this.OnSessionCreated;
				this.WampHostedRealm.SessionClosed += this.OnSessionClosed;
#else
				if (!Program.AsService)
				{
					this.WampHostedRealm.SessionCreated += this.OnSessionCreated;
					this.WampHostedRealm.SessionClosed += this.OnSessionClosed;
				}
#endif

				this.WampHost.Open();
				Helper.WriteLog(
					$"VIEApps WAMP Router is ready for serving [PID: {Process.GetCurrentProcess().Id}]" +
					$"\r\n- URI: {this.WampAddress}{this.WampRealm}" + 
					$"\r\n- WampSharp: v{this.ComponentVersion}"
				);
			}
			catch (Exception ex)
			{
				Helper.WriteLog("Error occured while starting VIEApps WAMP Router", ex);
			}
		}

		void OnSessionCreated(object sender, WampSessionCreatedEventArgs args)
		{
			this.ConnectionCounters++;
			Helper.WriteLog(
				"A session is opened..." + 
				$"\r\n- Session ID: {args.SessionId}" + 
				$"\r\n- Total of connections: {this.ConnectionCounters.ToString("###,##0")}"
			);
		}

		void OnSessionClosed(object sender, WampSessionCloseEventArgs args)
		{
			this.ConnectionCounters--;
			if (this.ConnectionCounters < 0)
				this.ConnectionCounters = 0;
			Helper.WriteLog(
				"A session is closed..." +
				$"\r\n- Session ID: {args.SessionId}" +
				$"\r\n- Reason: {args.Reason}" + 
				$"\r\n- Type: {args.CloseType}" +
				$"\r\n- Total of connections: {this.ConnectionCounters.ToString("###,##0")}"
			);
		}

		internal void Stop()
		{
			// stop the hosting of the router
			try
			{
				if (this.WampHostedRealm != null)
				{
#if DEBUG || SESSIONLOGS
				this.WampHostedRealm.SessionCreated -= this.OnSessionCreated;
				this.WampHostedRealm.SessionClosed -= this.OnSessionClosed;
#else
					if (!Program.AsService)
					{
						this.WampHostedRealm.SessionCreated -= this.OnSessionCreated;
						this.WampHostedRealm.SessionClosed -= this.OnSessionClosed;
					}
#endif
					this.WampHostedRealm = null;
				}

				this.WampHost?.Dispose();
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