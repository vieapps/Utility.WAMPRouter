using System;
using System.ServiceProcess;

namespace net.vieapps.Services.Utility.WAMPRouter
{
	public partial class ServiceRunner : ServiceBase
	{
		public ServiceRunner()
		{
			this.InitializeComponent();
		}

		ServiceComponent _component = new ServiceComponent();

		protected override void OnStart(string[] args)
		{
			this._component.Start(args);
		}

		protected override void OnStop()
		{
			this._component.Stop();
		}
	}
}