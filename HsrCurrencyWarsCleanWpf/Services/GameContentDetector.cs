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

	/// <summary>相邻行差异达到该值即认为进入了持续变化的游戏画面。</summary>
	private const double ContentEdgeDiffThreshold = 3.5;

	/// <summary>要求进入内容区后连续多少行保持变化，避免把 UI 栏内部噪点当成边界。</summary>
	private const int SustainedContentRows = 6;

	/// <summary>顶部 UI 栏允许的合理范围。</summary>
	private const double MinimumTopInset = 0.01;
	private const double MaximumTopInset = 0.30;

	/// <summary>游戏画面通常为 16:9，用于在无法可靠检测时给出推断值。</summary>
	private const double TargetAspectRatio = 16.0 / 9.0;

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

		int contentTop = blackTop;
		int contentBottom = height - blackBottom;

		// 在去黑边后的顶部区域寻找客户端 UI 栏与游戏画面的水平分界。
		int uiBarRows = DetectTopUiBarRows(gray, width, rowMean, contentTop, contentBottom);
		int detectedTopRows = uiBarRows > 0 ? contentTop + uiBarRows : contentTop;

		// 宽高比推断：客户区比 16:9 更"高"时，多出来的高度通常就是顶部客户端 UI 栏。
		// 云客户端标题栏常驻且导航栏内部有文字，逐行差异容易被文字干扰，
		// 因此宽高比推断可用时优先采用。
		double aspectInferredTopRatio = 0.0;
		double clientAspect = width / (double)height;
		if (clientAspect < TargetAspectRatio - 0.02 && blackTop == 0 && blackBottom == 0)
		{
			double expectedContentHeight = width / TargetAspectRatio;
			double inferred = Math.Max(0.0, (height - expectedContentHeight) / height);
			if (inferred > MinimumTopInset && inferred <= MaximumTopInset)
			{
				aspectInferredTopRatio = inferred;
			}
		}

		double detectedTopRatio = detectedTopRows / (double)height;
		double topInset = Math.Max(detectedTopRatio, aspectInferredTopRatio);
		double bottomInset = blackBottom / (double)height;
		double leftInset = blackLeft / (double)width;
		double rightInset = blackRight / (double)width;

		// 客户区比 16:9 更"宽"时，左右通常存在黑边，按 16:9 推断两侧内缩。
		if (clientAspect > TargetAspectRatio + 0.02 && blackLeft == 0 && blackRight == 0)
		{
			double expectedContentWidth = height * TargetAspectRatio;
			double inferredSide = Math.Max(0.0, (width - expectedContentWidth) / 2.0 / width);
			if (inferredSide > MinimumTopInset && inferredSide <= MaximumTopInset)
			{
				leftInset = Math.Max(leftInset, inferredSide);
				rightInset = Math.Max(rightInset, inferredSide);
			}
		}

		GameContentInsets suggested = new GameContentInsets(leftInset, topInset, rightInset, bottomInset).Clamp();

		bool detected = !suggested.IsNone;
		string description;
		if (!detected)
		{
			description = "未检测到客户端 UI 栏或黑边，按整个客户区计算。";
		}
		else if (aspectInferredTopRatio > 0.0)
		{
			description = $"客户区宽高比 {clientAspect:0.000} 小于 16:9，推断顶部有约 {aspectInferredTopRatio * 100.0:0.0}% 的客户端 UI 栏。{suggested.Describe()}";
		}
		else if (uiBarRows > 0)
		{
			description = $"检测到客户端 UI 栏约 {uiBarRows * 100.0 / height:0.0}%。{suggested.Describe()}";
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

	/// <summary>
	/// 从顶部开始寻找第一条持续变化的分界线，其上方视为客户端 UI 栏。
	/// 标题栏内部渐变平滑，相邻行差异很小；进入游戏画面后差异会持续变大。
	/// </summary>
	private static int DetectTopUiBarRows(byte[] gray, int width, double[] rowMean, int contentTop, int contentBottom)
	{
		int availableHeight = contentBottom - contentTop;
		if (availableHeight < 40)
		{
			return 0;
		}
		int searchEnd = contentTop + Math.Min(availableHeight - SustainedContentRows, (int)(availableHeight * 0.30));
		double[] diff = new double[contentBottom];
		for (int y = contentTop + 1; y < contentBottom; y++)
		{
			int currentOffset = y * width;
			int previousOffset = (y - 1) * width;
			long total = 0;
			for (int x = 0; x < width; x++)
			{
				total += Math.Abs(gray[currentOffset + x] - gray[previousOffset + x]);
			}
			diff[y] = total / (double)width;
		}

		for (int y = contentTop + 1; y <= searchEnd; y++)
		{
			bool sustained = true;
			for (int k = 0; k < SustainedContentRows; k++)
			{
				if (y + k >= contentBottom || diff[y + k] < ContentEdgeDiffThreshold)
				{
					sustained = false;
					break;
				}
			}
			if (!sustained)
			{
				continue;
			}
			double ratio = (y - contentTop) / (double)(contentBottom - contentTop);
			if (ratio >= MinimumTopInset && ratio <= MaximumTopInset)
			{
				return y - contentTop;
			}
		}
		return 0;
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
