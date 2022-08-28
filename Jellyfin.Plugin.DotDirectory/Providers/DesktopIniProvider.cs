namespace Jellyfin.Plugin.DotDirectory.Providers;

using Microsoft.Extensions.Logging;

public class DesktopIniProvider : ADotDirectoryImageProvider
{
    public DesktopIniProvider(ILogger<DotDirectoryProvider> logger) : base(logger) { }

    /// <inheritdoc />
    public override string Name => "DesktopIni";

    /// <inheritdoc />
    public override string FileName => "Desktop.ini";

    /// <inheritdoc />
    public override string SectionName => ".ShellClassInfo";

    /// <inheritdoc />
    public override string FieldName => "IconFile";
}
