using System;
using System.Windows.Forms;
using System.Diagnostics;

namespace net.vieapps.Services.Utility.WAMPRouter
{
	public partial class ServicePresenter : Form
	{
		ServiceComponent _component = null;

		public ServicePresenter()
		{
			this.InitializeComponent();
		}

		private void ServicePresenter_Load(object sender, EventArgs e)
		{
			// prepare arguments
			var args = Environment.GetCommandLineArgs();
			if (args != null && args.Length > 1)
			{
				var tmp = args;
				args = new string[args.Length - 1];
				tmp.CopyTo(args, 1);
			}
			else
				args = new string[] { };

			this.CommandLine.Text = "VIEApps.WAMPRouter.exe " + string.Join(" ", args).Trim();
			this.CommandLine.SelectionStart = this.CommandLine.TextLength;

			// update logs
			this.UpdateLogs("The VIEApps WAMP Router is now running as a Windows desktop app" + "\r\n");
			this.UpdateLogs("Args syntax: VIEApps.WAMPRouter.exe /address:<ws://ip:port> /realm:<realm-name>" + "\r\n");
			this.UpdateLogs("To install as a Windows service, use the InstallUtil.exe in the command prompt as \"InstallUtil /i VIEApps.WAMPRouter.exe\" (with Administrator privileges)");
			this.UpdateLogs("--------------------------------------------------------------------" + "\r\n");
			this.UpdateLogs("OUTPUT:" + "\r\n");

			// start
			try
			{
				this._component = new ServiceComponent();
				this._component.Start(args);
			}
			catch (Exception ex)
			{
				this.UpdateLogs("Error occurred while starting: " + ex.Message + "\r\n\r\nStack: " + ex.StackTrace);
			}
		}

		private void ServicePresenter_FormClosed(object sender, FormClosedEventArgs e)
		{
			if (this._component != null)
				this._component.Stop();
		}

		public delegate void UpdateLogsDelegator(string logs);

		internal void UpdateLogs(string logs)
		{
			if (base.InvokeRequired)
			{
				UpdateLogsDelegator method = new UpdateLogsDelegator(this.UpdateLogs);
				base.Invoke(method, new object[] { logs });
			}
			else
			{
				this.Logs.AppendText(logs + "\r\n");
				this.Logs.SelectionStart = this.Logs.TextLength;
				this.Logs.ScrollToCaret();
			}
		}
	}
}