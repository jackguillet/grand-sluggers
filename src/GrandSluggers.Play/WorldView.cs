using System.Numerics;
using GrandSluggers.Sim;
using Raylib_cs;

namespace GrandSluggers.Play;

public static class WorldView
{
    public static void DrawPark(Park park, bool furnace)
    {
        var ice = park.Surface == "ice";
        var ash = park.Surface == "ash";
        var jungle = park.Id == "canopy-yard";
        var water = ash ? Palette.C(90, 28, 18) : ice ? Palette.C(190, 220, 240) : Palette.Water;
        var grass = ash ? Palette.C(72, 48, 40)
            : jungle ? Palette.C(36, 110, 52)
            : ice ? Palette.C(210, 230, 245)
            : Palette.Grass;
        var cut = ash ? Palette.C(58, 36, 32)
            : jungle ? Palette.C(28, 92, 44)
            : ice ? Palette.C(186, 214, 232)
            : Palette.CutGrass;

        Raylib.DrawCube(new Vector3(0, -1.2f, 220), 900, 2, 900, water);
        Raylib.DrawCube(new Vector3(0, -0.2f, 180), 560, 0.6f, 560, grass);
        for (var i = 0; i < 12; i++)
        {
            var z = 40 + i * 36;
            var c = i % 2 == 0 ? grass : cut;
            Raylib.DrawCube(new Vector3(0, 0.02f, z), 420, 0.08f, 34, c);
        }

        DrawInfieldDirt();
        DrawFoulLines();
        DrawBases();
        DrawMound();
        DrawFence(park, furnace || ash);
        DrawStands();
        if (jungle) DrawJungle();
        else if (ash) DrawKeep();
        else if (!ice) DrawHarbor();
        DrawBackstop();
        foreach (var h in park.Hazards)
        {
            var p = new Vector3((float)h.X, 0, (float)h.Z);
            switch (h.Type)
            {
                case "freeze_volume":
                    Raylib.DrawCylinder(p, (float)h.Radius, (float)h.Radius * 0.6f, 4.5f, 10, Palette.C(160, 230, 255));
                    Raylib.DrawSphere(new Vector3(p.X, 5.2f, p.Z), 1.6f, Palette.C(230, 250, 255));
                    break;
                case "warp_pipe":
                    Raylib.DrawCylinder(p, (float)h.Radius, (float)h.Radius * 0.7f, 6f, 10, Palette.C(40, 170, 70));
                    Raylib.DrawCylinder(new Vector3(p.X, 6f, p.Z), (float)h.Radius * 1.1f, (float)h.Radius, 0.5f, 10, Palette.C(30, 120, 50));
                    break;
                case "barrel":
                    Raylib.DrawCylinder(p, (float)h.Radius, (float)h.Radius * 0.85f, 5.2f, 10, Palette.C(118, 72, 28));
                    Raylib.DrawCylinder(new Vector3(p.X, 5.2f, p.Z), (float)h.Radius * 1.05f, (float)h.Radius, 0.4f, 10, Palette.C(72, 42, 18));
                    break;
                case "billboard":
                    Raylib.DrawCube(new Vector3(p.X, 18, p.Z), 24, 16, 2, Palette.Gold);
                    break;
                case "ac_unit":
                    Raylib.DrawCube(new Vector3(p.X, 2, p.Z), 8, 4, 8, Palette.C(140, 140, 148));
                    break;
                case "lava_pit":
                    Raylib.DrawCylinder(p, (float)h.Radius, (float)h.Radius, 0.6f, 12, Palette.C(220, 60, 18));
                    Raylib.DrawSphere(new Vector3(p.X, 1.4f, p.Z), (float)h.Radius * 0.35f, Palette.EmberFire);
                    break;
                case "fire_breath":
                    Raylib.DrawCylinder(p, (float)h.Radius, (float)h.Radius * 0.4f, 8f, 10, Palette.Fade(Palette.EmberFire, 160));
                    break;
                case "statue":
                    Raylib.DrawCube(new Vector3(p.X, 10, p.Z), 10, 20, 10, Palette.C(36, 24, 28));
                    Raylib.DrawSphere(new Vector3(p.X, 22, p.Z), 4.2f, Palette.C(28, 18, 22));
                    break;
                case "tree":
                    Raylib.DrawCylinder(p, 1.4f, 1.8f, 8f, 6, Palette.C(92, 54, 28));
                    Raylib.DrawSphere(new Vector3(p.X, 12, p.Z), (float)Math.Max(6, h.Radius), Palette.C(28, 110, 48));
                    break;
            }
        }
    }

    public static void DrawRing(double x, double z, float radius, Color color) =>
        Raylib.DrawCylinder(new Vector3((float)x, 0.15f, (float)z), radius, radius, 0.2f, 16, color);

