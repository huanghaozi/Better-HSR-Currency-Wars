using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HsrCurrencyWarsCleanWpf.Services;

/// <summary>
/// 通过降采样灰度对比两张截图，判断画面是否发生了实质变化。
/// 用于校验一次点击是否真正生效，避免仅依赖 OCR 文字是否消失导致假通过。
/// </summary>
public static class ScreenshotComparer
{
	private const int SampleWidth = 64;

	private const int SampleHeight = 36;

	/// <summary>单个采样点的亮度差超过该值即认为该点发生变化。</summary>
	private const double ChangedLuminanceDelta = 25.0;

	/// <summary>变化采样点占比达到该值即认为画面发生了实质变化（页面切换）。</summary>
	private const double ChangedRatioThreshold = 0.08;

	/// <summary>变化占比达到阈值时返回 true。传入负值（无法比较）时返回 false。</summary>
	public static bool HasChanged(double changedRatio)
	{
		return changedRatio >= ChangedRatioThreshold;
	}

	/// <summary>
	/// 返回画面变化采样点占比（0~1）。任一参数为 null 或尺寸无效时返回 -1，表示无法判断。
	/// </summary>
	public static double ChangedRatio(BitmapSource? before, BitmapSource? after)
	{
		if (before == null || after == null)
		{
			return -1.0;
		}
		try
		{
			byte[] left = ToGraySamples(before);
			byte[] right = ToGraySamples(after);
			if (left.Length == 0 || left.Length != right.Length)
			{
				return -1.0;
			}
			int changed = 0;
			for (int i = 0; i < left.Length; i++)
			{
				if (Math.Abs(left[i] - right[i]) >= ChangedLuminanceDelta)
				{
					changed++;
				}
			}
			return changed / (double)left.Length;
		}
		catch
		{
			return -1.0;
		}
	}

	private static byte[] ToGraySamples(BitmapSource source)
	{
		if (source.PixelWidth <= 0 || source.PixelHeight <= 0)
		{
			return Array.Empty<byte>();
		}
		double scaleX = SampleWidth / (double)source.PixelWidth;
		double scaleY = SampleHeight / (double)source.PixelHeight;
		BitmapSource reduced = new TransformedBitmap(source, new ScaleTransform(scaleX, scaleY));
		FormatConvertedBitmap converted = new FormatConvertedBitmap(reduced, PixelFormats.Bgra32, null, 0.0);
		int width = Math.Max(1, converted.PixelWidth);
		int height = Math.Max(1, converted.PixelHeight);
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
