namespace DatabaseSchemaChecker;

/// <summary>
/// Compares the CHECK expression syntax used by EF with MySQL's CHECK_CLAUSE
/// rendering. Known SQL operators are parsed into a precedence-aware tree so
/// redundant grouping can be ignored without changing the meaning of the
/// expression. Expressions outside the supported grammar use a conservative
/// token representation and therefore fail closed.
/// </summary>
internal static class CheckExpressionNormalizer
{
    public static string Normalize(string expression)
    {
        try
        {
            var parser = new Parser(Tokenize(expression));
            var tree = parser.ParseExpression();
            if (!parser.AtEnd)
                throw new FormatException("Unsupported trailing CHECK syntax.");
            return Serialize(tree);
        }
        catch (FormatException)
        {
            // Do not guess about the meaning of syntax the parser does not
            // understand. Token boundaries, grouping, operators and literals
            // remain significant in this fallback representation.
            return ConservativeTokenForm(expression);
        }
    }

    private static List<Token> Tokenize(string expression)
    {
        var tokens = new List<Token>();
        for (var index = 0; index < expression.Length;)
        {
            var current = expression[index];
            if (char.IsWhiteSpace(current))
            {
                index++;
                continue;
            }

            if (current == '_' && IsUtf8Mb4EscapedMetadataLiteralIntroducer(expression, index))
            {
                var (literal, nextIndex) = ReadUtf8Mb4EscapedMetadataLiteral(expression, index);
                tokens.Add(new(TokenKind.String, literal));
                index = nextIndex;
                continue;
            }

            if (current == '_' && IsUtf8Mb4LiteralIntroducer(expression, index))
            {
                index += "_utf8mb4".Length;
                continue;
            }

            if (current == '`')
            {
                var (value, nextIndex) = ReadQuoted(expression, index, '`', unescapeDoubledQuote: true);
                tokens.Add(new(TokenKind.QuotedIdentifier, value.ToUpperInvariant()));
                index = nextIndex;
                continue;
            }

            if (current is '\'' or '"')
            {
                var literalStart = index;
                var (_, nextIndex) = ReadQuoted(expression, index, current, unescapeDoubledQuote: true);
                // Keep the quote delimiter and escapes: depending on SQL mode,
                // double quotes can differ from single-quoted string literals,
                // and backslash/doubled-quote forms depend on MySQL SQL mode.
                tokens.Add(new(TokenKind.String, expression[literalStart..nextIndex]));
                index = nextIndex;
                continue;
            }

            if (IsWordStart(current))
            {
                var start = index++;
                while (index < expression.Length && IsWordPart(expression[index]))
                    index++;
                tokens.Add(new(TokenKind.Word, expression[start..index].ToUpperInvariant()));
                continue;
            }

            if (char.IsDigit(current) ||
                current == '.' && index + 1 < expression.Length && char.IsDigit(expression[index + 1]))
            {
                var start = index++;
                while (index < expression.Length && char.IsDigit(expression[index]))
                    index++;
                if (index < expression.Length && expression[index] == '.')
                {
                    index++;
                    while (index < expression.Length && char.IsDigit(expression[index]))
                        index++;
                }
                if (index < expression.Length && expression[index] is 'e' or 'E')
                {
                    var exponentStart = index++;
                    if (index < expression.Length && expression[index] is '+' or '-')
                        index++;
                    var digitsStart = index;
                    while (index < expression.Length && char.IsDigit(expression[index]))
                        index++;
                    if (digitsStart == index)
                        index = exponentStart;
                }
                tokens.Add(new(TokenKind.Number, expression[start..index]));
                continue;
            }

            switch (current)
            {
                case '(':
                    tokens.Add(new(TokenKind.LeftParen, current.ToString()));
                    index++;
                    continue;
                case ')':
                    tokens.Add(new(TokenKind.RightParen, current.ToString()));
                    index++;
                    continue;
                case ',':
                    tokens.Add(new(TokenKind.Comma, current.ToString()));
                    index++;
                    continue;
                case '.':
                    tokens.Add(new(TokenKind.Dot, current.ToString()));
                    index++;
                    continue;
            }

            var op = ReadOperator(expression, ref index);
            tokens.Add(new(TokenKind.Operator, op));
        }

        if (tokens.Count > 0 && tokens[^1] is { Kind: TokenKind.Operator, Text: ";" })
            tokens.RemoveAt(tokens.Count - 1);
        return tokens;
    }

