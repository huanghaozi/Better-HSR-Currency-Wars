using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HsrCurrencyWarsCleanWpf.Services;

public readonly record struct GameContentDetectionResult(
	bool Detected,
	GameContentInsets SuggestedInsets,
	string Description);

/// <summary>
/// 从窗口客户区截图推断真实游戏画面区域。
/// 主要处理两类偏移：
/// 1. 云游戏客户端自绘的标题栏/导航栏，位于客户区顶部但属于客户端 UI，不是游戏画面；
/// 2. 游戏画面按固定宽高比缩放后留下的黑边（letterbox）。
/// 检测结果只作为建议值，用户可以在界面上手动覆盖。
/// </summary>
public static class GameContentDetector
{
	private const int SampleMaxWidth = 320;

	/// <summary>判定为黑边的最大平均亮度。</summary>
	private const double BlackRowMeanThreshold = 26.0;

	/// <summary>判定为黑边的最大行内方差，避免把暗场景误判成黑边。</summary>
	private const double BlackRowVarianceThreshold = 60.0;

	/// <summary>内缩比例允许的合理范围，过小视为噪声，过大视为误判。</summary>
	private const double MinimumInsetRatio = 0.01;
	private const double MaximumInsetRatio = 0.30;

	/// <summary>游戏画面通常为 16:9，用于在没有黑边时按宽高比推断客户端 UI 栏。</summary>
	private const double TargetAspectRatio = 16.0 / 9.0;

	/// <summary>宽高比偏差小于该值时不推断，避免把正常的轻微缩放当成 UI 栏。</summary>
	private const double AspectTolerance = 0.02;

	public static GameContentDetectionResult Detect(BitmapSource screenshot)
	{
		if (screenshot.PixelWidth < 8 || screenshot.PixelHeight < 8)
		{
			return new GameContentDetectionResult(false, GameContentInsets.None, "截图尺寸过小，无法检测游戏画面区域。");
		}

		int width;
		int height;
		byte[] gray = ToGraySamples(screenshot, out width, out height);
		double[] rowMean = new double[height];
		double[] rowVariance = new double[height];
		for (int y = 0; y < height; y++)
		{
			int offset = y * width;
			double total = 0.0;
			double totalSquared = 0.0;
			for (int x = 0; x < width; x++)
			{
				int value = gray[offset + x];
				total += value;
				totalSquared += value * value;
			}
			double mean = total / width;
			rowMean[y] = mean;
			rowVariance[y] = Math.Max(0.0, totalSquared / width - mean * mean);
		}

		double[] columnMean = new double[width];
		for (int x = 0; x < width; x++)
		{
			double total = 0.0;
			for (int y = 0; y < height; y++)
			{
				total += gray[y * width + x];
			}
			columnMean[x] = total / height;
		}

		int blackTop = CountLeadingBlackRows(rowMean, rowVariance, 0, height / 3, 1);
		int blackBottom = CountLeadingBlackRows(rowMean, rowVariance, height - 1, height - height / 3, -1);
		int blackLeft = CountLeadingBlackColumns(columnMean, 0, width / 3, 1);
		int blackRight = CountLeadingBlackColumns(columnMean, width - 1, width - width / 3, -1);

		double topInset = blackTop / (double)height;
		double bottomInset = blackBottom / (double)height;
		double leftInset = blackLeft / (double)width;
		double rightInset = blackRight / (double)width;

		double clientAspect = width / (double)height;
		bool noVerticalBlackBars = blackTop == 0 && blackBottom == 0;
		bool noHorizontalBlackBars = blackLeft == 0 && blackRight == 0;
		double inferredTop = 0.0;
		double inferredSide = 0.0;

		// 云客户端会把标题栏和导航栏画在客户区内部，导致客户区比 16:9 更"高"。
		// 多出来的高度就是客户端 UI 栏，按此推断顶部内缩。
		if (noVerticalBlackBars && clientAspect < TargetAspectRatio - AspectTolerance)
		{
			double excess = (height - width / TargetAspectRatio) / height;
			if (excess > MinimumInsetRatio && excess <= MaximumInsetRatio)
			{
				inferredTop = excess;
				topInset = Math.Max(topInset, inferredTop);
			}
		}
		// 客户区比 16:9 更"宽"且左右没有黑边时，按 16:9 推断两侧黑边。
		else if (noHorizontalBlackBars && clientAspect > TargetAspectRatio + AspectTolerance)
		{
			double excess = (width - height * TargetAspectRatio) / 2.0 / width;
			if (excess > MinimumInsetRatio && excess <= MaximumInsetRatio)
			{
				inferredSide = excess;
				leftInset = Math.Max(leftInset, inferredSide);
				rightInset = Math.Max(rightInset, inferredSide);
			}
		}

		GameContentInsets suggested = new GameContentInsets(leftInset, topInset, rightInset, bottomInset).Clamp();
		bool detected = !suggested.IsNone;

		string description;
		if (!detected)
		{
			description = "未检测到黑边，且客户区宽高比接近 16:9，按整个客户区计算。";
		}
		else if (inferredTop > 0.0)
		{
			description = $"客户区宽高比 {clientAspect:0.000} 小于 16:9，推断顶部有约 {inferredTop * 100.0:0.0}% 的客户端 UI 栏（标题栏/导航栏）。{suggested.Describe()}";
		}
		else if (inferredSide > 0.0)
		{
			description = $"客户区宽高比 {clientAspect:0.000} 大于 16:9，推断左右各有约 {inferredSide * 100.0:0.0}% 黑边。{suggested.Describe()}";
		}
		else
		{
			description = $"检测到黑边。{suggested.Describe()}";
		}

		return new GameContentDetectionResult(detected, suggested, description);
	}

	private static int CountLeadingBlackRows(double[] rowMean, double[] rowVariance, int start, int end, int step)
	{
		int count = 0;
		for (int y = start; step > 0 ? y < end : y > end; y += step)
		{
			if (rowMean[y] > BlackRowMeanThreshold || rowVariance[y] > BlackRowVarianceThreshold)
			{
				break;
			}
			count++;
		}
		return count;
	}

	private static int CountLeadingBlackColumns(double[] columnMean, int start, int end, int step)
	{
		int count = 0;
		for (int x = start; step > 0 ? x < end : x > end; x += step)
		{
			if (columnMean[x] > BlackRowMeanThreshold)
			{
				break;
			}
			count++;
		}
		return count;
	}

	private static byte[] ToGraySamples(BitmapSource source, out int width, out int height)
	{
		double scale = Math.Min(1.0, SampleMaxWidth / (double)Math.Max(1, source.PixelWidth));
		BitmapSource reduced = scale < 1.0
			? new TransformedBitmap(source, new ScaleTransform(scale, scale))
			: source;
		FormatConvertedBitmap converted = new FormatConvertedBitmap(reduced, PixelFormats.Bgra32, null, 0.0);
		width = Math.Max(1, converted.PixelWidth);
		height = Math.Max(1, converted.PixelHeight);
		int stride = width * 4;
		byte[] pixels = new byte[stride * height];
		converted.CopyPixels(pixels, stride, 0);
		byte[] gray = new byte[width * height];
		for (int i = 0, offset = 0; offset < pixels.Length; offset += 4, i++)
		{
			gray[i] = (byte)((pixels[offset] * 11 + pixels[offset + 1] * 59 + pixels[offset + 2] * 30) / 100);
		}
		return gray;
	}
}
