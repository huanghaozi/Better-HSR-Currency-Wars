namespace HsrCurrencyWarsCleanWpf.Services;

public sealed record WindowClientRect(int Left, int Top, int Width, int Height)
{
	/// <summary>用于日志输出的紧凑描述。</summary>
	public string DescribeRect()
	{
		return $"{Width}x{Height}（left={Left}, top={Top}）";
	}
}