    private static bool IsUtf8Mb4LiteralIntroducer(string expression, int index)
    {
        const string introducer = "_utf8mb4";
        if (index > 0 && IsWordPart(expression[index - 1]))
            return false;
        if (expression.Length - index <= introducer.Length ||
            !expression.AsSpan(index, introducer.Length).Equals(introducer.AsSpan(), StringComparison.OrdinalIgnoreCase))
            return false;
        return expression[index + introducer.Length] is '\'' or '"';
    }

    private static bool IsUtf8Mb4EscapedMetadataLiteralIntroducer(string expression, int index)
    {
        const string escapedIntroducer = "_utf8mb4\\'";
        if (index > 0 && IsWordPart(expression[index - 1]))
            return false;
        return expression.Length - index >= escapedIntroducer.Length &&
               expression.AsSpan(index, escapedIntroducer.Length)
                   .Equals(escapedIntroducer.AsSpan(), StringComparison.OrdinalIgnoreCase);
    }

    private static (string Literal, int NextIndex) ReadUtf8Mb4EscapedMetadataLiteral(string expression, int start)
    {
        const string escapedIntroducer = "_utf8mb4\\'";
        var bodyStart = start + escapedIntroducer.Length;
        var body = new System.Text.StringBuilder();
        for (var index = bodyStart; index < expression.Length; index++)
        {
            var current = expression[index];
            if (current == '\\')
            {
                if (index + 1 < expression.Length && expression[index + 1] == '\'' && body.Length > 0)
                {
                    var nextIndex = index + 2;
                    if (nextIndex == expression.Length || char.IsWhiteSpace(expression[nextIndex]) ||
                        expression[nextIndex] is ',' or ')')
                        return ($"'{body}'", nextIndex);
                }

                throw new FormatException("Unsupported escape in MySQL CHECK metadata literal.");
            }

            // MySQL renders these CHECK_CLAUSE status literals with escaped
            // quote delimiters. Restrict the decoded body to the simple ASCII
            // identifiers present in the verified report; other forms fail closed.
            if (!char.IsAsciiLetterOrDigit(current) && current != '_')
                throw new FormatException("Unsupported character in MySQL CHECK metadata literal.");
            body.Append(current);
        }

        throw new FormatException("Unterminated MySQL CHECK metadata literal.");
    }

    private static (string Value, int NextIndex) ReadQuoted(
        string expression,
        int start,
        char quote,
        bool unescapeDoubledQuote)
    {
        var value = new System.Text.StringBuilder();
        for (var index = start + 1; index < expression.Length; index++)
        {
            var current = expression[index];
            if (current == '\\' && quote != '`' && index + 1 < expression.Length)
            {
                value.Append(current).Append(expression[++index]);
                continue;
            }
            if (current == quote)
            {
                if (unescapeDoubledQuote && index + 1 < expression.Length && expression[index + 1] == quote)
                {
                    value.Append(quote);
                    index++;
                    continue;
                }
                return (value.ToString(), index + 1);
            }
            value.Append(current);
        }
        throw new FormatException("Unterminated quoted token in CHECK expression.");
    }

    private static string ReadOperator(string expression, ref int index)
    {
        foreach (var candidate in new[] { "<=>", "<=", ">=", "<>", "!=", "&&", "||" })
        {
            if (expression.AsSpan(index).StartsWith(candidate.AsSpan(), StringComparison.Ordinal))
            {
                index += candidate.Length;
                return candidate;
            }
        }

        return expression[index++].ToString();
    }

    private static bool IsWordStart(char value) => char.IsLetter(value) || value is '_' or '$';
    private static bool IsWordPart(char value) => char.IsLetterOrDigit(value) || value is '_' or '$';

