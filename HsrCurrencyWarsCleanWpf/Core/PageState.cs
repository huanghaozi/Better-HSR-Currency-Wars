namespace HsrCurrencyWarsCleanWpf.Core;

/// <summary>
/// 货币战争的页面状态。流程按状态推进，而不是按固定步骤顺序盲走。
/// </summary>
public enum PageState
{
	/// <summary>无法识别当前页面。</summary>
	Unknown,

	/// <summary>货币战争首页：可以开始新一轮。</summary>
	Home,

	/// <summary>模式选择：标准博弈 / 超频博弈。</summary>
	ModeSelect,

	/// <summary>难度与职级选择：可以开始对局。</summary>
	DifficultySelect,

	/// <summary>匹配中：竞争对手生成动画。</summary>
	Matchmaking,

	/// <summary>词条与阵营展示：显示本场对局首领与敌人难度。</summary>
	TraitReveal,

	/// <summary>位面展示：点击空白处继续。</summary>
	PositionReveal,

	/// <summary>投资环境选择：人才引进 / 轮岗 / 减益概念股。</summary>
	InvestmentEnv,

	/// <summary>局内棋盘（备战阶段）。</summary>
	OpeningBoard,

	/// <summary>局内策略选择。</summary>
	StrategySelect,

	/// <summary>退出确认弹窗：放弃并结算 / 暂时离开。</summary>
	ConfirmExit,

	/// <summary>结算页：下一步 / 下一页 / 返回货币战争。</summary>
	Settlement,
}
