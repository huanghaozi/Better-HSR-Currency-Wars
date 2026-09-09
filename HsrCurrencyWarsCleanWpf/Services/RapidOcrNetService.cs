using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HsrCurrencyWarsCleanWpf.Core;
using RapidOcrNet;
using SkiaSharp;

namespace HsrCurrencyWarsCleanWpf.Services;

/// <summary>
/// 基于 RapidOcrNet 的本地 OCR 服务（纯 C#，无需 Python 进程）。
/// 使用 PP-OCRv6 small 模型，识别率与速度均优于旧的 Python + PP-OCRv4 方案。
/// </summary>
public sealed class RapidOcrNetService : IOcrService, IDisposable
{
	private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);

	private readonly Lazy<RapidOcr> _ocr;

	private bool _disposed;

	public string Name { get; }

	public RapidOcrNetService(string modelDirectory)
	{
		string detPath = Path.Combine(modelDirectory, "PP-OCRv6_det_small.onnx");
		string recPath = Path.Combine(modelDirectory, "PP-OCRv6_rec_small.onnx");
		_ocr = new Lazy<RapidOcr>(() => CreateEngine(modelDirectory, detPath, recPath));
		Name = "RapidOcrNet PP-OCRv6 small";
	}

	/// <summary>
	/// 从 v6 模型目录反推 OCRRuntime 根目录（…\OCRRuntime\models\v6 → …\OCRRuntime）。
	/// </summary>
	private static string ResolveOcrRuntimeDirectory(string modelDirectory)
	{
		return Directory.GetParent(Directory.GetParent(modelDirectory)!.FullName)!.FullName;
	}

	/// <summary>模型文件是否齐全，用于启动时判断能否使用本服务。</summary>
	public static bool IsAvailable(string modelDirectory)
	{
		string ocrRuntimeDirectory = ResolveOcrRuntimeDirectory(modelDirectory);
		return File.Exists(Path.Combine(modelDirectory, "PP-OCRv6_det_small.onnx"))
			&& File.Exists(Path.Combine(modelDirectory, "PP-OCRv6_rec_small.onnx"))
			&& File.Exists(Path.Combine(modelDirectory, "ppocrv6_dict.txt"))
			&& File.Exists(Path.Combine(ocrRuntimeDirectory, "models", "v5", "ch_PP-LCNet_x0_25_textline_ori_cls_mobile.onnx"));
	}

	/// <summary>返回缺失的模型文件清单，供启动时给出明确提示。</summary>
	public static IReadOnlyList<string> FindMissingFiles(string modelDirectory)
	{
		string ocrRuntimeDirectory = ResolveOcrRuntimeDirectory(modelDirectory);
		string[] required =
		{
			Path.Combine(modelDirectory, "PP-OCRv6_det_small.onnx"),
			Path.Combine(modelDirectory, "PP-OCRv6_rec_small.onnx"),
			Path.Combine(modelDirectory, "ppocrv6_dict.txt"),
			Path.Combine(ocrRuntimeDirectory, "models", "v5", "ch_PP-LCNet_x0_25_textline_ori_cls_mobile.onnx"),
		};
		return required.Where(path => !File.Exists(path)).ToList();
	}

	private static RapidOcr CreateEngine(string modelDirectory, string detPath, string recPath)
	{
		RapidOcr ocr = new RapidOcr();
		// PPOCRv6Small 预设里分类模型和字典用的是相对路径（models\v5、models\v6），
		// 依赖当前工作目录。这里全部替换为绝对路径，避免因启动目录不同而找不到模型。
		string ocrRuntimeDirectory = ResolveOcrRuntimeDirectory(modelDirectory);
		string clsPath = Path.Combine(ocrRuntimeDirectory, "models", "v5", "ch_PP-LCNet_x0_25_textline_ori_cls_mobile.onnx");
		string keysPath = Path.Combine(modelDirectory, "ppocrv6_dict.txt");
		RapidOcrModelSet models = RapidOcrModelSet.PPOCRv6Small with
		{
			DetModelPath = detPath,
			RecModelPath = recPath,
			ClsModelPath = clsPath,
			KeysPath = keysPath,
		};
		// 初始化失败时直接抛给调用方，不做吞异常处理。
		ocr.InitModels(models);
		return ocr;
	}

	public async Task<OcrScanResult> RecognizeAsync(BitmapSource image, CancellationToken cancellationToken = default(CancellationToken))
	{
		cancellationToken.ThrowIfCancellationRequested();
		await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
		try
		{
			// 转成 SKBitmap 后在线程池执行，避免阻塞 UI 线程。
			using SKBitmap bitmap = ToSkBitmap(image);
			RapidOcr engine = _ocr.Value;
			OcrResult result = await Task.Run(
				() => engine.Detect(bitmap, RapidOcrOptions.PPOCRv6, cancellationToken, null),
				cancellationToken).ConfigureAwait(false);
			return Convert(result);
		}
		finally
		{
			_gate.Release();
		}
	}

	private static OcrScanResult Convert(OcrResult result)
	{
		List<OcrTextItem> items = new List<OcrTextItem>(result.TextBlocks.Length);
		foreach (TextBlock block in result.TextBlocks)
		{
			if (string.IsNullOrWhiteSpace(block.Text) || block.BoxPoints == null || block.BoxPoints.Length < 4)
			{
				continue;
			}
			SKPointI[] points = block.BoxPoints;
			int left = points.Min(p => p.X);
			int top = points.Min(p => p.Y);
			int right = points.Max(p => p.X);
			int bottom = points.Max(p => p.Y);
			double confidence = (block.CharScores != null && block.CharScores.Length > 0)
				? block.CharScores.Average()
				: block.BoxScore;
			items.Add(new OcrTextItem(block.Text, new Rect(left, top, Math.Max(1, right - left), Math.Max(1, bottom - top)), confidence));
		}
		string rawText = string.IsNullOrWhiteSpace(result.StrRes)
			? string.Join("\n", items.Select(i => i.Text))
			: result.StrRes;
		return new OcrScanResult(rawText, items, DateTime.Now);
	}

	private static SKBitmap ToSkBitmap(BitmapSource source)
	{
		FormatConvertedBitmap converted = source.Format == PixelFormats.Bgra32
			? new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0.0)
			: new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0.0);
		int width = converted.PixelWidth;
		int height = converted.PixelHeight;
		int stride = width * 4;
		byte[] pixels = new byte[stride * height];
		converted.CopyPixels(pixels, stride, 0);
		SKBitmap bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul));
		System.Runtime.InteropServices.Marshal.Copy(pixels, 0, bitmap.GetPixels(), pixels.Length);
		return bitmap;
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;
		if (_ocr.IsValueCreated)
		{
			_ocr.Value.Dispose();
		}
		_gate.Dispose();
	}
}
