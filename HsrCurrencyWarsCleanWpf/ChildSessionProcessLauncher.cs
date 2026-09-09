using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading.Tasks;

namespace HsrCurrencyWarsCleanWpf;

/// <summary>
/// 通过临时计划任务把进程启动到指定的 Windows Child Session 中。
/// 计划任务在启动后立即删除，避免残留。
/// </summary>
internal static class ChildSessionProcessLauncher
{
	private const int TaskActionExecute = 0;

	private const int TaskCreate = 2;

	private const int TaskLogonInteractiveToken = 3;

	private const int TaskRunLevelHighest = 1;

	private const int TaskRunUseSessionId = 4;

	internal static Task LaunchCurrentToolAsync(uint childSessionId)
	{
		string fullPath = Path.GetFullPath(Environment.ProcessPath ?? throw new InvalidOperationException("无法取得当前程序路径。"));
		if (string.Equals(Path.GetFileNameWithoutExtension(fullPath), "dotnet", StringComparison.OrdinalIgnoreCase))
		{
			string value = Assembly.GetEntryAssembly()?.Location ?? throw new InvalidOperationException("无法取得入口程序集路径。");
			return LaunchAsync(childSessionId, fullPath, Quote(value) + " --child-session", AppContext.BaseDirectory);
		}
		return LaunchAsync(childSessionId, ValidateExecutable(fullPath), "--child-session", AppContext.BaseDirectory);
	}

	internal static Task LaunchExecutableAsync(uint childSessionId, string executablePath)
	{
		string text = ValidateExecutable(executablePath);
		return LaunchAsync(childSessionId, text, string.Empty, Path.GetDirectoryName(text) ?? AppContext.BaseDirectory);
	}

	private static Task LaunchAsync(uint childSessionId, string executablePath, string arguments, string workingDirectory)
	{
		return Task.Run(delegate
		{
			LaunchWithTemporaryTask(childSessionId, executablePath, arguments, workingDirectory);
		});
	}

	private static void LaunchWithTemporaryTask(uint childSessionId, string executablePath, string arguments, string workingDirectory)
	{
		uint? num = ChildSessionNativeMethods.TryGetChildSessionId();
		if (num != childSessionId)
		{
			throw new InvalidOperationException($"目标桌面分身已变化。请求会话 {childSessionId}，当前会话 " + (num?.ToString(CultureInfo.InvariantCulture) ?? "无") + "。");
		}
		Type type = Type.GetTypeFromProgID("Schedule.Service") ?? throw new InvalidOperationException("Windows 未提供任务计划程序 COM 服务。");
		string text = $"BetterHSR-ChildSession-{Guid.NewGuid():N}";
		string name;
		using (WindowsIdentity windowsIdentity = WindowsIdentity.GetCurrent())
		{
			name = windowsIdentity.Name;
		}
		object? obj = null;
		object? obj2 = null;
		object? obj3 = null;
		object? obj4 = null;
		object? obj5 = null;
		object? obj6 = null;
		bool flag = false;
		try
		{
			obj = Activator.CreateInstance(type) ?? throw new InvalidOperationException("无法创建任务计划程序 COM 对象。");
			dynamic val = obj;
			val.Connect();
			obj2 = val.GetFolder("\\");
			dynamic val2 = obj2;
			obj3 = val.NewTask(0);
			dynamic val3 = obj3;
			val3.RegistrationInfo.Author = "Better HSR-Currency Wars";
			val3.RegistrationInfo.Description = $"临时启动 {Path.GetFileName(executablePath)} 到 Child Session {childSessionId}";
			val3.Settings.Enabled = true;
			val3.Settings.Hidden = true;
			val3.Settings.AllowDemandStart = true;
			val3.Settings.DisallowStartIfOnBatteries = false;
			val3.Settings.StopIfGoingOnBatteries = false;
			val3.Settings.ExecutionTimeLimit = "PT0S";
			val3.Principal.UserId = name;
			val3.Principal.LogonType = 3;
			val3.Principal.RunLevel = 1;
			obj4 = val3.Actions.Create(0);
			dynamic val4 = obj4;
			val4.Path = executablePath;
			val4.Arguments = arguments;
			val4.WorkingDirectory = workingDirectory;
			obj5 = val2.RegisterTaskDefinition(text, val3, 2, name, null, 3, null);
			flag = true;
			dynamic val5 = obj5;
			obj6 = val5.RunEx(null, 4, checked((int)childSessionId), null);
			if (obj6 == null)
			{
				throw new InvalidOperationException("任务计划程序没有返回运行实例。");
			}
		}
		finally
		{
			if (flag && obj2 != null)
			{
				try
				{
					dynamic val6 = obj2;
					val6.DeleteTask(text, 0);
				}
				catch (COMException)
				{
				}
			}
			ReleaseComObject(obj6);
			ReleaseComObject(obj5);
			ReleaseComObject(obj4);
			ReleaseComObject(obj3);
			ReleaseComObject(obj2);
			ReleaseComObject(obj);
		}
	}

	private static string ValidateExecutable(string path)
	{
		string fullPath = Path.GetFullPath(path);
		if (!File.Exists(fullPath))
		{
			throw new FileNotFoundException("要启动的程序不存在。", fullPath);
		}
		if (!string.Equals(Path.GetExtension(fullPath), ".exe", StringComparison.OrdinalIgnoreCase))
		{
			throw new ArgumentException("只允许启动 .exe 程序。", "path");
		}
		return fullPath;
	}

	private static string Quote(string value)
	{
		return "\"" + value.Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
	}

	private static void ReleaseComObject(object? value)
	{
		if (value != null && Marshal.IsComObject(value))
		{
			Marshal.FinalReleaseComObject(value);
		}
	}
}
