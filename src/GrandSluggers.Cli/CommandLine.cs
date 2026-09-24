using System.Globalization;

namespace GrandSluggers.Cli;

/// <summary>A command line the CLI refuses: exit 2, the message on stderr, nothing run.</summary>
sealed class UsageException(string message) : Exception(message);

enum Arity
{
    /// <summary>A flag alone: <c>--night</c>.</summary>
    Switch,
    /// <summary>A flag and the token after it, whatever it is: <c>--seed -3</c>.</summary>
    Value,
    /// <summary>A flag and the token after it when that token is not a flag: <c>--trace</c> or <c>--trace out.json</c>.</summary>
    OptionalValue
}

/// <summary>One flag a command accepts. <see cref="Name"/> is the key the command reads it by.</summary>
sealed record Option(string Name, Arity Arity, params string[] Aliases)
{
    public bool Matches(string token) => token == Name || Aliases.Contains(token);
}

/// <summary>
/// A command's arguments, parsed against the flags it declares. An unknown flag, a repeated flag, a flag
/// missing its value and a positional past the command's count are refused here, so a typo cannot run the
/// default game and read as a pass. Values are checked where they are read, by the same exception.
/// </summary>
sealed class CommandLine
{
    readonly Dictionary<string, string?> _given = [];
    readonly List<string> _positionals = [];

    CommandLine() { }

    public static CommandLine Parse(IReadOnlyList<string> args, IReadOnlyList<Option> options, int maxPositionals)
    {
        var line = new CommandLine();
        for (var i = 0; i < args.Count; i++)
        {
            var token = args[i];
            if (!token.StartsWith('-'))
            {
                if (line._positionals.Count >= maxPositionals)
                    throw new UsageException($"unexpected argument '{token}'");
                line._positionals.Add(token);
                continue;
            }
            var option = options.FirstOrDefault(o => o.Matches(token))
                ?? throw new UsageException($"unknown flag '{token}'");
            if (line._given.ContainsKey(option.Name))
                throw new UsageException($"{option.Name} is given twice");
            string? value = null;
            switch (option.Arity)
            {
                case Arity.Value:
                    if (i + 1 >= args.Count) throw new UsageException($"{option.Name} needs a value");
                    value = args[++i];
                    break;
                case Arity.OptionalValue:
                    if (i + 1 < args.Count && !args[i + 1].StartsWith('-')) value = args[++i];
                    break;
            }
            line._given[option.Name] = value;
        }
        return line;
    }

    /// <summary>The flags given, by <see cref="Option.Name"/>.</summary>
    public IEnumerable<string> Given => _given.Keys;

    public bool Has(string name) => _given.ContainsKey(name);

    public string? Positional(int index) => index < _positionals.Count ? _positionals[index] : null;

    /// <summary>The flag's value, or <paramref name="fallback"/> when the flag is absent.</summary>
    public string? Text(string name, string? fallback = null) =>
        _given.TryGetValue(name, out var value) ? value : fallback;

    public int Int(string name, int fallback)
    {
        if (!_given.TryGetValue(name, out var value)) return fallback;
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n)
            ? n
            : throw new UsageException($"{name} must be a whole number; got '{value}'");
    }

    /// <summary><c>on</c> or <c>off</c>; anything else is refused rather than read as the default.</summary>
    public bool OnOff(string name, bool fallback) => Text(name) switch
    {
        null when !Has(name) => fallback,
        "on" => true,
        "off" => false,
        var other => throw new UsageException($"{name} must be on or off; got '{other}'")
    };

    /// <summary>The value when it is one of <paramref name="allowed"/>, null when the flag is absent.</summary>
    public string? OneOf(string name, IReadOnlyList<string> allowed)
    {
        var value = Text(name);
        if (!Has(name)) return null;
        return value is not null && allowed.Contains(value)
            ? value
            : throw new UsageException($"{name} must be one of {string.Join("|", allowed)}; got '{value}'");
    }
}
