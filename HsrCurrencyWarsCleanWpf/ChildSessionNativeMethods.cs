using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace HsrCurrencyWarsCleanWpf;

/// <summary>
/// Windows Child Session 相关原生方法封装。
/// </summary>
internal static class ChildSessionNativeMethods
{
	private const uint NoChildSessionId = uint.MaxValue;

	private static readonly nint CurrentServerHandle = IntPtr.Zero;

	[DllImport("wtsapi32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool WTSEnableChildSessions([MarshalAs(UnmanagedType.Bool)] bool enable);

	[DllImport("wtsapi32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool WTSIsChildSessionsEnabled([MarshalAs(UnmanagedType.Bool)] out bool enabled);

	[DllImport("wtsapi32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool WTSGetChildSessionId(out uint sessionId);

	[DllImport("wtsapi32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool WTSLogoffSession(nint server, uint sessionId, [MarshalAs(UnmanagedType.Bool)] bool wait);

	internal static void EnableChildSessions()
	{
		if (!WTSEnableChildSessions(enable: true))
		{
			throw CreateLastWin32Exception("启用 Windows Child Session 失败");
		}
	}

	internal static bool AreChildSessionsEnabled()
	{
		if (!WTSIsChildSessionsEnabled(out var enabled))
		{
			throw CreateLastWin32Exception("读取 Windows Child Session 状态失败");
		}
		return enabled;
	}

	internal static uint? TryGetChildSessionId()
	{
		if (!WTSGetChildSessionId(out var sessionId) || sessionId == uint.MaxValue)
		{
			return null;
		}
		return sessionId;
	}

	internal static uint LogoffChildSession(bool wait = true)
	{
		uint? num = TryGetChildSessionId();
		if (!num.HasValue)
		{
			throw new InvalidOperationException("当前没有可注销的桌面分身会话。");
		}
		if (!WTSLogoffSession(CurrentServerHandle, num.Value, wait))
		{
			throw CreateLastWin32Exception($"注销 Child Session {num.Value} 失败");
		}
		return num.Value;
	}

	internal static int GetConfiguredRdpPort()
	{
		using RegistryKey? registryKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Terminal Server\\WinStations\\RDP-Tcp");
		object? obj = registryKey?.GetValue("PortNumber");
		return (obj == null) ? 3389 : Convert.ToInt32(obj, CultureInfo.InvariantCulture);
	}

	internal static bool IsRdpWrapperEnabled()
	{
		using RegistryKey? registryKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Services\\TermService\\Parameters");
		return (Convert.ToString(registryKey?.GetValue("ServiceDll"), CultureInfo.InvariantCulture) ?? string.Empty).Contains("rdpwrap.dll", StringComparison.OrdinalIgnoreCase);
	}

	private static Win32Exception CreateLastWin32Exception(string operation)
	{
		int lastWin32Error = Marshal.GetLastWin32Error();
		return new Win32Exception(lastWin32Error, $"{operation}（Win32 {lastWin32Error}）。");
	}
}
