namespace GrandSluggers.Sim;

public static class PresetTeams
{
    public static Team SparkAllStars(ContentCatalog content) => content.Team(
        "Spark All-Stars",
        "rio", "nico", "pip", "marlow", "vale", "lace", "zig", "dart", "vine");

    public static Team EmberCourt(ContentCatalog content) => content.Team(
        "Ember Court",
        "ashlord", "cinder", "soot", "brondo", "boom", "konga", "frost", "grit", "hex");

    public static Team MixedRivals(ContentCatalog content) => content.Team(
        "Mixed Rivals",
        "rio", "ashlord", "brondo", "cinder", "vale", "boom", "konga", "soot", "frost");
}
