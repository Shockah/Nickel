using System.Diagnostics.CodeAnalysis;

namespace Nanoray.PluginManager;

public static class SemanticVersionParser
{
    public static bool TryParse(string? versionStr, [MaybeNullWhen(false)] out SemanticVersion version)
    {
        version = default;
        int patch = 0;
        string? prereleaseTag = null;

        // normalize
        versionStr = versionStr?.Trim();
        if (string.IsNullOrWhiteSpace(versionStr))
            return false;
        char[] raw = versionStr.ToCharArray();

        // read major/minor version
        int i = 0;
        if (!TryParseVersionPart(raw, ref i, out int major) || !TryParseLiteral(raw, ref i, '.') || !TryParseVersionPart(raw, ref i, out int minor))
            return false;

        // read optional patch version
        if (TryParseLiteral(raw, ref i, '.') && !TryParseVersionPart(raw, ref i, out patch))
            return false;

        // read optional prerelease tag
        if (TryParseLiteral(raw, ref i, '-') && !TryParseTag(raw, ref i, out prereleaseTag))
            return false;

        // validate
        if (i != versionStr.Length)
            return false;

        version = new() { MajorVersion = major, MinorVersion = minor, PatchVersion = patch, PrereleaseTag = prereleaseTag };
        return true;
    }

    private static bool TryParseVersionPart(char[] raw, ref int index, out int part)
    {
        part = 0;

        // take digits
        string str = "";
        for (int i = index; i < raw.Length && char.IsDigit(raw[i]); i++)
            str += raw[i];

        // validate
        if (str.Length == 0)
            return false;
        if (str.Length > 1 && str[0] == '0')
            return false; // can't have leading zeros

        // parse
        part = int.Parse(str);
        index += str.Length;
        return true;
    }

    private static bool TryParseLiteral(char[] raw, ref int index, char ch)
    {
        if (index >= raw.Length || raw[index] != ch)
            return false;

        index++;
        return true;
    }

    private static bool TryParseTag(char[] raw, ref int index, [NotNullWhen(true)] out string? tag)
    {
        // read tag length
        int length = 0;
        for (int i = index; i < raw.Length && (char.IsLetterOrDigit(raw[i]) || raw[i] == '-' || raw[i] == '.'); i++)
            length++;

        // validate
        if (length == 0)
        {
            tag = null;
            return false;
        }

        // parse
        tag = new string(raw, index, length);
        index += length;
        return true;
    }
}
