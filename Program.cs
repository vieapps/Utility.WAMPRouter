using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Diagnostics;

namespace net.vieapps.Services.WAMPRouter
{
	static class Program
	{

		static void Main()
		{
			ServiceBase.Run(new ServiceBase[] { new ServiceRunner() });
		}

	}
}