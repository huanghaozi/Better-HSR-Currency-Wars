using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using HsrCurrencyWarsCleanWpf.Services;
using Microsoft.Win32;

namespace HsrCurrencyWarsCleanWpf;

internal sealed class DesktopCloneWindow : Window
{
	private readonly RdpActiveXHost _rdpHost = new RdpActiveXHost();
	private readonly TextBlock _statusText = new TextBlock();
	private readonly TextBlock _statusTitle = new TextBlock();
	private readonly Border _statusDot = new Border();
	private readonly Border _statusBadge = new Border();
	private readonly Button _launchToolButton = new Button();
	private readonly Button _launchProgramButton = new Button();
	private readonly Button _logoffButton = new Button();
	private readonly DispatcherTimer _statusTimer;
	internal DesktopCloneWindow()
	{
		Title = "桌面分身（测试） - Better HSR-Currency Wars";
		Width = 1280;
		Height = 840;
		MinWidth = 980;
		MinHeight = 680;
		WindowStartupLocation = WindowStartupLocation.CenterOwner;
		Background = BrushFromHex("#F3F7FC");
		FontFamily = new FontFamily("Microsoft YaHei UI");
		UseLayoutRounding = true;
		SnapsToDevicePixels = true;

		Grid root = new Grid();
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

		Border headerCard = CreateCard(new Thickness(16, 14, 16, 12));
		Grid header = new Grid { Margin = new Thickness(22, 16, 20, 16) };
		header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		StackPanel heading = new StackPanel();
		heading.Children.Add(new TextBlock
		{
			Text = "桌面分身",
			FontSize = 24,
			FontWeight = FontWeights.Bold,
			Foreground = BrushFromHex("#102A43")
		});
		heading.Children.Add(new TextBlock
		{
			Text = "在独立的 Windows 会话中运行游戏和自动化，不占用主桌面的鼠标与键盘",
			Margin = new Thickness(0, 5, 0, 0),
			FontSize = 13,
			Foreground = BrushFromHex("#6B7F93")
		});
		header.Children.Add(heading);

		_statusDot.Width = 9;
		_statusDot.Height = 9;
		_statusDot.CornerRadius = new CornerRadius(5);
		_statusDot.Margin = new Thickness(0, 0, 8, 0);
		_statusTitle.FontSize = 13;
		_statusTitle.FontWeight = FontWeights.SemiBold;
		StackPanel badgeContent = new StackPanel { Orientation = Orientation.Horizontal };
		badgeContent.Children.Add(_statusDot);
		badgeContent.Children.Add(_statusTitle);
		_statusBadge.Padding = new Thickness(13, 8, 13, 8);
		_statusBadge.CornerRadius = new CornerRadius(18);
		_statusBadge.VerticalAlignment = VerticalAlignment.Center;
		_statusBadge.Child = badgeContent;
		Grid.SetColumn(_statusBadge, 1);
		header.Children.Add(_statusBadge);
		headerCard.Child = header;
		root.Children.Add(headerCard);

		Border actionCard = CreateCard(new Thickness(16, 0, 16, 12));
		Grid actionGrid = new Grid { Margin = new Thickness(18, 15, 18, 15) };
		actionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		actionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		WrapPanel buttons = new WrapPanel { VerticalAlignment = VerticalAlignment.Center };
		Button reconnectButton = CreateButton("重新连接", "↻", Connect_Click, "#4BA9E8", "#FFFFFF", "#4BA9E8");
		ConfigureButton(_launchToolButton, "启动本工具", "▶", LaunchTool_Click, "#238CC8", "#FFFFFF", "#238CC8");
		ConfigureButton(_launchProgramButton, "启动其他程序", "＋", LaunchProgram_Click, "#EAF4FB", "#24506F", "#C7DDEC");
		ConfigureButton(_logoffButton, "注销分身", "⏻", Logoff_Click, "#FFF1F3", "#C74358", "#F3C7CE");
		buttons.Children.Add(reconnectButton);
		buttons.Children.Add(_launchToolButton);
		buttons.Children.Add(_launchProgramButton);
		buttons.Children.Add(_logoffButton);
		actionGrid.Children.Add(buttons);

		StackPanel statusPanel = new StackPanel
		{
			Margin = new Thickness(20, 0, 0, 0),
			VerticalAlignment = VerticalAlignment.Center
		};
		statusPanel.Children.Add(new TextBlock
		{
			Text = "当前状态",
			FontSize = 12,
			Foreground = BrushFromHex("#8A9BAD"),
			Margin = new Thickness(0, 0, 0, 3)
		});
		_statusText.FontSize = 13;
		_statusText.TextWrapping = TextWrapping.Wrap;
		_statusText.Foreground = BrushFromHex("#36566F");
		statusPanel.Children.Add(_statusText);
		Grid.SetColumn(statusPanel, 1);
		actionGrid.Children.Add(statusPanel);
		actionCard.Child = actionGrid;
		Grid.SetRow(actionCard, 1);
		root.Children.Add(actionCard);

		Border viewerCard = CreateCard(new Thickness(16, 0, 16, 16));
		Grid viewer = new Grid();
		viewer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(42) });
		viewer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
		Grid viewerHeader = new Grid { Margin = new Thickness(17, 0, 14, 0) };
		viewerHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		viewerHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		viewerHeader.Children.Add(new TextBlock
		{
			Text = "分身画面",
			VerticalAlignment = VerticalAlignment.Center,
			FontSize = 14,
			FontWeight = FontWeights.SemiBold,
			Foreground = BrushFromHex("#284B63")
		});
		TextBlock resolutionText = new TextBlock
		{
			Text = "1920 × 1080",
			VerticalAlignment = VerticalAlignment.Center,
			FontSize = 12,
			Foreground = BrushFromHex("#7890A4")
		};
		Grid.SetColumn(resolutionText, 1);
		viewerHeader.Children.Add(resolutionText);
		viewer.Children.Add(viewerHeader);

		Border rdpBorder = new Border
		{
			Margin = new Thickness(5, 0, 5, 5),
			BorderBrush = BrushFromHex("#B8CCE0"),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(9),
			Background = Brushes.Black
		};
		WindowsFormsHost host = new WindowsFormsHost { Child = _rdpHost };
		rdpBorder.Child = host;
		Grid.SetRow(rdpBorder, 1);
		viewer.Children.Add(rdpBorder);
		viewerCard.Child = viewer;
		Grid.SetRow(viewerCard, 2);
		root.Children.Add(viewerCard);
		Content = root;

		_statusTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
		_statusTimer.Tick += StatusTimer_Tick;
		Loaded += DesktopCloneWindow_Loaded;
		Closing += DesktopCloneWindow_Closing;
		Closed += (_, _) => _statusTimer.Stop();
		UpdateStatus();
	}

	internal static bool IsAdministrator()
	{
		using WindowsIdentity identity = WindowsIdentity.GetCurrent();
		return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
	}

	private async void DesktopCloneWindow_Loaded(object sender, RoutedEventArgs e)
	{
		_statusTimer.Start();
		await ConnectAsync();
	}

	private async void Connect_Click(object sender, RoutedEventArgs e)
	{
		await ConnectAsync();
	}

	private async Task ConnectAsync()
	{
		try
		{
			if (ChildSessionNativeMethods.IsRdpWrapperEnabled())
			{
				throw new InvalidOperationException("检测到 RDP Wrapper。桌面分身要求使用 Windows 原版远程桌面组件。");
			}
			_statusText.Text = "正在启用 Child Session 并连接本机 1920×1080 分身...";
			await Task.Run(ChildSessionNativeMethods.EnableChildSessions);
			_rdpHost.ConnectToChildSession(1920, 1080);
			UpdateStatus();
		}
		catch (Exception ex)
		{
			ShowError("创建桌面分身失败", ex);
			UpdateStatus();
		}
	}

	private async void LaunchTool_Click(object sender, RoutedEventArgs e)
	{
		await RunForSessionAsync(ChildSessionProcessLauncher.LaunchCurrentToolAsync, "本工具");
	}

	private async void LaunchProgram_Click(object sender, RoutedEventArgs e)
	{
		OpenFileDialog dialog = new OpenFileDialog
		{
			Title = "选择要在桌面分身中启动的程序",
			Filter = "程序 (*.exe)|*.exe",
			CheckFileExists = true,
			Multiselect = false
		};
		if (dialog.ShowDialog(this) != true)
		{
			return;
		}
		await RunForSessionAsync(id => ChildSessionProcessLauncher.LaunchExecutableAsync(id, dialog.FileName), dialog.SafeFileName);
	}

	private async Task RunForSessionAsync(Func<uint, Task> action, string displayName)
	{
		uint? sessionId = ChildSessionNativeMethods.TryGetChildSessionId();
		if (sessionId == null)
		{
			MessageBox.Show(this, "桌面分身尚未完成登录。请先在下方画面完成 Windows 登录。", "桌面分身", MessageBoxButton.OK, MessageBoxImage.Information);
			return;
		}
		try
		{
			_statusText.Text = $"正在桌面分身中启动 {displayName}...";
			await action(sessionId.Value);
			_statusText.Text = $"已在桌面分身中启动 {displayName}。";
		}
		catch (Exception ex)
		{
			ShowError($"启动 {displayName} 失败", ex);
		}
	}

	private void Logoff_Click(object sender, RoutedEventArgs e)
	{
		TryLogoffChildSession();
	}

	private bool TryLogoffChildSession()
	{
		if (ChildSessionNativeMethods.TryGetChildSessionId() == null)
		{
			try { _rdpHost.DisconnectSession(); } catch { }
			return true;
		}
		if (MessageBox.Show(this, "注销桌面分身会关闭其中的星铁、工具和 OCR 进程。确定继续吗？", "注销桌面分身", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
		{
			return false;
		}
		try
		{
			_rdpHost.DisconnectSession();
			ChildSessionNativeMethods.LogoffChildSession();
			UpdateStatus();
			return true;
		}
		catch (Exception ex)
		{
			ShowError("注销桌面分身失败", ex);
			return false;
		}
	}

	private void StatusTimer_Tick(object? sender, EventArgs e) => UpdateStatus();

	private void UpdateStatus()
	{
		uint? sessionId = ChildSessionNativeMethods.TryGetChildSessionId();
		_launchToolButton.IsEnabled = sessionId != null;
		_launchProgramButton.IsEnabled = sessionId != null;
		_logoffButton.IsEnabled = sessionId != null;
		if (sessionId != null)
		{
			_statusTitle.Text = "已连接";
			_statusDot.Background = BrushFromHex("#39B77A");
			_statusBadge.Background = BrushFromHex("#E8F8F0");
			_statusTitle.Foreground = BrushFromHex("#267A56");
			_statusText.Text = "会话已就绪，可以在分身内启动本工具或其他程序。";
		}
		else
		{
			_statusTitle.Text = "等待连接";
			_statusDot.Background = BrushFromHex("#E8A23A");
			_statusBadge.Background = BrushFromHex("#FFF6E7");
			_statusTitle.Foreground = BrushFromHex("#94601B");
			_statusText.Text = "请在分身画面完成 Windows 登录；需要使用本机账户的真实密码，不能使用 PIN。";
		}
	}

	private void DesktopCloneWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
	{
		if (ChildSessionNativeMethods.TryGetChildSessionId() == null)
		{
			try { _rdpHost.DisconnectSession(); } catch { }
			return;
		}
		if (!TryLogoffChildSession())
		{
			e.Cancel = true;
		}
	}

	private static Button CreateButton(string text, string icon, RoutedEventHandler handler, string background, string foreground, string border)
	{
		Button button = new Button();
		ConfigureButton(button, text, icon, handler, background, foreground, border);
		return button;
	}

	private static void ConfigureButton(Button button, string text, string icon, RoutedEventHandler handler, string background, string foreground, string border)
	{
		button.Click += handler;
		button.Content = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Children =
			{
				new TextBlock { Text = icon, FontSize = 17, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center },
				new TextBlock { Text = text, FontSize = 13, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center }
			}
		};
		button.Height = 44;
		button.Margin = new Thickness(0, 0, 9, 0);
		button.Padding = new Thickness(15, 0, 15, 0);
		button.MinWidth = 126;
		button.Background = BrushFromHex(background);
		button.Foreground = BrushFromHex(foreground);
		button.BorderBrush = BrushFromHex(border);
		button.BorderThickness = new Thickness(1);
		button.Cursor = System.Windows.Input.Cursors.Hand;
		button.Style = CreateButtonStyle();
	}

	private static Style CreateButtonStyle()
	{
		Style style = new Style(typeof(Button));
		FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
		border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
		border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
		border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
		border.SetValue(Border.CornerRadiusProperty, new CornerRadius(9));
		FrameworkElementFactory presenter = new FrameworkElementFactory(typeof(ContentPresenter));
		presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
		presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
		presenter.SetValue(ContentPresenter.MarginProperty, new TemplateBindingExtension(Control.PaddingProperty));
		border.AppendChild(presenter);
		style.Setters.Add(new Setter(Control.TemplateProperty, new ControlTemplate(typeof(Button)) { VisualTree = border }));
		style.Triggers.Add(new Trigger { Property = UIElement.IsMouseOverProperty, Value = true, Setters = { new Setter(UIElement.OpacityProperty, 0.88) } });
		style.Triggers.Add(new Trigger { Property = UIElement.IsEnabledProperty, Value = false, Setters = { new Setter(UIElement.OpacityProperty, 0.42) } });
		return style;
	}

	private static Border CreateCard(Thickness margin)
	{
		return new Border
		{
			Margin = margin,
			Background = Brushes.White,
			BorderBrush = BrushFromHex("#D7E5F1"),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(13),
			Effect = new DropShadowEffect
			{
				Color = Color.FromRgb(58, 92, 120),
				Opacity = 0.10,
				BlurRadius = 14,
				ShadowDepth = 2,
				Direction = 270
			}
		};
	}

	private static SolidColorBrush BrushFromHex(string value)
	{
		return new SolidColorBrush((Color)ColorConverter.ConvertFromString(value));
	}

	private void ShowError(string title, Exception exception)
	{
		Exception actual = exception.GetBaseException();
		MessageBox.Show(this, actual.Message, title, MessageBoxButton.OK, MessageBoxImage.Error);
	}
}
