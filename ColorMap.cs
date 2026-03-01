using System.Drawing;

namespace FeColorizer;

public static class ColorMap
{
    public static readonly Dictionary<char, (string Name, Color Color)> Map = new()
    {
        ['A'] = ("Aqua",     Color.FromArgb(0,   210, 211)),
        ['B'] = ("Blue",     Color.FromArgb(33,  150, 243)),
        ['C'] = ("Cyan",     Color.FromArgb(0,   188, 212)),
        ['D'] = ("Denim",    Color.FromArgb(21,  96,  189)),
        ['E'] = ("Emerald",  Color.FromArgb(80,  200, 120)),
        ['F'] = ("Fuchsia",  Color.FromArgb(255, 0,   144)),
        ['G'] = ("Green",    Color.FromArgb(67,  160, 71 )),
        ['H'] = ("Hazel",    Color.FromArgb(142, 118, 24 )),
        ['I'] = ("Indigo",   Color.FromArgb(63,  81,  181)),
        ['J'] = ("Jade",     Color.FromArgb(0,   168, 107)),
        ['K'] = ("Khaki",    Color.FromArgb(195, 176, 145)),
        ['L'] = ("Lavender", Color.FromArgb(150, 123, 182)),
        ['M'] = ("Magenta",  Color.FromArgb(236, 64,  122)),
        ['N'] = ("Navy",     Color.FromArgb(0,   31,  91 )),
        ['O'] = ("Orange",   Color.FromArgb(255, 152, 0  )),
        ['P'] = ("Purple",   Color.FromArgb(156, 39,  176)),
        ['Q'] = ("Quartz",   Color.FromArgb(183, 164, 208)),
        ['R'] = ("Red",      Color.FromArgb(229, 57,  53 )),
        ['S'] = ("Silver",   Color.FromArgb(158, 158, 158)),
        ['T'] = ("Turquoise",Color.FromArgb(29,  233, 182)),
        ['U'] = ("Umber",    Color.FromArgb(99,  81,  71 )),
        ['V'] = ("Violet",   Color.FromArgb(143, 0,   255)),
        ['W'] = ("White",    Color.FromArgb(224, 224, 224)),
        ['X'] = ("Xanadu",   Color.FromArgb(115, 134, 120)),
        ['Y'] = ("Yellow",   Color.FromArgb(255, 214, 0  )),
        ['Z'] = ("Zinc",     Color.FromArgb(113, 121, 126)),
    };

    /// <summary>
    /// Finds the first A-Z letter in <paramref name="folderName"/>, skipping
    /// leading digits, hyphens, spaces, etc. (e.g. "01-Film" → 'F').
    /// Returns null if no letter is found.
    /// </summary>
    public static char? GetLetter(string folderName)
    {
        foreach (char c in folderName)
        {
            if (!char.IsLetter(c)) continue;
            char upper = char.ToUpperInvariant(c);
            return Map.ContainsKey(upper) ? upper : null;
        }
        return null;
    }

    /// <summary>Returns the mapped color for a folder name, or null if unmapped.</summary>
    public static Color? GetColor(string folderName)
    {
        char? letter = GetLetter(folderName);
        return letter.HasValue ? Map[letter.Value].Color : null;
    }
}
