namespace Esolang.Funge.Parser;

/// <summary>
/// Parses Funge-98 source text into a <see cref="FungeSpace"/>.
/// </summary>
public static class FungeParser
{
    /// <summary>
    /// Parses a Funge-98 source string into a populated <see cref="FungeSpace"/>.
    /// Each character is placed at its (column, row) coordinate.
    /// Space characters (ASCII 32) are not stored; they use the default cell value.
    /// </summary>
    /// <param name="source">The Funge-98 source text.</param>
    /// <returns>A <see cref="FungeSpace"/> containing the program.</returns>
    public static FungeSpace Parse(string source)
    {
        var space = new FungeSpace();
        int x = 0, y = 0, z = 0;
        foreach (var ch in source)
        {
            if (ch == '\r') continue;
            if (ch == '\n') { x = 0; y++; continue; }
            if (ch == '\f') { x = 0; y = 0; z++; continue; }

            var cell = ch switch
            {
                '\t' or '\v' => ' ',
                _ => ch,
            };

            var pos = new FungeVector(x, y, z);
            space.EnsureBounds(pos);
            if (cell != ' ')
                space[pos] = cell;
            x++;
        }
        return space;
    }

    /// <summary>
    /// Reads a file and parses its contents as a Funge-98 program.
    /// </summary>
    /// <param name="path">Path to the source file.</param>
    /// <returns>A <see cref="FungeSpace"/> containing the program.</returns>
    public static FungeSpace ParseFile(string path) => Parse(File.ReadAllText(path));
}
