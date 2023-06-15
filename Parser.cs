using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Kum;

class ParserException : Exception
{
    public ParserException(string message, Location location) : base(message)
    {
        Location = location;
    }

    public Location Location { get; }

    public override string ToString() => $"""
            Parser failed at {Location}
            {Message}
            """;
}

class Location
{
    public int Row { get; private set; } = 1;
    public int Col { get; private set; } = 1;

    public void IncRow(int by = 1)
    {
        Row += by;
        Col = 1;
    }

    public void IncCol(int by = 1) => Col += by;

    public override string ToString() => $"{Row}:{Col}";
}

partial class Parser
{
    const char Quote = '\'';
    const char Break = '\n';
    const char Space = ' ';
    const char BackSlash = '\\';

    public List<Token> Parse(ReadOnlySpan<char> code)
    {
        var result = new List<Token>();
        var location = new Location();
        for (var i = 0; i < code.Length && i != -1;)
        {
            var slice = code[i..];
            if (slice[0] != Space)
            {
                var str = new string(slice);
                Match match;
                if ((match = IdentRegex().Match(str)).Success)
                {
                    i += match.Length;
                    location.IncCol(match.Length);
                    result.Add(new(match.Value, TokenType.Ident));
                }
                else if ((match = NumberRegex().Match(str)).Success)
                {
                    i += match.Length;
                    location.IncCol(match.Length);
                    result.Add(new(
                        double.Parse(match.Value, CultureInfo.InvariantCulture),
                        TokenType.Number
                    ));
                }
                else if (IsString(slice))
                {
                    str = GetString(slice, location);
                    i += str.Length;
                    result.Add(new(str, TokenType.String));
                }
                else if (IsLineBreak(slice))
                {
                    i++;
                    location.IncRow();
                    result.Add(new(null, TokenType.LineBreak));
                }
                else
                {
                    throw new ParserException($"Unknown symbol, tokens: {string.Join(", ", result)}", location);
                }
            }
            i += slice.IndexOfAnyExcept(Space);
        }
        return result;
    }

    [GeneratedRegex("^[а-я]+")]
    private partial Regex IdentRegex();

    [GeneratedRegex("^[0-9]+(\\.[0-9]+)?")]
    private partial Regex NumberRegex();

    private bool IsString(ReadOnlySpan<char> code)
    {
        return code[0] == Quote;
    }

    private string GetString(ReadOnlySpan<char> code, Location location)
    {
        var sb = new StringBuilder(Quote.ToString());
        var next = code[1..];
        while (true)
        {
            var idx = next.IndexOfAny(Quote, Break);
            location.IncCol(idx == -1 ? next.Length : idx + 1);
            if (idx == -1)
            {
                throw new ParserException(
                    "Qoute or line-break not found",
                    location
                );
            }
            var backSlashes = 0;
            for (var i = idx; i >= 0; i--)
            {
                if (next[i] == BackSlash) backSlashes++;
                else break;
            }
            if (backSlashes % 2 == 0)
            {
                switch (next[idx])
                {
                    case Quote:
                        sb.Append(next[..(idx + 1)]);
                        return sb.ToString();
                    case Break:
                        throw new ParserException(
                            "String is terminated by line-break instead closing quote",
                            location
                        );
                }
            }
            else
            {
                sb.Append(next[..(idx + 1)]);
                next = next[(idx + 1)..];
            }
        }
    }

    private bool IsLineBreak(ReadOnlySpan<char> code) => code.StartsWith("\n") || code.StartsWith("\r\n");
}
