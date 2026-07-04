using PdfSharp.Fonts;

namespace ISO11820.Services;

/// <summary>
/// PDFsharp 6.x 字体解析器 — 从 Windows Fonts 目录加载字体
/// </summary>
public class SystemFontResolver : IFontResolver
{
    private readonly string _fontDir;

    public string DefaultFontName => "SimHei";

    public SystemFontResolver()
    {
        // 直接使用环境变量获取 Windows 字体目录
        var windir = Environment.GetEnvironmentVariable("WINDIR") ?? @"C:\Windows";
        _fontDir = Path.Combine(windir, "Fonts");
    }

    public byte[]? GetFont(string faceName)
    {
        var lower = faceName.ToLower();

        // 按优先级匹配字体文件
        var candidates = lower switch
        {
            "arial" => new[] { "arial.ttf" },
            "microsoft yahei" => new[] { "msyh.ttf", "msyh.ttc" },
            "simsun" => new[] { "simsun.ttf", "simsun.ttc" },
            "simhei" => new[] { "simhei.ttf" },
            _ => new[] { "simhei.ttf", "simsun.ttf", "arial.ttf" }  // 默认：优先独立TTF中文字体
        };

        foreach (var file in candidates)
        {
            var path = Path.Combine(_fontDir, file);
            if (File.Exists(path))
                return File.ReadAllBytes(path);
        }

        return null;
    }

    public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        var lower = familyName.ToLower();

        // 检查字体文件是否存在
        bool FontExists(params string[] files)
        {
            return files.Any(f => File.Exists(Path.Combine(_fontDir, f)));
        }

        if (lower.Contains("yahei") || lower.Contains("microsoft"))
        {
            if (FontExists("msyh.ttc", "msyh.ttf"))
                return new FontResolverInfo("Microsoft YaHei");
        }

        if (lower == "arial")
        {
            if (FontExists("arial.ttf"))
                return new FontResolverInfo("Arial");
        }

        if (lower.Contains("simhei") || lower.Contains("hei"))
        {
            if (FontExists("simhei.ttf"))
                return new FontResolverInfo("SimHei");
        }

        if (lower.Contains("simsun") || lower.Contains("song"))
        {
            if (FontExists("simsun.ttf", "simsun.ttc"))
                return new FontResolverInfo("SimSun");
        }

        // 最终回退：优先独立 TTF 中文字体
        if (FontExists("simhei.ttf"))
            return new FontResolverInfo("SimHei");

        if (FontExists("arial.ttf"))
            return new FontResolverInfo("Arial");

        return null;
    }
}
