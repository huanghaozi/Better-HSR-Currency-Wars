using System;

namespace HsrCurrencyWarsCleanWpf.Services;

/// <summary>
/// 游戏画面相对窗口客户区的内缩比例。
/// 用于处理云游戏客户端自绘标题栏、导航栏以及画面黑边（letterbox）导致的坐标偏移。
/// 例如云客户端顶部有 6% 高度的标题栏时，将 Top 设为 0.06 即可让所有比例坐标基于真实游戏画面。
/// </summary>
public sealed record GameContentInsets(double Left, double Top, double Right, double Bottom)
{
	/// <summary>单个方向允许的最大内缩比例，避免配置错误把画面裁没。</summary>
	private const double MaxInsetPerEdge = 0.45;

	public static GameContentInsets None { get; } = new GameContentInsets(0.0, 0.0, 0.0, 0.0);

	public bool IsNone => Left <= 0.0 && Top <= 0.0 && Right <= 0.0 && Bottom <= 0.0;

	/// <summary>把各方向内缩限制到合理范围，保证剩余画面宽高大于 0。</summary>
	public GameContentInsets Clamp()
	{
		double left = Math.Clamp(Left, 0.0, MaxInsetPerEdge);
		double top = Math.Clamp(Top, 0.0, MaxInsetPerEdge);
		double right = Math.Clamp(Right, 0.0, MaxInsetPerEdge);
		double bottom = Math.Clamp(Bottom, 0.0, MaxInsetPerEdge);
		if (left + right >= 1.0 - 1e-6)
		{
			left = MaxInsetPerEdge;
			right = MaxInsetPerEdge;
		}
		if (top + bottom >= 1.0 - 1e-6)
		{
			top = MaxInsetPerEdge;
			bottom = MaxInsetPerEdge;
		}
		return new GameContentInsets(left, top, right, bottom);
	}

	/// <summary>把内缩应用到窗口客户区矩形，得到真实游戏画面在屏幕上的矩形。</summary>
	public WindowClientRect Apply(WindowClientRect clientRect)
	{
		GameContentInsets insets = Clamp();
		if (insets.IsNone)
		{
			return clientRect;
		}
		int insetLeft = (int)Math.Round(clientRect.Width * insets.Left);
		int insetTop = (int)Math.Round(clientRect.Height * insets.Top);
		int insetRight = (int)Math.Round(clientRect.Width * insets.Right);
		int insetBottom = (int)Math.Round(clientRect.Height * insets.Bottom);
		int width = Math.Max(1, clientRect.Width - insetLeft - insetRight);
		int height = Math.Max(1, clientRect.Height - insetTop - insetBottom);
		return new WindowClientRect(clientRect.Left + insetLeft, clientRect.Top + insetTop, width, height);
	}

	public string Describe()
	{
		GameContentInsets insets = Clamp();
		if (insets.IsNone)
		{
			return "游戏画面内缩：无（按整个客户区计算）";
		}
		return $"游戏画面内缩：左 {insets.Left:P1} 上 {insets.Top:P1} 右 {insets.Right:P1} 下 {insets.Bottom:P1}";
	}
}
