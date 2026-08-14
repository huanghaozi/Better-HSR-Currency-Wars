using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using HsrCurrencyWarsCleanWpf.Services;

namespace HsrCurrencyWarsCleanWpf;

internal sealed class UpdateAvailableWindow : Window
{
	private static readonly Brush TextBrush = new SolidColorBrush(Color.FromRgb(27, 34, 48));
	private static readonly Brush MutedBrush = new SolidColorBrush(Color.FromRgb(104, 118, 138));
	private static readonly Brush PrimaryBrush = new SolidColorBrush(Color.FromRgb(95, 175, 230));
	private readonly PasswordBox _mirrorChyanCdkBox = new PasswordBox();

	public bool StartAutomaticUpdate { get; private set; }
	public string MirrorChyanCdk { get; private set; } = "";

	public UpdateAvailableWindow(string newVersion, string currentVersion, string sourceName, string sizeText, string notes,
		bool isLatest = false, bool canAutomaticUpdate = true, string? headingOverride = null, string mirrorChyanCdk = "")
	{
		Title = "发现新版本";
		Width = 820;
		Height = 780;
		MinWidth = 720;
		MinHeight = 700;
		WindowStartupLocation = WindowStartupLocation.CenterOwner;
		ResizeMode = ResizeMode.CanResize;
		Background = new SolidColorBrush(Color.FromRgb(245, 249, 254));
		FontFamily = new FontFamily("Microsoft YaHei UI");
		_mirrorChyanCdkBox.Password = mirrorChyanCdk ?? "";
		MirrorChyanCdk = _mirrorChyanCdkBox.Password;
		Closed += (_, _) => MirrorChyanCdk = _mirrorChyanCdkBox.Password.Trim();
		Content = BuildContent(newVersion, currentVersion, sourceName, sizeText, notes, isLatest, canAutomaticUpdate, headingOverride);
	}

	private UIElement BuildContent(string newVersion, string currentVersion, string sourceName, string sizeText, string notes,
		bool isLatest, bool canAutomaticUpdate, string? headingOverride)
	{
		Grid root = new Grid { Margin = new Thickness(28) };
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

		StackPanel heading = new StackPanel();
		heading.Children.Add(new TextBlock { Text = headingOverride ?? (isLatest ? $"V{currentVersion} 已是最新版本" : $"发现新版本 V{newVersion}"), FontSize = 26, FontWeight = FontWeights.Bold, Foreground = TextBrush });
		heading.Children.Add(new TextBlock
		{
			Text = isLatest ? $"检查完成  ·  更新源 {sourceName}" : $"当前 V{currentVersion}  ·  更新源 {sourceName}  ·  完整包 {sizeText}",
			FontSize = 13,
			Foreground = MutedBrush,
			Margin = new Thickness(0, 8, 0, 18)
		});
		root.Children.Add(heading);

		Border notesPanel = CreatePanel();
		notesPanel.Margin = new Thickness(0, 0, 0, 14);
		StackPanel notesStack = new StackPanel();
		notesStack.Children.Add(new TextBlock { Text = "更新内容", FontSize = 15, FontWeight = FontWeights.SemiBold, Foreground = TextBrush });
		notesStack.Children.Add(new TextBlock { Text = notes, TextWrapping = TextWrapping.Wrap, Foreground = MutedBrush, Margin = new Thickness(0, 8, 0, 0), MaxHeight = 125 });
		notesPanel.Child = notesStack;
		Grid.SetRow(notesPanel, 1);
		root.Children.Add(notesPanel);

		Border downloadPanel = CreatePanel();
		StackPanel downloadStack = new StackPanel();
		downloadStack.Children.Add(new TextBlock { Text = "下载与更新方式", FontSize = 15, FontWeight = FontWeights.SemiBold, Foreground = TextBrush, Margin = new Thickness(0, 0, 0, 8) });
		downloadStack.Children.Add(CreateMirrorChyanRow());
		downloadStack.Children.Add(CreateDownloadRow("国内用户建议", "有 CDK 时使用 Mirror酱高速更新；未填写时使用 GitHub 增量更新", MirrorChyanUpdateService.WebsiteUrl, "Mirror酱"));
		downloadStack.Children.Add(CreateDownloadRow("海外用户建议", "GitHub Releases 下载速度通常更稳定", VelopackUpdateService.OverseasDownloadUrl, "GitHub"));
		downloadStack.Children.Add(CreateDownloadRow("GitHub 增量", isLatest ? "当前已是最新版本，无需下载；仍可打开发布页检查文件" : "安装版点击下方“增量更新”后下载差异包并自动安装", VelopackUpdateService.OverseasDownloadUrl, "发布页"));
		downloadStack.Children.Add(CreateDownloadRow("网盘链接", "百度网盘，提取码：t8g7；也可重新安装以修复文件", VelopackUpdateService.CloudDriveDownloadUrl, "打开"));
		downloadPanel.Child = downloadStack;
		Grid.SetRow(downloadPanel, 2);
		root.Children.Add(downloadPanel);

		Grid actions = new Grid { Margin = new Thickness(0, 18, 0, 0) };
		actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		actions.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		actions.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		Button later = CreateActionButton(isLatest ? "关闭" : "稍后再说", false);
		later.Click += (_, _) => Close();
		Grid.SetColumn(later, 1);
		actions.Children.Add(later);
		Button automatic = CreateActionButton("增量更新", true);
		automatic.Click += (_, _) => { StartAutomaticUpdate = true; Close(); };
		automatic.Visibility = !isLatest && canAutomaticUpdate ? Visibility.Visible : Visibility.Collapsed;
		Grid.SetColumn(automatic, 2);
		actions.Children.Add(automatic);
		Grid.SetRow(actions, 3);
		root.Children.Add(actions);
		return root;
	}