    public static void DrawPerson(double x, double z, string faction, bool batting, bool pitching, float batAngle, bool star)
    {
        var body = Palette.Body(faction);
        var accent = Palette.Accent(faction);
        var skin = Palette.SkinTone(faction);
        var px = (float)x;
        var pz = (float)z;
        Raylib.DrawCapsule(new Vector3(px, 0.4f, pz), new Vector3(px, 4.3f, pz), 1.15f, 8, 6, body);
        Raylib.DrawSphere(new Vector3(px, 5.2f, pz), 1.08f, skin);
        Raylib.DrawCube(new Vector3(px, 6.15f, pz), 1.8f, 0.45f, 1.8f, accent);
        if (star)
            Raylib.DrawSphere(new Vector3(px, 7.3f, pz), 0.45f, Palette.Gold);
        if (batting)
        {
            var rad = batAngle * MathF.PI / 180f;
            var bx = px + MathF.Cos(rad) * 2.4f;
            var by = 3.4f + MathF.Sin(rad) * 2.1f;
            var bz = pz + 0.6f;
            Raylib.DrawCube(new Vector3(bx, by, bz), 0.35f, 3.4f, 0.35f,
                faction == "ember" ? Palette.C(28, 22, 22) : Palette.C(92, 48, 24));
        }
        if (pitching)
            Raylib.DrawSphere(new Vector3(px, 4.6f, pz + 1.4f), 0.45f, Palette.Skin);
    }

    public static void DrawBall(Vector3 p, bool heat, bool furnace, IReadOnlyList<Vector3>? trail)
    {
        if (trail is not null)
        {
            for (var i = 1; i < trail.Count; i++)
            {
                var a = (byte)(40 + i * 8);
                var c = heat || furnace ? Palette.Fade(Palette.EmberFire, a) : Palette.Fade(Palette.C(255, 255, 255), a);
                Raylib.DrawLine3D(trail[i - 1], trail[i], c);
            }
        }
        var color = heat || furnace ? Palette.EmberFire : Palette.Ball;
        Raylib.DrawSphere(p, 0.55f, color);
        if (heat)
            Raylib.DrawSphere(p + new Vector3(0, 0.2f, 0), 0.85f, Palette.Fade(Palette.EmberFire, 90));
    }

    public static void DrawFielderGhost(double x, double z, string faction) =>
        DrawPerson(x, z, faction, false, false, 0, false);

    public static Camera3D BattingCamera() => Cam(new Vector3(7, 9, -16), new Vector3(0, 4, 50));
    public static Camera3D PitchingCamera() => Cam(new Vector3(-6, 10, 82), new Vector3(0, 4, 8));
    public static Camera3D FollowCamera(Vector3 ball) =>
        Cam(ball + new Vector3(18, 16, -22), ball + new Vector3(0, 2, 8));
    public static Camera3D HighCamera() => Cam(new Vector3(0, 95, -40), new Vector3(0, 0, 140));

    static Camera3D Cam(Vector3 pos, Vector3 target) => new()
    {
        Position = pos,
        Target = target,
        Up = Vector3.UnitY,
        FovY = 48,
        Projection = CameraProjection.Perspective
    };

    static void DrawInfieldDirt()
    {
        var h = 0.08f;
        var home = new Vector3(0, h, 0);
        var first = new Vector3(63.64f, h, 63.64f);
        var second = new Vector3(0, h, 127.28f);
        var third = new Vector3(-63.64f, h, 63.64f);
        Raylib.DrawTriangle3D(home, first, second, Palette.Dirt);
        Raylib.DrawTriangle3D(home, second, third, Palette.Dirt);
        Raylib.DrawCylinder(new Vector3(0, 0, 0), 16, 16, 0.12f, 20, Palette.Dirt);
        Raylib.DrawCylinder(new Vector3(63.64f, 0, 63.64f), 10, 10, 0.12f, 14, Palette.Dirt);
        Raylib.DrawCylinder(new Vector3(-63.64f, 0, 63.64f), 10, 10, 0.12f, 14, Palette.Dirt);
        Raylib.DrawCylinder(new Vector3(0, 0, 127.28f), 10, 10, 0.12f, 14, Palette.Dirt);
    }

    static void DrawFoulLines()
    {
        Raylib.DrawLine3D(new Vector3(0, 0.2f, 0), new Vector3(240, 0.2f, 240), Palette.Chalk);
        Raylib.DrawLine3D(new Vector3(0, 0.2f, 0), new Vector3(-240, 0.2f, 240), Palette.Chalk);
        Raylib.DrawCylinder(new Vector3(0, 0.1f, 0), 1.4f, 1.4f, 0.2f, 10, Palette.Chalk);
    }

