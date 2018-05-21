using System;
using System.ServiceProcess;
using System.Windows.Forms;

namespace net.vieapps.Services.Utility.WAMPRouter
{
	static class Program
	{
		internal static bool AsService { get; set; } = !Environment.UserInteractive;
		internal static ServicePresenter Form { get; set; } = null;

		static void Main(string[] args)
		{
			if (Program.AsService)
				ServiceBase.Run(new ServiceRunner());
			else
			{
				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);

				Program.Form = new ServicePresenter();
				Application.Run(Program.Form);
			}
		}
	}
}