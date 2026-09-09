using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Velopack;
using Velopack.Locators;

namespace HsrCurrencyWarsCleanWpf;

public class App : Application
{
	public static bool IsChildSessionInstance { get; private set; }

	public static bool OpenDesktopCloneOnStartup { get; private set; }

	/// <summary>
	/// 配置目录。安装版写入 Velopack 的 UserData，避免升级时覆盖用户配置；便携版使用程序目录。
	/// </summary>
	public static string ConfigDirectory
	{
		get
		{
			if (VelopackLocator.IsCurrentSet && !VelopackLocator.Current.IsPortable && !string.IsNullOrWhiteSpace(VelopackLocator.Current.RootAppDir))
			{
				string text = Path.Combine(VelopackLocator.Current.RootAppDir, "UserData");
				Directory.CreateDirectory(text);
				string text2 = Path.Combine(text, "config.clean.json");
				string text3 = Path.Combine(AppContext.BaseDirectory, "config.clean.json");
				if (!File.Exists(text2) && File.Exists(text3))
				{
					File.Copy(text3, text2, overwrite: false);
				}
				return text;
			}
			return AppContext.BaseDirectory;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "10.0.9.0")]
	public void InitializeComponent()
	{
		base.StartupUri = new Uri("MainWindow.xaml", UriKind.Relative);
	}

	[STAThread]
	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "10.0.9.0")]
	public static void Main()
	{
		VelopackApp.Build().Run();
		string[] commandLineArgs = Environment.GetCommandLineArgs();
		IsChildSessionInstance = Array.Exists(commandLineArgs, (string arg) => string.Equals(arg, "--child-session", StringComparison.OrdinalIgnoreCase));
		OpenDesktopCloneOnStartup = Array.Exists(commandLineArgs, (string arg) => string.Equals(arg, "--desktop-clone", StringComparison.OrdinalIgnoreCase));
		App app = new App();
		app.InitializeComponent();
		app.Run();
	}
}
