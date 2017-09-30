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
				ServiceName = "VIEApps-WAMP-Router",
				DisplayName = "VIEApps WAMP Router",
				Description = "Router for serving messages of RPC and Pub/Sub via web application messaging protocol (WAMP)"
			});

			this.AfterInstall += new InstallEventHandler(this.StartServiceAfterInstall);
		}

		void StartServiceAfterInstall(object sender, InstallEventArgs e)
		{
			try
			{
				using (var controller = new ServiceController("VIEApps-WAMP-Router"))
				{
					controller.Start();
				}
			}
			catch { }
		}
	}
}