using System;
using System.Collections.Generic;
using System.Linq;

namespace HsrCurrencyWarsCleanWpf.Core;

/// <summary>单个页面锚点：文字与权重。</summary>
public sealed record PageAnchor(string Text, int Weight);

/// <summary>页面状态定义。</summary>
public sealed record PageStateDefinition(PageState State, string DisplayName, IReadOnlyList<PageAnchor> Anchors);

/// <summary>状态识别结果。</summary>
public sealed record PageStateMatch(PageState State, string DisplayName, int Score, IReadOnlyList<string> HitAnchors)
{
	public static PageStateMatch Unknown { get; } = new PageStateMatch(PageState.Unknown, "未知页面", 0, Array.Empty<string>());
}

/// <summary>
/// 通过全屏 OCR 的文字锚点判断当前处于哪个页面。
/// 每个状态有若干带权重的锚点，命中后累加得分，得分最高且达到阈值的状态即为当前页面。
/// 独有文字给高权重，通用文字给低权重，避免"下一步"这类多处出现的字样造成误判。
/// </summary>
public static class PageStateDetector
{
	/// <summary>判定为某个状态所需的最低得分。</summary>
	public const int MinimumScore = 8;

	private static readonly PageStateDefinition[] Definitions = new PageStateDefinition[]
	{
		new PageStateDefinition(PageState.Home, "货币战争首页", new PageAnchor[]
		{
			new PageAnchor("开始货币战争", 10),
			new PageAnchor("本期剩余时间", 6),
			new PageAnchor("零和博弈", 4),
			new PageAnchor("晋升等级", 3),
		}),
		new PageStateDefinition(PageState.ModeSelect, "模式选择", new PageAnchor[]
		{
			new PageAnchor("进入标准博弈", 10),
			new PageAnchor("超频博弈", 8),
			new PageAnchor("标准博弈", 5),
			new PageAnchor("通关可获得积分", 4),
		}),
		new PageStateDefinition(PageState.DifficultySelect, "难度选择", new PageAnchor[]
		{
			new PageAnchor("开始对局", 10),
			new PageAnchor("敌人难度", 6),
			new PageAnchor("晋升点", 5),
			new PageAnchor("词缀数量", 5),
		}),
		new PageStateDefinition(PageState.Matchmaking, "匹配中", new PageAnchor[]
		{
			new PageAnchor("竞争对手生成中", 10),
		}),
		new PageStateDefinition(PageState.TraitReveal, "词条展示", new PageAnchor[]
		{
			new PageAnchor("本场对局首领", 10),
			new PageAnchor("阵营", 6),
			new PageAnchor("随从强化", 4),
			new PageAnchor("变宝为废", 3),
		}),
		new PageStateDefinition(PageState.PositionReveal, "位面展示", new PageAnchor[]
		{
			new PageAnchor("位面", 8),
			new PageAnchor("点击空白处继续", 6),
		}),
		new PageStateDefinition(PageState.InvestmentEnv, "投资环境", new PageAnchor[]
		{
			new PageAnchor("投资环境", 10),
			new PageAnchor("剩余次数", 6),
			new PageAnchor("人才引进", 5),
			new PageAnchor("轮岗", 5),
			new PageAnchor("减益概念股", 5),
		}),
		new PageStateDefinition(PageState.OpeningBoard, "局内棋盘", new PageAnchor[]
		{
			new PageAnchor("备战阶段", 10),
			new PageAnchor("前台区域", 6),
			new PageAnchor("后台区域", 6),
			new PageAnchor("购买经验", 4),
		}),
		new PageStateDefinition(PageState.StrategySelect, "策略选择", new PageAnchor[]
		{
			new PageAnchor("请选择投资策略", 10),
			new PageAnchor("刷新次数", 6),
			new PageAnchor("返回备战界面", 5),
		}),
		new PageStateDefinition(PageState.ConfirmExit, "退出确认", new PageAnchor[]
		{
			new PageAnchor("放弃并结算", 10),
			new PageAnchor("暂时离开", 10),
			new PageAnchor("当前进度", 6),
		}),
		new PageStateDefinition(PageState.Settlement, "结算页", new PageAnchor[]
		{
			new PageAnchor("返回货币战争", 10),
			new PageAnchor("下一页", 8),
			new PageAnchor("前往结算", 6),
			new PageAnchor("下一步", 3),
		}),
	};

	public static IReadOnlyList<PageStateDefinition> All => Definitions;

	/// <summary>
	/// 根据 OCR 结果识别当前页面状态。返回得分最高的状态；低于阈值时返回 Unknown。
	/// </summary>
	public static PageStateMatch Detect(OcrScanResult scan, int fuzzyScore)
	{
		if (scan == null || scan.Items.Count == 0)
		{
			return PageStateMatch.Unknown;
		}
		PageStateMatch best = PageStateMatch.Unknown;
		foreach (PageStateDefinition definition in Definitions)
		{
			int score = 0;
			List<string> hits = new List<string>();
			foreach (PageAnchor anchor in definition.Anchors)
			{
				if (scan.Items.Any(item => TextMatcher.FuzzyContains(item.Text, anchor.Text, fuzzyScore)))
				{
					score += anchor.Weight;
					hits.Add(anchor.Text);
				}
			}
			if (score > best.Score)
			{
				best = new PageStateMatch(definition.State, definition.DisplayName, score, hits);
			}
		}
		return best.Score >= MinimumScore ? best : PageStateMatch.Unknown;
	}

	/// <summary>返回所有命中状态及其得分，用于诊断输出。</summary>
	public static IReadOnlyList<PageStateMatch> DetectAll(OcrScanResult scan, int fuzzyScore)
	{
		if (scan == null || scan.Items.Count == 0)
		{
			return Array.Empty<PageStateMatch>();
		}
		List<PageStateMatch> results = new List<PageStateMatch>();
		foreach (PageStateDefinition definition in Definitions)
		{
			int score = 0;
			List<string> hits = new List<string>();
			foreach (PageAnchor anchor in definition.Anchors)
			{
				if (scan.Items.Any(item => TextMatcher.FuzzyContains(item.Text, anchor.Text, fuzzyScore)))
				{
					score += anchor.Weight;
					hits.Add(anchor.Text);
				}
			}
			if (score > 0)
			{
				results.Add(new PageStateMatch(definition.State, definition.DisplayName, score, hits));
			}
		}
		return results.OrderByDescending(r => r.Score).ToList();
	}

	public static string GetDisplayName(PageState state)
	{
		return Definitions.FirstOrDefault(d => d.State == state)?.DisplayName ?? state.ToString();
	}
}
