namespace Jellyfin.Plugin.DotDirectory.Providers;

using Microsoft.Extensions.Logging;

public class DotDirectoryProvider : ADotDirectoryImageProvider
{
    public DotDirectoryProvider(ILogger<DotDirectoryProvider> logger) : base(logger) { }

    /// <inheritdoc />
    public override string Name => "DotDirectory";

    /// <inheritdoc />
    public override string FileName => ".directory";

    /// <inheritdoc />
    public override string SectionName => "Desktop Entry";

    /// <inheritdoc />
    public override string FieldName => "Icon";
}
