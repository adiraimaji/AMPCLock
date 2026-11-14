using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace AMPCLock;

internal static class Program
{
	private const string MutexName = "AMPCLock.SingleInstance";

	internal static readonly EventWaitHandle LockSignal = new EventWaitHandle(initialState: false, EventResetMode.AutoReset, "AMPCLock.LockSignal");

	[STAThread]
	private static int Main(string[] args)
	{
		try
		{
			return Run(args);
		}
		catch (Exception ex)
		{
			ConfigStore.Log("fatal: " + ex);
			MessageBox.Show("AM PC Lock could not start:\r\n\r\n" + ex.Message, "AM PC Lock", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			return 1;
		}
	}

	private static int Run(string[] args)
	{
		ApplicationConfiguration.Initialize();
		AppConfig appConfig = ConfigStore.Load();
		HashSet<string> hashSet = args.Select((string a) => a.Trim().ToLowerInvariant()).ToHashSet();
		ConfigStore.Log("start args=[" + string.Join(" ", args) + "]");
		if (hashSet.Contains("--selftest"))
		{
			return SelfTest.Run(appConfig);
		}
		if (hashSet.Contains("--unlock"))
		{
			ConfigStore.Log("manual unlock requested; journal restored = " + SystemGuard.RestoreFromJournal());
			return 0;
		}
		if (hashSet.Contains("--preview"))
		{
			ConfigStore.Log("preview: creating form");
			PreviewForm mainForm = new PreviewForm(appConfig);
			ConfigStore.Log("preview: form created, entering message loop");
			Application.Run(mainForm);
			ConfigStore.Log("preview: message loop exited");
			return 0;
		}
		if (hashSet.Contains("--previewlock"))
		{
			ConfigStore.Log("previewlock: showing overlays");
			LockController controller2 = new LockController(appConfig, preview: true);
			controller2.Lock();
			System.Windows.Forms.Timer closer = new System.Windows.Forms.Timer
			{
				Interval = 8000
			};
			closer.Tick += delegate
			{
				closer.Stop();
				ConfigStore.Log("previewlock: closing");
				controller2.Unlock();
				Application.Exit();
			};
			closer.Start();
			Application.Run(new ApplicationContext());
			return 0;
		}
		if (hashSet.Contains("--verifytest"))
		{
			using (VerifyIdentityForm mainForm2 = new VerifyIdentityForm(appConfig))
			{
				Application.Run(mainForm2);
				return 0;
			}
		}
		if (hashSet.Contains("--recoverytest"))
		{
			using (RecoveryForm mainForm3 = new RecoveryForm(new LockController(appConfig, preview: true)))
			{
				Application.Run(mainForm3);
				return 0;
			}
		}
		if (hashSet.Contains("--locktest"))
		{
			int num = 5;
			if (int.TryParse(Environment.GetEnvironmentVariable("AMPCLOCK_LOCKTEST_SECONDS"), out var result) && result >= 2 && result <= 60)
			{
				num = result;
			}
			ConfigStore.Log($"locktest: locking for {num} seconds");
			LockController controller = new LockController(appConfig);
			controller.Lock();
			System.Windows.Forms.Timer release = new System.Windows.Forms.Timer
			{
				Interval = num * 1000
			};
			release.Tick += delegate
			{
				release.Stop();
				ConfigStore.Log("locktest: unlocking");
				controller.Unlock();
				Application.Exit();
			};
			release.Start();
			Application.Run(new ApplicationContext());
			return 0;
		}
		bool createdNew;
		using (new Mutex(initiallyOwned: true, "AMPCLock.SingleInstance", out createdNew))
		{
			if (!createdNew)
			{
				LockSignal.Set();
				return 0;
			}
			if (SystemGuard.RestoreFromJournal())
			{
				ConfigStore.Log("recovered desktop state from a previous run");
			}
			bool startLocked = hashSet.Contains("/lock") || hashSet.Contains("--lock") || hashSet.Contains("/startuplock") || appConfig.LockOnStartup;
			bool openSettingsOnStart = hashSet.Contains("--settings");
			Application.Run(new TrayApp(appConfig, startLocked, openSettingsOnStart));
			return 0;
		}
	}
}
