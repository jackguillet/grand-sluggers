using GrandSluggers.Play;

var demo = args.Contains("--demo") || args.Contains("-d");
var two = args.Contains("--two") || args.Contains("-2");
var park = "harbor-diamond";
var seed = 7;
for (var i = 0; i < args.Length - 1; i++)
{
    if (args[i] is "--seed" or "-s" && int.TryParse(args[i + 1], out var n))
        seed = n;
    if (args[i] is "--park" or "-p")
        park = args[i + 1];
}

using var game = new Game(demo, seed, park, two);
game.Run();
