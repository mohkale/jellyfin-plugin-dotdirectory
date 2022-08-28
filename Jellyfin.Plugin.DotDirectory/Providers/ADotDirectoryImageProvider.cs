namespace Jellyfin.Plugin.DotDirectory.Providers;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Text;
using System.Text.RegularExpressions;

using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.Audio;

public abstract class ADotDirectoryImageProvider : IDynamicImageProvider
{
    /// <inheritdoc />
    public abstract string Name { get; }

    private readonly ILogger<DotDirectoryProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ADotDirectoryImageProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public ADotDirectoryImageProvider(ILogger<DotDirectoryProvider> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public bool Supports(BaseItem item)
    {
        return item is Series || item is Season || item is Movie || item is MusicAlbum;
    }

    /// <inheritdoc />
    public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
    {
        yield return ImageType.Primary;
    }

    // ┌────────────────────────────────────────────────────┐
    // │               Config Extraction Body               │
    // └────────────────────────────────────────────────────┘

    /// <summary>
    /// Name of INI file containing icon reference.
    /// </summary>
    public abstract string FileName { get; }

    /// <summary>
    /// Name of section in <see cref="FileName"/> where the icon reference should be.
    /// </summary>
    public abstract string SectionName { get; }

    /// <summary>
    /// Name of field in <see cref="SectionName"/>  where the icon value should be.
    /// </summary>
    public abstract string FieldName { get; }

    /// <inheritdoc />
    public async Task<DynamicImageResponse> GetImage(BaseItem item, ImageType type, CancellationToken cancellationToken)
    {
        if (!item.IsFileProtocol || (type != ImageType.Primary && type != ImageType.Thumb))
        {
            return new DynamicImageResponse { HasImage = false };
        }

        string directory;
        if (!Directory.Exists(item.Path))
        {
            directory = item.ContainingFolderPath;
        }
        else
        {
            directory = item.Path;
        }

        var configFile = Path.Join(directory, FileName);
        if (!File.Exists(configFile))
        {
            _logger.LogDebug("Skipping cover extraction because config={path} doesn't exist", configFile);
            return new DynamicImageResponse { HasImage = false };
        }

        var iconFile = await ReadIconFromConfigFile(configFile);
        if (iconFile is null)
        {
            _logger.LogDebug("Skipping cover extraction because config={path} doesn't contain an icon reference", configFile);
            return new DynamicImageResponse { HasImage = false };
        }
        _logger.LogDebug("Read icon value from {path} as {icon}", configFile, iconFile);

        if (!IsAbsolutePath(iconFile))
        {
            iconFile = Path.Join(directory, iconFile);
            _logger.LogDebug("Resolved relative icon value from config={path} to icon={icon}", configFile, iconFile);
        }

        if (!File.Exists(iconFile))
        {
            _logger.LogWarning("Icon file icon={icon} from file={path} does not exist", iconFile, configFile);
            return new DynamicImageResponse { HasImage = false };
        }

        var result = new DynamicImageResponse
        {
            HasImage = true,
            Path = iconFile,
            Protocol = MediaProtocol.File,
        };
        result.SetFormatFromMimeType(result.Path);
        return result;
    }

    // ┌────────────────────────────────────────────────────┐
    // │                   Helper Methods                   │
    // └────────────────────────────────────────────────────┘

    /// <summary>
    /// Assert whether a given file path is absolute or relative.
    /// </summary>
    /// <param name="path">Path to check for absoluteness.</param>
    private static bool IsAbsolutePath(string path)
    {
        return !String.IsNullOrWhiteSpace(path)
            && path.IndexOfAny(System.IO.Path.GetInvalidPathChars().ToArray()) == -1
            && Path.IsPathRooted(path);
    }

    private static readonly string REGEX_NEWLINE = @"(?:\r\n|\r|\n)";
    // Matches blank lines, or both unix or windows comment initilaizers
    private static readonly string REGEX_INI_COMMENT = @"^\s*(?:[;#].*|\s*)$";
    private static readonly string REGEX_INI_SECTION = @"^\s*\[([^\]]+)\]\s*$";
    private static readonly string REGEX_INI_FIELD = @"^\s*([0-9A-Za-z]+)\s*=\s*(.+)$";

    public async Task<string?> ReadIconFromConfigFile(string filePath)
    {
        return await ReadIconFromConfigFile(filePath, default(CancellationToken));
    }

    /// <summary>
    /// Read configured cover location from <see cref="filePath"/>.
    /// </summary>
    public async Task<string?> ReadIconFromConfigFile(string filePath, CancellationToken cancellationToken)
    {
        FileStream fileStream;
        try
        {
            fileStream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true);
        }
        catch (System.IO.IOException err)
        {
            _logger.LogError(err, "Failed to read config file={file}", filePath);
            return null;
        }

        byte[] buffer = new byte[0x1000];

        int numRead;
        string? currentSection = null;
        string? partialLine = null;
        while ((numRead = await fileStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) != 0
               || partialLine != null)
        {
            // Process what was just read, prepending any partially read trailing
            // output from the previous loop if necessary. This is required in the
            // circumstance where a config file doesn't have a propper trailing
            // newline.
            string text = "";
            if (partialLine != null)
            {
                text = partialLine;
                partialLine = null;
            }
            if (numRead > 0)
            {
                try
                {
                    text += Encoding.ASCII.GetString(buffer, 0, numRead);
                }
                catch (Exception err)
                {
                    if (err is DecoderFallbackException || err is ArgumentException)
                    {
                        _logger.LogError(err, "Failed to read chunk from file={file}", filePath);
                        return null;
                    }
                    throw;
                }
            }

            // Split the read text into individual lines, saving the incompletely
            // read last line (if it was in fact incomplete) for a later read and
            // loop to process.
            string[] lines = Regex.Split(text, REGEX_NEWLINE);
            if (Regex.IsMatch(text, REGEX_NEWLINE + "$"))
            {
                partialLine = lines.Last();
                lines = lines.Take(lines.Count() - 1).ToArray();
            }

            // Process each line until we reach a field called FieldName in the
            // section SectionName.
            foreach (string line in lines)
            {
                Match sectionMatch;
                Match fieldMatch;
                if (Regex.IsMatch(line, REGEX_INI_COMMENT))
                {
                    continue;
                }
                else if ((sectionMatch = Regex.Match(line, REGEX_INI_SECTION)).Success)
                {
                    currentSection = sectionMatch.Groups[1].Value;
                }
                else if ((fieldMatch = Regex.Match(line, REGEX_INI_FIELD)).Success)
                {
                    var key = fieldMatch.Groups[1].Value;
                    var val = fieldMatch.Groups[2].Value;
                    if (key == FieldName)
                    {
                        if (currentSection != SectionName)
                        {
                            _logger.LogWarning("Found icon definition to icon={icon} from file={file} but its in incorrect section={section}", val, filePath, currentSection);
                            continue;
                        }
                        return val;
                    }
                }
                else
                {
                    _logger.LogWarning("Unable to parse line={line} from file={file}", line, filePath);
                }
            }
        }
        _logger.LogDebug("Failed to find Icon in file={file}", filePath);
        return null;
    }
}