    private static string ConservativeTokenForm(string expression)
    {
        try
        {
            return string.Concat(Tokenize(expression).Select(token =>
                $"{(int)token.Kind}:{token.Text.Length}:{token.Text};"));
        }
        catch (FormatException)
        {
            return "RAW:" + expression.Trim();
        }
    }

    private static string Serialize(Expression expression)
    {
        if (expression.Children.Count == 0)
            return $"A{expression.Kind.Length}:{expression.Kind}{expression.Value.Length}:{expression.Value}";

        var children = string.Concat(expression.Children.Select(child =>
        {
            var serialized = Serialize(child);
            return $"{serialized.Length}:{serialized}";
        }));
        return $"N{expression.Kind.Length}:{expression.Kind}{expression.Children.Count}:{children}";
    }

    private static Expression Atom(Token token) =>
        new("TOKEN_" + token.Kind, token.Text, Array.Empty<Expression>());

    private static Expression Node(string kind, params Expression[] children) =>
        new(kind, string.Empty, children);

    private static Expression Associative(string kind, Expression left, Expression right)
    {
        var children = new List<Expression>();
        if (left.Kind == kind)
            children.AddRange(left.Children);
        else
            children.Add(left);
        if (right.Kind == kind)
            children.AddRange(right.Children);
        else
            children.Add(right);
        return new(kind, string.Empty, children);
    }

    private enum TokenKind
    {
        Word,
        QuotedIdentifier,
        String,
        Number,
        Operator,
        LeftParen,
        RightParen,
        Comma,
        Dot
    }

    private sealed record Token(TokenKind Kind, string Text);
    private sealed record Expression(string Kind, string Value, IReadOnlyList<Expression> Children);

    private sealed class Parser(IReadOnlyList<Token> tokens)
    {
        private int _position;
        public bool AtEnd => _position == tokens.Count;

        public Expression ParseExpression() => ParseOr();

        private Expression ParseOr()
        {
            var expression = ParseAnd();
            while (MatchWord("OR"))
                expression = Associative("OR", expression, ParseAnd());
            return expression;
        }

        private Expression ParseAnd()
        {
            var expression = ParseNot();
            while (MatchWord("AND"))
                expression = Associative("AND", expression, ParseNot());
            return expression;
        }

        private Expression ParseNot()
        {
            if (MatchWord("NOT"))
                return Node("NOT", ParseNot());
            return ParsePredicate();
        }

        private Expression ParsePredicate()
        {
            var left = ParseAdditive();
            var negated = false;
            if (IsWord("NOT") && NextWordIs("IN", "BETWEEN", "LIKE", "REGEXP", "RLIKE"))
            {
                _position++;
                negated = true;
            }

            if (MatchWord("IS"))
            {
                var isNot = MatchWord("NOT");
                var value = TakeWord("NULL", "TRUE", "FALSE");
                return Node(isNot ? "IS_NOT" : "IS", left,
                    Atom(new Token(TokenKind.Word, value)));
            }

            if (MatchWord("IN"))
            {
                Require(TokenKind.LeftParen);
                var values = new List<Expression>();
                if (!Is(TokenKind.RightParen))
                {
                    values.Add(ParseOr());
                    while (Match(TokenKind.Comma))
                        values.Add(ParseOr());
                }
                Require(TokenKind.RightParen);
                return Node(negated ? "NOT_IN" : "IN", new[] { left }.Concat(values).ToArray());
            }

            if (MatchWord("BETWEEN"))
            {
                var lower = ParseAdditive();
                RequireWord("AND");
                var upper = ParseAdditive();
                return Node(negated ? "NOT_BETWEEN" : "BETWEEN", left, lower, upper);
            }

            if (IsWord("LIKE") || IsWord("REGEXP") || IsWord("RLIKE"))
            {
                var operation = tokens[_position++].Text;
                var right = ParseAdditive();
                var children = new List<Expression> { left, right };
                if (MatchWord("ESCAPE"))
                    children.Add(ParseAdditive());
                return Node((negated ? "NOT_" : string.Empty) + operation, children.ToArray());
            }

            if (negated)
                throw new FormatException("Unsupported NOT predicate.");

            if (IsComparisonOperator(Current))
            {
                var operation = tokens[_position++].Text;
                return Node("COMPARE_" + operation, left, ParseAdditive());
            }

            return left;
        }

