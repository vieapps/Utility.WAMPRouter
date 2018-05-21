namespace net.vieapps.Services.Utility.WAMPRouter
{
	public partial class ServiceRunner : System.ServiceProcess.ServiceBase
	{
		ServiceComponent Component { get; set; } = new ServiceComponent();

		public ServiceRunner() => this.InitializeComponent();

		protected override void OnStart(string[] args) => this.Component.Start(args);

		protected override void OnStop() => this.Component.Stop();
	}
}