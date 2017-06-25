using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace net.vieapps.Services.WAMPRouter
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
				DisplayName = "VIEApps NGX WAMP Router",
				Description = "RPC and Pub/Sub routing via WAMP protocol",
				ServiceName = "VIEAppsWAMPRouter"
			});

			this.AfterInstall += new InstallEventHandler(this.StartServiceAfterInstall);
		}

		void StartServiceAfterInstall(object sender, InstallEventArgs e)
		{
			try
			{
				using (var serviceController = new ServiceController("VIEAppsWAMPRouter"))
				{
					serviceController.Start();
				}
			}
			catch { }
		}

	}
}