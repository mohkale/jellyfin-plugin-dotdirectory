<p align="right">
  <a href="https://github.com/mohkale/jellyfin-plugin-dotdirectory/actions/workflows/build.yml" alt=".NET"><img src="https://github.com/mohkale/jellyfin-plugin-dotdirectory/actions/workflows/build.yml/badge.svg" /></a>
</p>

# Jellyfin.Plugin.DotDirectory

Jellyfin plugin to extract cover information from freedesktop [.directory][.directory] files and
windows [Desktop.INI][desktop.ini] files.

[.directory]: https://specifications.freedesktop.org/desktop-entry-spec/latest/ar01s02.html
[desktop.ini]: https://docs.microsoft.com/en-us/windows/win32/shell/how-to-customize-folders-with-desktop-ini

## Plugin Description

This plugin extracts cover image information from standard directory customisation
systems on Windows and Free-Desktop. It's useful if you try to declare cover
information with File browsers like Dolphin or Windows Explorer and want the same
information reflected on Jellyfin. It also provides a convenient way to declare
covers independent of the Jellyfin workflow.

**Note**: Jellyfin has its own resolution for cover information which in some cases
may overwrite the covers determined by this plugin. For example if a folder has a
folder.jpg file then when the metadata for that item is refreshed it will replace the
.directory cover with folder.jpg.

### Config Description

#### [.directory][.directory]

```ini
[Desktop Entry]
Icon=./cover.jpg
```

#### [Desktop.INI][desktop.ini]

```ini
[.ShellClassInfo]
IconResource=C:\Icons\a.ico
```

## Installation

### Installing from Repository

1. Add my [manifest][mohkale-manifest] to your Jellyfin server.
1. If done correctly you should see a new repository on the page.
1. In the top menu, navigate to Catalog.
1. Under the Metadata section click on `DotDirectory`.
1. Click Install.
1. Restart Jellyfin.
1. On the left navigation menu, click on Plugins.
1. If successful, you will see `DotDirectory` with status as Active

[mohkale-manifest]: https://github.com/mohkale/jellyfin-plugin-manifest

### Manual Installation

1. Navigate to the [releases][releases] page.
1. Download the latest zip file.
1. Unzip contents into `<jellyfin data directory>/plugins/DotDirectory`.
1. Restart Jellyfin.
1. On the left navigation menu, click on Plugins.
1. If successful, you will see `DotDirectory` with status as Active.

[releases]: https://github.com/mohkale/jellyfin-plugin-dotdirectory/releases

## Usage

Once installed you'll likely have to enable the DotDirectory cover image provider on
the collections you want it used in. Then it should automatically pickup cover images
from .directory files on the next metadata refresh.
