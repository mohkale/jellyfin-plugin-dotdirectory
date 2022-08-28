namespace Jellyfin.Plugin.DotDirectory;

using Jellyfin.Plugin.DotDirectory.Configuration;

using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Serialization;

public class Plugin : BasePlugin<PluginConfiguration>
{
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer) { }

    /// <inheritdoc />
    public override string Name => "DotDirectory";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("96e4df86-207a-4d89-b015-5672d7ab30f4");

    /// <inheritdoc />
    public new string Description = "Source media cover information from FreeDesktop standard Desktop Entry files.";
}