        private Expression ParseAdditive()
        {
            var expression = ParseMultiplicative();
            while (IsOperator("+") || IsOperator("-"))
            {
                var operation = tokens[_position++].Text;
                expression = Node("ARITH_" + operation, expression, ParseMultiplicative());
            }
            return expression;
        }

        private Expression ParseMultiplicative()
        {
            var expression = ParseUnary();
            while (IsOperator("*") || IsOperator("/") || IsOperator("%") || IsWord("DIV") || IsWord("MOD"))
            {
                var operation = tokens[_position++].Text;
                expression = Node("ARITH_" + operation, expression, ParseUnary());
            }
            return expression;
        }

        private Expression ParseUnary()
        {
            if (IsOperator("+") || IsOperator("-") || IsOperator("~"))
            {
                var operation = tokens[_position++].Text;
                return Node("UNARY_" + operation, ParseUnary());
            }
            return ParsePrimary();
        }

        private Expression ParsePrimary()
        {
            if (Match(TokenKind.LeftParen))
            {
                var expression = ParseOr();
                Require(TokenKind.RightParen);
                return expression;
            }

            if (Current is { Kind: TokenKind.Word or TokenKind.QuotedIdentifier })
            {
                var identifier = ParseIdentifierPath();
                if (Match(TokenKind.LeftParen))
                {
                    var arguments = new List<Expression>();
                    if (!Is(TokenKind.RightParen))
                    {
                        arguments.Add(ParseOr());
                        while (Match(TokenKind.Comma))
                            arguments.Add(ParseOr());
                    }
                    Require(TokenKind.RightParen);
                    return Node("CALL_" + identifier, arguments.ToArray());
                }
                return new("IDENTIFIER", identifier, Array.Empty<Expression>());
            }

            if (Current is { Kind: TokenKind.String or TokenKind.Number })
                return Atom(tokens[_position++]);

            throw new FormatException("Unsupported CHECK primary expression.");
        }

        private string ParseIdentifierPath()
        {
            var identifier = tokens[_position++];
            var path = $"I{identifier.Text.Length}:{identifier.Text}";
            while (Match(TokenKind.Dot))
            {
                if (Current is not { Kind: TokenKind.Word or TokenKind.QuotedIdentifier })
                    throw new FormatException("Invalid qualified identifier.");
                identifier = tokens[_position++];
                path += $".I{identifier.Text.Length}:{identifier.Text}";
            }
            return path;
        }

        private static bool IsComparisonOperator(Token? token) =>
            token is { Kind: TokenKind.Operator, Text: "=" or "<>" or "!=" or "<" or ">" or "<=" or ">=" or "<=>" };

        private Token? Current => _position < tokens.Count ? tokens[_position] : null;
        private bool Is(TokenKind kind) => Current?.Kind == kind;
        private bool IsOperator(string operation) => Current is { Kind: TokenKind.Operator } token && token.Text == operation;
        private bool IsWord(string word) => Current is { Kind: TokenKind.Word } token && token.Text == word;
        private bool NextWordIs(params string[] words) =>
            _position + 1 < tokens.Count && tokens[_position + 1].Kind == TokenKind.Word &&
            words.Contains(tokens[_position + 1].Text, StringComparer.Ordinal);

        private bool Match(TokenKind kind)
        {
            if (!Is(kind))
                return false;
            _position++;
            return true;
        }

        private bool MatchWord(string word)
        {
            if (!IsWord(word))
                return false;
            _position++;
            return true;
        }

        private string TakeWord(params string[] words)
        {
            foreach (var word in words)
            {
                if (MatchWord(word))
                    return word;
            }
            throw new FormatException("Expected a CHECK predicate keyword.");
        }

        private void Require(TokenKind kind)
        {
            if (!Match(kind))
                throw new FormatException("Unexpected CHECK expression token.");
        }

        private void RequireWord(string word)
        {
            if (!MatchWord(word))
                throw new FormatException("Expected a CHECK operator keyword.");
        }
    }
}
