using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace TranscriptionMedia;

public class App : Application
{
	private Mutex? instance;

	[DllImport("kernel32.dll")]
	private static extern bool SetDefaultDllDirectories(uint flags);

	protected override async void OnStartup(StartupEventArgs e)
	{
		SetDefaultDllDirectories(4096u);
		base.OnStartup(e);
		if (Enumerable.Contains(e.Args, "--worker"))
		{
			await EngineWorker.Run(Enumerable.Contains(e.Args, "--cpu"), Enumerable.Contains(e.Args, "--integrated"));
			Shutdown();
			return;
		}
		string text = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(AppContext.BaseDirectory + Environment.UserName)));
		instance = new Mutex(initiallyOwned: true, "Local\\TranscriptionMedia-" + text, out var createdNew);
		if (!createdNew)
		{
			MessageBox.Show("TranscriptionMedia ya está abierta.");
			Shutdown();
			return;
		}
		Window window = (MainWindow = new MainWindow());
		MainWindow window3 = (MainWindow)window;
		window3.Show();

#if AUDIT_DIAGNOSTICS
        if(e.Args.Contains("--audit-test")){await Task.Delay(500);await window3.AuditTest(e.Args.Last());return;}
if (Enumerable.Contains(e.Args, "--flow-test"))
		{
			await Task.Delay(500);
			await window3.FlowTest(e.Args.Last());
		}
		if (Enumerable.Contains(e.Args, "--ui-test"))
		{
			await Task.Delay(500);
			window3.UiSmokeTest(e.Args.Last());
		}
		if (Enumerable.Contains(e.Args, "--self-test"))
		{
			await Task.Delay(500);
			await window3.SelfTest(e.Args.Last());
		}
#endif
	}

	[STAThread]
	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "10.0.6.0")]
	public static void Main()
	{
		new App().Run();
	}
}
