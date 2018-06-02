using System;
using System.Net;
using System.Reflection;
using System.Configuration;
using System.Collections.Concurrent;
using WampSharp.V2;
using WampSharp.V2.Realm;

namespace net.vieapps.Services.Utility.WAMPRouter
{
	public class ServiceComponent
	{

		#region Properties
		public string ComponentInfo { get; } = "WampSharp v18.6.1.netstandard-2-rxnet-4.0-msgpack-0.9-json-11.0-fleck-1.0.3+rev:2018.06.02";

		public IWampHost Host { get; private set; } = null;

		public IWampHostedRealm HostedRealm { get; private set; } = null;

		public string Address { get; set; } = null;

		public string Realm { get; set; } = null;

		public ConcurrentDictionary<long, SessionInfo> Connections { get; } = new ConcurrentDictionary<long, SessionInfo>();
		#endregion

		#region Event handlers
		public Action<Exception> OnError { get; set; } = (ex) => { };

		public Action OnStarted { get; set; } = () => { };

		public Action OnStopped { get; set; } = () => { };

		public Action<SessionInfo> OnSessionCreated { get; set; } = info => { };

		public Action<SessionInfo> OnSessionClosed { get; set; } = info => { };
		#endregion

		public void Start(string[] args)
		{
			// prepare
			if (string.IsNullOrWhiteSpace(this.Address) || string.IsNullOrWhiteSpace(this.Realm))
			{
				if (args != null && args.Length > 0)
					for (var index = 0; index < args.Length; index++)
					{
						if (args[index].StartsWith("/address:"))
							this.Address = args[index].Substring(args[index].IndexOf(":") + 1).Trim();
						else if (args[index].StartsWith("/realm:"))
							this.Realm = args[index].Substring(args[index].IndexOf(":") + 1).Trim();
					}
				else
				{
					this.Address = ConfigurationManager.AppSettings["Address"];
					this.Realm = ConfigurationManager.AppSettings["Realm"];
				}

				// default settings
				if (string.IsNullOrWhiteSpace(this.Address))
					this.Address = "ws://0.0.0.0:16429/";
				else if (!this.Address.EndsWith("/"))
					this.Address += "/";

				if (string.IsNullOrWhiteSpace(this.Realm))
					this.Realm = "VIEAppsRealm";
			}

			// open the hosting of the WAMP router
			try
			{
				this.Host = new DefaultWampHost(this.Address);

				this.HostedRealm = this.Host.RealmContainer.GetRealmByName(this.Realm);

				this.HostedRealm.SessionCreated += (sender, arguments) =>
				{
					var details = arguments.HelloDetails.TransportDetails;
					var type = details.GetType();
					var property = type.GetProperty("Peer", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
					var peer = property != null && property.CanRead
						? property.GetValue(details)
						: null;
					var uri = peer != null ? new Uri(peer as string) : null;
					property = type.GetProperty("Id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
					var id = property != null && property.CanRead
						? property.GetValue(details)
						: null;
					var info = new SessionInfo
					{
						SessionID = arguments.SessionId,
						ConnectionID = id != null ? (Guid)id : Guid.NewGuid(),
						EndPoint = new IPEndPoint(IPAddress.Parse(uri != null ? uri.Host : "0.0.0.0"), uri != null ? uri.Port : 16429),
						State = "Open"
					};
					this.Connections.TryAdd(arguments.SessionId, info);
					this.OnSessionCreated?.Invoke(info);
				};

				this.HostedRealm.SessionClosed += (sender, arguments) =>
				{
					if (this.Connections.TryRemove(arguments.SessionId, out SessionInfo info))
					{
						info.State = "Close";
						info.CloseType = arguments.CloseType.ToString();
						info.CloseReason = arguments.Reason;
					}
					this.OnSessionClosed?.Invoke(info);
				};

				this.Host.Open();
				this.OnStarted?.Invoke();
			}
			catch (Exception ex)
			{
				this.OnError?.Invoke(ex);
			}
		}

		public void Stop()
		{
			try
			{
				this.HostedRealm = null;
				this.Host?.Dispose();
				this.OnStopped?.Invoke();
			}
			catch (Exception ex)
			{
				this.OnError?.Invoke(ex);
			}
		}
	}

	public class SessionInfo
	{
		public long SessionID { get; internal set; }
		public Guid ConnectionID { get; internal set; }
		public IPEndPoint EndPoint { get; internal set; }
		public string State { get; internal set; }
		public string CloseType { get; internal set; }
		public string CloseReason { get; internal set; }
		public string Name { get; internal set; }
		public string URI { get; internal set; }
	}
}