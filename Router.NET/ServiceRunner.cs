namespace net.vieapps.Services.Utility.WAMPRouter
{
	public partial class ServiceRunner : System.ServiceProcess.ServiceBase
	{
		public ServiceRunner() => this.InitializeComponent();

		protected override void OnStart(string[] args) => Program.Start(args);

		protected override void OnStop() => Program.Stop();
	}
}