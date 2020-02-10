using System;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Web
{
	public class AcumaticaRole : RoleEntryPoint
	{
		public override bool OnStart()
		{
			System.Diagnostics.Trace.Write("Role was started");

			//Enabling Diagnostic Monitor
			DiagnosticMonitorConfiguration config = DiagnosticMonitor.GetDefaultInitialConfiguration();
			//config.DiagnosticInfrastructureLogs.ScheduledTransferLogLevelFilter = LogLevel.Error;
			//config.DiagnosticInfrastructureLogs.ScheduledTransferPeriod = TimeSpan.FromMinutes(10);
			//config.WindowsEventLog.DataSources.Add("Application!*");
			//config.WindowsEventLog.ScheduledTransferPeriod = TimeSpan.FromMinutes(10);

			DiagnosticMonitor.Start("DiagnosticsConnectionString", config);

			//Enabling Crash Dumps
			//Microsoft.WindowsAzure.Diagnostics.CrashDumps.EnableCollection(false);

			// For information on handling configuration changes
			RoleEnvironment.Changing += RoleEnvironmentChanging;

			return base.OnStart();
		}

		private void RoleEnvironmentChanging(object sender, RoleEnvironmentChangingEventArgs e)
		{
			foreach (RoleEnvironmentChange change in e.Changes)
			{
				if (change.GetType() == typeof(RoleEnvironmentConfigurationSettingChange))
				{
					e.Cancel = true;
				}
			}
		}
	}
}
