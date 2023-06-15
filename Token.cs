namespace Kum;

enum TokenType
{
    Ident,
    Number,
    Operator,
    String,
    LineBreak
}

enum Operator
{
    Add,
    Sub,
    Mul,
    Div,
    AddAssign,
    SubAssign,
    MulAssign,
    DivAssign
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
