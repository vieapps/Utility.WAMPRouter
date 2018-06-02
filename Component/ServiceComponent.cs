using System;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Configuration;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using WampSharp.V2;
using WampSharp.V2.Realm;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Fleck;

namespace net.vieapps.Services.Utility.WAMPRouter
{
	public class ServiceComponent
	{
		public const string Powered = "WampSharp v18.6.1.netstandard-2-rxnet-4.0-msgpack-0.9-json-11.0-fleck-1.0.3+rev:2018.06.02";

		public IWampHost Host { get; private set; } = null;

		public IWampHostedRealm HostedRealm { get; private set; } = null;

		public string Address { get; set; } = null;

		public string Realm { get; set; } = null;

		WebSocketServer StatisticsServer { get; set; } = null;

		public ConcurrentDictionary<long, SessionInfo> Sessions { get; } = new ConcurrentDictionary<long, SessionInfo>();

		public Action<Exception> OnError { get; set; } = null;

		public Action OnStarted { get; set; } = null;

		public Action OnStopped { get; set; } = null;

		public Action<SessionInfo> OnSessionCreated { get; set; } = null;

		public Action<SessionInfo> OnSessionClosed { get; set; } = null;

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

			// statistics
			if ("true".Equals(ConfigurationManager.AppSettings["StatisticsWebSocketServer:Enable"] ?? "true"))
			{
				var port = 56429;
				try
				{
					port = Convert.ToInt32(ConfigurationManager.AppSettings["StatisticsWebSocketServer:Port"] ?? "56429");
				}
				catch { }
				this.StatisticsServer = new WebSocketServer($"ws://0.0.0.0:{port}/");
			}

			// start
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
						EndPoint = new IPEndPoint(IPAddress.Parse(uri != null ? uri.Host : "0.0.0.0"), uri != null ? uri.Port : 16429)
					};
					this.Sessions.TryAdd(arguments.SessionId, info);
					this.OnSessionCreated?.Invoke(info);
				};

				this.HostedRealm.SessionClosed += (sender, arguments) =>
				{
					if (this.Sessions.TryRemove(arguments.SessionId, out SessionInfo info))
					{
						info.CloseType = arguments.CloseType.ToString();
						info.CloseReason = arguments.Reason;
					}
					this.OnSessionClosed?.Invoke(info);
				};

				this.StatisticsServer?.Start(socket =>
				{
					socket.OnMessage = message =>
					{
						try
						{
							var json = JObject.Parse(message);
							var command = (json.Value<string>("command") ?? json.Value<string>("Command")) ?? "Unknown";

							if (command.ToLower().Equals("info"))
								socket.Send(this.RouterInfo.ToString(Formatting.None));

							else if (command.ToLower().Equals("sessions"))
								socket.Send(this.SessionsInfo.ToString(Formatting.None));

							else if (command.ToLower().Equals("connections"))
								socket.Send(new JObject
								{
									{ "Connections", this.Sessions.Count }
								}.ToString(Formatting.None));

							else if (command.ToLower().Equals("update"))
							{
								if (this.Sessions.TryGetValue(json.Value<long>("SessionID"), out SessionInfo sessionInfo))
								{
									sessionInfo.Name = json.Value<string>("Name");
									sessionInfo.URI = json.Value<string>("URI");
									sessionInfo.Description = json.Value<string>("Description");
								}
							}

							else
								socket.Send(new JObject
								{
									{ "Error", $"Unknown command [{message}]" }
								}.ToString(Formatting.None));
						}
						catch (Exception ex)
						{
							socket.Send(new JObject
							{
								{ "Error", $"Bad command [{message}] => {ex.Message}" }
							}.ToString(Formatting.None));
						}
					};
				});

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
				this.StatisticsServer?.Dispose();
				this.OnStopped?.Invoke();
			}
			catch (Exception ex)
			{
				this.OnError?.Invoke(ex);
			}
		}

		public JObject RouterInfo
			=> new JObject
			{
				{ "URI", $"{this.Address}{this.Realm}" },
				{ "HostedRealmSessionID", $"{this.HostedRealm.SessionId}" },
				{ "Platform", $"{RuntimeInformation.FrameworkDescription} @ {(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows" : RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "Linux" : "Other OS")} {RuntimeInformation.OSArchitecture} ({RuntimeInformation.OSDescription.Trim()})" },
				{ "Powered", ServiceComponent.Powered },
				{ "ProcessID", $"{Process.GetCurrentProcess().Id}" },
				{ "WorkingMode", Environment.UserInteractive ? "Interactive app" : "Background service" },
				{ "StatisticsServer", $"{this.StatisticsServer != null}".ToLower() },
				{ "StatisticsServerPort", this.StatisticsServer != null ? this.StatisticsServer.Port : 56429 }
			};

		public string RouterInfoString
		{
			get
			{
				var json = this.RouterInfo;
				return
					$"- Listening URI: {json.Value<string>("URI")}" + "\r\n\t" +
					$"- Hosted Realm Session ID: {json.Value<string>("HostedRealmSessionID")}" + "\r\n\t" +
					$"- Platform: {json.Value<string>("Platform")}" + "\r\n\t" +
					$"- Powered: {json.Value<string>("Powered")}" + "\r\n\t" +
					$"- Process ID: {json.Value<string>("ProcessID")}" + "\r\n\t" +
					$"- Working Mode: {json.Value<string>("WorkingMode")}" + "\r\n\t" +
					$"- Statistics Server: {json.Value<string>("StatisticsServer")}" + "\r\n\t" +
					$"- Statistics Server Port: {json.Value<long>("StatisticsServerPort")}";
			}
		}

		public JObject SessionsInfo
		{
			get
			{
				var sessions = new JArray();
				this.Sessions.Values.ToList().ForEach(info => sessions.Add(info.ToJson()));
				return new JObject
				{
					{ "Total", this.Sessions.Count },
					{ "Sessions", sessions }
				};
			}
		}

		public string SessionsInfoString
		{
			get
			{
				var json = this.SessionsInfo;
				var sessions = json["Sessions"] as JArray;
				var info = $"Total of sessions: {json.Value<long>("Total")}";
				if (sessions.Count > 0)
				{
					info += "\r\n" + "Details:";
					foreach (JObject session in sessions)
						info += "\r\n\t" + $"Session ID: {session.Value<long>("SessionID")} - Connection Info: {session.Value<string>("ConnectionID")} - {session.Value<string>("EndPoint")})";
				}
				return info;
			}
		}

		~ServiceComponent()
		{
			try
			{
				this.Stop();
			}
			catch { }
		}
	}

	public class SessionInfo
	{
		public long SessionID { get; internal set; }
		public Guid ConnectionID { get; internal set; }
		public IPEndPoint EndPoint { get; internal set; }
		public string CloseType { get; internal set; }
		public string CloseReason { get; internal set; }
		public string Name { get; internal set; }
		public string URI { get; internal set; }
		public string Description { get; internal set; }
		internal JObject ToJson()
			=> new JObject
			{
				{ "SessionID", this.SessionID },
				{ "ConnectionID", $"{this.ConnectionID}" },
				{ "EndPoint", $"{this.EndPoint}" },
				{ "Name", this.Name },
				{ "URI", this.URI },
				{ "Description", this.Description }
			};
	}
}