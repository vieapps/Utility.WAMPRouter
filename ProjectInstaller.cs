using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace net.vieapps.Services.Utility.WAMPRouter
{
	[RunInstaller(true)]
	public partial class ProjectInstaller : Installer
	{
		public ProjectInstaller()
		{
			this.InitializeComponent();

			this.Installers.Add(new ServiceProcessInstaller()
			{
				Account = ServiceAccount.LocalSystem,
				Username = null,
				Password = null
			});

			this.Installers.Add(new ServiceInstaller()
			{
				StartType = ServiceStartMode.Automatic,
				ServiceName = "VIEAppsWAMPRouter",
				DisplayName = "VIEApps WAMP Router",
				Description = "Router for serving messages of RPC and Pub/Sub via WAMP protocol"
			});

			this.AfterInstall += new InstallEventHandler(this.StartServiceAfterInstall);
		}

		void StartServiceAfterInstall(object sender, InstallEventArgs e)
		{
			try
			{
				using (var controller = new ServiceController("VIEAppsWAMPRouter"))
				{
					controller.Start();
				}
			}
			catch { }
		}
	}
}