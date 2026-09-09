using System;
using System.Collections.Generic;

namespace HsrCurrencyWarsCleanWpf.Core;

public sealed class FlowStep
{
	public required FlowStepKind Kind { get; init; }

	public required string Name { get; init; }

	public IReadOnlyList<string> Aliases { get; init; } = Array.Empty<string>();

	public RatioRegion SearchRegion { get; init; } = CurrencyWarsFlow.FullWindow;

	public RatioPoint? ClickPoint { get; init; }

	public RatioPoint? FallbackPoint { get; init; }

	/// <summary>
	/// 优先直接点击 <see cref="FallbackPoint"/>，而不是先走 OCR 识别重试。
	/// 适用于 OCR 容易误识别到其他同名字样、但固定坐标稳定的按钮（例如词条页底部的“下一步”）。
	/// 固定坐标点击后仍会验证页面是否切换，未切换时才回退 OCR。
	/// </summary>
	public bool PreferFixedPoint { get; init; }

	public string? Key { get; init; }

	public double TimeoutSeconds { get; init; } = 12.0;

	public double WaitAfterSeconds { get; init; }

	public bool FixedWaitAfter { get; init; }

	public double StandardDelayAfterSeconds { get; init; } = 1.3;

	public bool CheckDebuffAfterStep { get; init; }
}
