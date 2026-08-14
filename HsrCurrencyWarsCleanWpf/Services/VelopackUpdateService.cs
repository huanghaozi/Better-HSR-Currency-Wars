using System;
using System.Threading;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace HsrCurrencyWarsCleanWpf.Services;

public sealed record VelopackUpdateCheckResult(
	bool IsInstalled,
	UpdateManager? Manager,
	Velopack.UpdateInfo? Update,
	string SourceName,
	string Message);

public static class VelopackUpdateService
{
	public const string GithubRepositoryUrl = "https://github.com/439awsl-hue/Better-HSR-Currency-Wars";
	public const string DomesticSetupUrl = "https://pan.baidu.com/s/1GA8BFFIcAtqdhuhgZk2g0A?pwd=t8g7";
	public const string OverseasDownloadUrl = GithubRepositoryUrl + "/releases/latest";
	public const string CloudDriveDownloadUrl = "https://pan.baidu.com/s/1GA8BFFIcAtqdhuhgZk2g0A?pwd=t8g7";

	public static bool IsInstalled
	{
		get
		{
			try
			{
				GithubSource githubSource = new GithubSource(GithubRepositoryUrl, null!, prerelease: false, null!);
				return new UpdateManager(githubSource).IsInstalled;
			}
			catch
			{
				return false;
			}
		}
	}

	public static async Task<VelopackUpdateCheckResult> CheckAsync(string mirrorChyanCdk = "", CancellationToken cancellationToken = default)
	{
		GithubSource githubSource = new GithubSource(GithubRepositoryUrl, null!, prerelease: false, null!);
		UpdateManager githubManager = new UpdateManager(githubSource);
		if (!githubManager.IsInstalled)
		{
			return new VelopackUpdateCheckResult(false, null, null, "", "当前是免安装/测试版，继续使用普通更新检查。");
		}

		MirrorChyanCheckResult mirrorResult = await MirrorChyanUpdateService.CheckLatestAsync(
			githubManager.CurrentVersion?.ToString() ?? UpdateChecker.CurrentVersion,
			mirrorChyanCdk,
			cancellationToken);
		if (mirrorResult.Success && !UpdateChecker.IsRemoteNewer(
			mirrorResult.LatestVersion,
			githubManager.CurrentVersion?.ToString() ?? UpdateChecker.CurrentVersion))
		{
			return new VelopackUpdateCheckResult(true, githubManager, null, "Mirror酱", "当前已是最新版本。");
		}

		try
		{
			Velopack.UpdateInfo? update = await githubManager.CheckForUpdatesAsync();
			string sourceName = mirrorResult.Success ? "Mirror酱检测 / GitHub 增量" : "GitHub";
			return new VelopackUpdateCheckResult(true, githubManager, update, sourceName, update == null ? "当前已是最新版本。" : "发现可自动安装的新版本。");
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			return new VelopackUpdateCheckResult(true, null, null, "", $"{mirrorResult.Message}\nGitHub 增量更新源失败：{ex.Message}");
		}
	}
}