	private UIElement CreateMirrorChyanRow()
	{
		Grid row = new Grid { Margin = new Thickness(0, 5, 0, 8) };
		row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
		row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
		row.Children.Add(new TextBlock
		{
			Text = "Mirror酱 CDK：",
			FontWeight = FontWeights.SemiBold,
			Foreground = TextBrush,
			VerticalAlignment = VerticalAlignment.Center
		});

		StackPanel details = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
		details.Children.Add(new TextBlock
		{
			Text = "用于国内高速下载；留空仍可检查版本并使用 GitHub 更新",
			Foreground = MutedBrush,
			TextWrapping = TextWrapping.Wrap
		});
		_mirrorChyanCdkBox.Height = 32;
		_mirrorChyanCdkBox.Margin = new Thickness(0, 5, 8, 0);
		_mirrorChyanCdkBox.Padding = new Thickness(8, 4, 8, 4);
		_mirrorChyanCdkBox.BorderBrush = new SolidColorBrush(Color.FromRgb(183, 205, 227));
		_mirrorChyanCdkBox.PasswordChar = '●';
		details.Children.Add(_mirrorChyanCdkBox);
		Grid.SetColumn(details, 1);
		row.Children.Add(details);

		Button website = CreateActionButton("获取 CDK", true);
		website.Click += (_, _) => Process.Start(new ProcessStartInfo(MirrorChyanUpdateService.WebsiteUrl) { UseShellExecute = true });
		Grid.SetColumn(website, 2);
		row.Children.Add(website);
		return row;
	}

	private static Border CreatePanel() => new Border
	{
		Background = Brushes.White,
		BorderBrush = new SolidColorBrush(Color.FromRgb(183, 205, 227)),
		BorderThickness = new Thickness(1),
		CornerRadius = new CornerRadius(12),
		Padding = new Thickness(18)
	};

	private static UIElement CreateDownloadRow(string title, string description, string url, string buttonText)
	{
		Grid row = new Grid { Margin = new Thickness(0, 5, 0, 5) };
		row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
		row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
		row.Children.Add(new TextBlock { Text = title + "：", FontWeight = FontWeights.SemiBold, Foreground = TextBrush, VerticalAlignment = VerticalAlignment.Center });
		StackPanel details = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
		details.Children.Add(new TextBlock { Text = description, Foreground = MutedBrush, TextWrapping = TextWrapping.Wrap });
		if (!string.IsNullOrWhiteSpace(url))
		{
			TextBox linkText = new TextBox
			{
				Text = url,
				IsReadOnly = true,
				BorderThickness = new Thickness(0),
				Background = Brushes.Transparent,
				Foreground = new SolidColorBrush(Color.FromRgb(38, 112, 181)),
				TextWrapping = TextWrapping.Wrap,
				Padding = new Thickness(0),
				Margin = new Thickness(0, 4, 8, 0),
				Cursor = Cursors.IBeam
			};
			details.Children.Add(linkText);
		}
		Grid.SetColumn(details, 1);
		row.Children.Add(details);
		Button button = CreateActionButton(buttonText, !string.IsNullOrWhiteSpace(url));
		button.IsEnabled = !string.IsNullOrWhiteSpace(url);
		if (button.IsEnabled)
		{
			button.Click += (_, _) => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
		}
		Grid.SetColumn(button, 2);
		row.Children.Add(button);
		return row;
	}

	private static Button CreateActionButton(string text, bool primary)
	{
		Button button = new Button
		{
			Content = text,
			MinWidth = 88,
			Height = 36,
			Margin = new Thickness(8, 0, 0, 0),
			Padding = new Thickness(14, 0, 14, 0),
			Cursor = Cursors.Hand,
			Foreground = primary ? Brushes.White : TextBrush,
			Background = primary ? PrimaryBrush : Brushes.White,
			BorderBrush = new SolidColorBrush(Color.FromRgb(183, 205, 227)),
			BorderThickness = new Thickness(1)
		};
		return button;
	}
}
