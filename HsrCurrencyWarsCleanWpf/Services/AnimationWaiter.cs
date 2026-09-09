using System;
using System.Windows.Media.Imaging;

namespace HsrCurrencyWarsCleanWpf.Services;

/// <summary>
/// 判断画面是否已经稳定（动画播完）。
/// 通过连续两帧的降采样灰度差异来判断：差异足够小说明画面不再变化。
/// </summary>
public static class AnimationWaiter
{
	/// <summary>连续两帧差异小于该值时认为画面稳定。</summary>
	public const double StableThreshold = 0.02;

	public static bool IsStable(double changedRatio)
	{
		return changedRatio >= 0.0 && changedRatio < StableThreshold;
	}

	public static double DiffRatio(BitmapSource? before, BitmapSource? after)
	{
		return ScreenshotComparer.ChangedRatio(before, after);
	}
}