    static void DrawBases()
    {
        void Bag(float x, float z) =>
            Raylib.DrawCube(new Vector3(x, 0.25f, z), 2.2f, 0.4f, 2.2f, Palette.Chalk);
        Bag(63.64f, 63.64f);
        Bag(0, 127.28f);
        Bag(-63.64f, 63.64f);
        Raylib.DrawCube(new Vector3(0, 0.2f, -0.4f), 2.4f, 0.25f, 2.4f, Palette.Chalk);
    }

    static void DrawMound()
    {
        Raylib.DrawCylinder(new Vector3(0, 0, 60.5f), 9, 9, 1.1f, 16, Palette.Mound);
        Raylib.DrawCube(new Vector3(0, 1.15f, 60.5f), 1.8f, 0.15f, 0.4f, Palette.Chalk);
    }

    static void DrawFence(Park park, bool furnace)
    {
        var wall = furnace ? Palette.EmberFire : Palette.Fence;
        for (var i = -16; i <= 16; i++)
        {
            var t = i / 16f;
            var spray = t * 48f;
            var fence = (float)AtBatResolver.FenceAt(park, spray);
            var a = spray * MathF.PI / 180f;
            var x = MathF.Sin(a) * fence;
            var z = MathF.Cos(a) * fence;
            Raylib.DrawCube(new Vector3(x, 8, z), 10, 16, 3.2f, wall);
        }
        var lf = (float)park.LeftFenceFt;
        var rf = (float)park.RightFenceFt;
        Raylib.DrawCylinder(new Vector3(MathF.Sin(-0.78f) * lf, 0, MathF.Cos(-0.78f) * lf), 0.7f, 0.7f, 42, 8, Palette.FoulPole);
        Raylib.DrawCylinder(new Vector3(MathF.Sin(0.78f) * rf, 0, MathF.Cos(0.78f) * rf), 0.7f, 0.7f, 42, 8, Palette.FoulPole);
    }

    static void DrawStands()
    {
        Raylib.DrawCube(new Vector3(0, 14, -48), 90, 22, 18, Palette.C(232, 236, 240));
        Raylib.DrawCube(new Vector3(-95, 12, 40), 24, 18, 80, Palette.C(236, 240, 244));
        Raylib.DrawCube(new Vector3(95, 12, 40), 24, 18, 80, Palette.C(236, 240, 244));
        for (var i = -4; i <= 4; i++)
            Raylib.DrawCube(new Vector3(i * 8, 22, -52), 5, 4, 4, i == 0 ? Palette.Spark : Palette.C(200, 80, 70));
    }

    static void DrawHarbor()
    {
        Raylib.DrawCube(new Vector3(-60, 18, 470), 28, 36, 22, Palette.C(240, 244, 248));
        Raylib.DrawCube(new Vector3(-20, 14, 490), 18, 28, 18, Palette.SparkDark);
        Raylib.DrawCube(new Vector3(50, 22, 480), 24, 44, 20, Palette.C(248, 248, 252));
        Raylib.DrawCube(new Vector3(90, 10, 460), 40, 8, 12, Palette.C(180, 80, 60));
        Raylib.DrawCylinder(new Vector3(140, 0, 430), 3, 3, 28, 8, Palette.C(80, 70, 60));
    }

    static void DrawJungle()
    {
        for (var i = -4; i <= 4; i++)
        {
            var x = i * 38;
            Raylib.DrawCylinder(new Vector3(x, 0, 430), 3, 4, 22, 6, Palette.C(92, 54, 28));
            Raylib.DrawSphere(new Vector3(x, 26, 430), 14, Palette.C(24, 96, 40));
        }
        Raylib.DrawCube(new Vector3(0, 8, 500), 220, 16, 40, Palette.C(28, 86, 38));
    }

    static void DrawKeep()
    {
        Raylib.DrawCube(new Vector3(0, 28, 470), 90, 56, 40, Palette.C(36, 22, 28));
        Raylib.DrawCube(new Vector3(-48, 40, 470), 16, 80, 16, Palette.C(28, 16, 20));
        Raylib.DrawCube(new Vector3(48, 40, 470), 16, 80, 16, Palette.C(28, 16, 20));
        Raylib.DrawCube(new Vector3(0, 8, 430), 120, 8, 18, Palette.C(70, 24, 18));
        Raylib.DrawSphere(new Vector3(0, 18, 250), 6, Palette.Fade(Palette.EmberFire, 90));
    }

    static void DrawBackstop()
    {
        Raylib.DrawCube(new Vector3(0, 8, -18), 36, 16, 1.2f, Palette.C(190, 198, 206));
        Raylib.DrawCube(new Vector3(-18, 8, -10), 1.2f, 16, 16, Palette.C(190, 198, 206));
        Raylib.DrawCube(new Vector3(18, 8, -10), 1.2f, 16, 16, Palette.C(190, 198, 206));
    }
}
