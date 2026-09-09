using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace HsrCurrencyWarsCleanWpf;

/// <summary>
/// 承载 RDP ActiveX 控件的宿主，用于在桌面分身窗口中显示 Child Session 画面。
/// </summary>
internal sealed class RdpActiveXHost : AxHost
{
	[ComImport]
	[Guid("302D8188-0052-4807-806A-362B628F9AC5")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IMsRdpExtendedSettings
	{
		void set_Property([In][MarshalAs(UnmanagedType.BStr)] string propertyName, [In][MarshalAs(UnmanagedType.Struct)] ref object value);

		[return: MarshalAs(UnmanagedType.Struct)]
		object get_Property([In][MarshalAs(UnmanagedType.BStr)] string propertyName);
	}

	private const string RdpClientClsid = "A0C63C30-F08D-4AB4-907C-34905D770C7D";

	private bool _connectStarted;

	internal int ConnectedState
	{
		get
		{
			if (!base.IsHandleCreated)
			{
				return 0;
			}
			return Convert.ToInt32(GetComProperty(GetRequiredOcx(), "Connected"), CultureInfo.InvariantCulture);
		}
	}

	internal RdpActiveXHost()
		: base(RdpClientClsid)
	{
		Dock = DockStyle.Fill;
	}

	internal void ConnectToChildSession(int width = 1920, int height = 1080)
	{
		if (_connectStarted || ConnectedState != 0)
		{
			return;
		}
		object requiredOcx = GetRequiredOcx();
		SetComProperty(requiredOcx, "Server", "localhost");
		SetComProperty(requiredOcx, "DesktopWidth", Math.Clamp(width, 200, 8192));
		SetComProperty(requiredOcx, "DesktopHeight", Math.Clamp(height, 200, 8192));
		SetComProperty(requiredOcx, "ColorDepth", 32);
		SetComProperty(requiredOcx, "ConnectingText", "正在创建星铁货币战争桌面分身...");
		SetComProperty(requiredOcx, "DisconnectedText", "桌面分身已断开");
		object? target = GetComProperty(requiredOcx, "SecuredSettings2") ?? throw new COMException("RDP ActiveX 未返回 SecuredSettings2。");
		SetComProperty(target, "KeyboardHookMode", 1);
		SetComProperty(target, "AudioRedirectionMode", 0);
		object? target2 = GetComProperty(requiredOcx, "AdvancedSettings7") ?? throw new COMException("RDP ActiveX 未返回 AdvancedSettings7。");
		SetComProperty(target2, "RDPPort", ChildSessionNativeMethods.GetConfiguredRdpPort());
		SetComProperty(target2, "EnableCredSspSupport", true);
		SetComProperty(target2, "EnableWindowsKey", 1);
		SetComProperty(target2, "SmartSizing", true);
		object value = true;
		IMsRdpExtendedSettings obj = (IMsRdpExtendedSettings)requiredOcx;
		TrySetExtendedProperty(obj, "EnableZoom", true);
		obj.set_Property("ConnectToChildSession", ref value);
		_connectStarted = true;
		try
		{
			InvokeComMethod(requiredOcx, "Connect");
		}
		catch
		{
			_connectStarted = false;
			throw;
		}
	}

	internal void DisconnectSession()
	{
		if (!base.IsHandleCreated)
		{
			_connectStarted = false;
			return;
		}
		try
		{
			if (_connectStarted || ConnectedState != 0)
			{
				InvokeComMethod(GetRequiredOcx(), "Disconnect");
			}
		}
		finally
		{
			_connectStarted = false;
		}
	}

	private object GetRequiredOcx()
	{
		if (!base.IsHandleCreated)
		{
			_ = base.Handle;
		}
		return GetOcx() ?? throw new InvalidOperationException("RDP ActiveX 控件尚未完成初始化。");
	}

	private static object? GetComProperty(object target, string propertyName)
	{
		return target.GetType().InvokeMember(propertyName, BindingFlags.GetProperty, null, target, null, CultureInfo.InvariantCulture);
	}

	private static void SetComProperty(object target, string propertyName, object value)
	{
		target.GetType().InvokeMember(propertyName, BindingFlags.SetProperty, null, target, new object[1] { value }, CultureInfo.InvariantCulture);
	}

	private static object? InvokeComMethod(object target, string methodName, params object[]? arguments)
	{
		return target.GetType().InvokeMember(methodName, BindingFlags.InvokeMethod, null, target, arguments, CultureInfo.InvariantCulture);
	}

	private static void TrySetExtendedProperty(IMsRdpExtendedSettings settings, string name, object value)
	{
		try
		{
			settings.set_Property(name, ref value);
		}
		catch (COMException)
		{
		}
	}
}
