using GrandSluggers.Play;

var demo = args.Contains("--demo") || args.Contains("-d");
var seed = 7;
for (var i = 0; i < args.Length - 1; i++)
    if (args[i] is "--seed" or "-s" && int.TryParse(args[i + 1], out var n))
        seed = n;

using var game = new Game(demo, seed);
game.Run();
