namespace Kum;

enum TokenType
{
    Ident,
    Number,
    String,
    LineBreak
}

class Token
{
    public Token(object? value, TokenType tokenType)
    {
        Value = value;
        Type = tokenType;
    }

    public object? Value { get; }
    public TokenType Type { get; }

    public override string ToString() => $"Token(value={Value}, type={Type})";
}
