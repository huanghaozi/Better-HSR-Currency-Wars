# Velopack 发布方式

首次接入 Velopack 的版本需要用户下载并运行 `Setup.exe`。从下一个版本开始，程序可以从腾讯云 COS 自动下载完整包或增量包并安装重启。

打包示例：

```powershell
.\Build-Velopack.ps1 -Version 12.99.0
```

输出目录为 `VelopackReleases`。不要清空该目录；生成下一版本增量包时需要保留上一版本的完整 `.nupkg`。

上传腾讯云 COS 的 `/updates/` 目录时，至少上传或覆盖：

- `RELEASES`
- `releases.win.json`
- 新版本的完整 `.nupkg`
- 新版本的增量 `.nupkg`（若生成）

`Setup.exe` 用于首次安装，可以放在 `/updates/`，也可以放在 GitHub Release 或其他下载位置。`Portable.zip` 是免安装版，不具备安装版的自动替换能力。

腾讯云对象必须允许匿名读取；程序使用永久对象地址，不使用一小时后失效的临时签名链接。
