using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace HsrCurrencyWarsCleanWpf.Services;

public sealed record MirrorChyanCheckResult(
	bool Success,
	string LatestVersion,
	string DownloadUrl,
	string ReleaseNotes,
	string Message);

public static class MirrorChyanUpdateService
{
	public const string ResourceId = "Better-HSR-Currency-Wars";
	public const string WebsiteUrl = "https://mirrorchyan.com/zh/projects?rid=Better-HSR-Currency-Wars&source=better_hsrcw_update_window";
	private const string ApiUrl = "https://mirrorchyan.com/api/resources/Better-HSR-Currency-Wars/latest";

	private static readonly HttpClient Http = new HttpClient
	{
		Timeout = TimeSpan.FromSeconds(8)
	};

	public static async Task<MirrorChyanCheckResult> CheckLatestAsync(string currentVersion, string cdk = "", CancellationToken cancellationToken = default)
	{
		try
		{
			string url = ApiUrl
				+ "?current_version=" + Uri.EscapeDataString(currentVersion)
				+ "&os=win&arch=x64&user_agent=Better_HSRCW";
			if (!string.IsNullOrWhiteSpace(cdk))
			{
				url += "&cdk=" + Uri.EscapeDataString(cdk.Trim());
			}

			using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
			request.Headers.UserAgent.ParseAdd("Better-HSR-Currency-Wars-Updater");
			using HttpResponseMessage response = await Http.SendAsync(request, cancellationToken);
			if (!response.IsSuccessStatusCode)
			{
				return new MirrorChyanCheckResult(false, "", "", "", $"Mirror酱返回 {(int)response.StatusCode} {response.ReasonPhrase}");
			}

			await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
			using JsonDocument document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
			JsonElement root = document.RootElement;
			int code = root.TryGetProperty("code", out JsonElement codeElement) && codeElement.TryGetInt32(out int parsedCode) ? parsedCode : -1;
			string message = GetString(root, "msg");
			if (code != 0 || !root.TryGetProperty("data", out JsonElement data) || data.ValueKind != JsonValueKind.Object)
			{
				return new MirrorChyanCheckResult(false, "", "", "", string.IsNullOrWhiteSpace(message) ? $"Mirror酱检查失败（代码 {code}）" : message);
			}

			string version = GetString(data, "version_name");
			if (string.IsNullOrWhiteSpace(version))
			{
				return new MirrorChyanCheckResult(false, "", "", "", "Mirror酱没有返回最新版本号。");
			}

			return new MirrorChyanCheckResult(
				true,
				version,
				GetString(data, "url"),
				GetString(data, "release_note"),
				"Mirror酱版本检查完成。");
		}
		catch (OperationCanceledException)
		{
			return new MirrorChyanCheckResult(false, "", "", "", "Mirror酱更新检查已取消或超时。");
		}
		catch (Exception ex)
		{
			return new MirrorChyanCheckResult(false, "", "", "", "Mirror酱更新检查失败：" + ex.Message);
		}
	}

	private static string GetString(JsonElement element, string propertyName)
	{
		return element.TryGetProperty(propertyName, out JsonElement value) && value.ValueKind == JsonValueKind.String
			? value.GetString() ?? ""
			: "";
	}
}
