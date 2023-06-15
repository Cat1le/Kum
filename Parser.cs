using System.Diagnostics.CodeAnalysis;
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
                else if (TryGetOperator(slice, out var op, out var opLen))
                {
                    i += opLen + 1;
                    result.Add(new(op, TokenType.Operator));
                }
                else if (TryGetString(slice, location, out str, out var strLen))
                {
                    i += strLen + 1;
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
                    throw new ParserException($"Unknown symbol '{slice[0]}'", location);
                }
            }
            i += Math.Max(slice.IndexOfAnyExcept(Space), 0);
        }
        return result;
    }

    [GeneratedRegex("^[a-zа-я]+", RegexOptions.IgnoreCase)]
    private partial Regex IdentRegex();

    [GeneratedRegex("^[0-9]+(\\.[0-9]+)?")]
    private partial Regex NumberRegex();

    private bool TryGetOperator(ReadOnlySpan<char> code, out Operator? op, out int length)
    {
        length = 2;
        switch (code[..2])
        {
            case "+=":
                op = Operator.AddAssign;
                return true;
            case "-=":
                op = Operator.SubAssign;
                return true;
            case "*=":
                op = Operator.MulAssign;
                return true;
            case "/=":
                op = Operator.DivAssign;
                return true;
        }
        length = 1;
        switch (code[0])
        {
            case '+':
                op = Operator.Add;
                return true;
            case '-':
                op = Operator.Sub;
                return true;
            case '*':
                op = Operator.Mul;
                return true;
            case '/':
                op = Operator.Div;
                return true;
            default:
                op = null;
                length = -1;
                return false;
        }
    }

    private bool TryGetString(ReadOnlySpan<char> code, Location location, [NotNullWhen(true)] out string? value, out int length)
    {
        static int P(ReadOnlySpan<char> chars, StringBuilder sb)
        {
            var seen = false;
            foreach (var c in chars)
            {
                if (c == BackSlash)
                {
                    if (seen)
                    {
                        sb.Append(c);
                    }
                    seen = !seen;
                }
                else
                {
                    sb.Append(c);
                    seen = false;
                }
            }
            return chars.Length;
        }

        if (code[0] != Quote)
        {
            value = null;
            length = -1;
            return false;
        }
        var sb = new StringBuilder(Quote.ToString());
        var next = code[1..];
        length = 0;
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
            for (var i = idx - 1; i >= 0; i--)
            {
                if (next[i] == BackSlash) backSlashes++;
                else break;
            }
            if (backSlashes % 2 == 0)
            {
                switch (next[idx])
                {
                    case Quote:
                        length += P(next[..(idx + 1)], sb);
                        value = sb.ToString();
                        return true;
                    case Break:
                        throw new ParserException(
                            "String is terminated by line-break instead closing quote",
                            location
                        );
                }
            }
            else
            {
                length += P(next[..(idx + 1)], sb);
                next = next[(idx + 1)..];
            }
        }
    }

    private bool IsLineBreak(ReadOnlySpan<char> code) => code.StartsWith("\n") || code.StartsWith("\r\n");
}
