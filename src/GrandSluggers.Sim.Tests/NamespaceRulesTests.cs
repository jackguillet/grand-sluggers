using System.Reflection;
using System.Reflection.Emit;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The Sim's one-way rule: <c>GrandSluggers.Sim</c> is the core (rules, match, live play, the narrator and the camera and
/// stamp tables the sim emits); <c>GrandSluggers.Sim.Front</c> is front-of-house (menus, the book, HUD layout);
/// <c>GrandSluggers.Sim.Tooling</c> is the agent and still tooling. Front and Tooling may read the core; the core reads
/// neither. Checked on the compiled assembly — every signature and every instruction of every method body — so a call
/// that compiles is still caught.
/// </summary>
public sealed class NamespaceRulesTests
{
    const string Core = "GrandSluggers.Sim";
    static readonly string[] Outer = [Core + ".Front", Core + ".Tooling"];

    static readonly Assembly Sim = typeof(Match).Assembly;
    static readonly Dictionary<short, OpCode> OpCodesByValue = typeof(OpCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Select(f => (OpCode)f.GetValue(null)!)
        .ToDictionary(o => o.Value);

    static bool IsOuter(Type? type)
    {
        for (; type is not null; type = type.IsNested ? type.DeclaringType : null)
        {
            if (type.IsGenericType && !type.IsGenericTypeDefinition && type.GetGenericArguments().Any(IsOuter)) return true;
            if (type.HasElementType && IsOuter(type.GetElementType())) return true;
            if (type.Namespace is { } ns && Outer.Any(o => ns == o || ns.StartsWith(o + ".", StringComparison.Ordinal))) return true;
        }
        return false;
    }

    static bool IsCore(Type type) => type.Namespace == Core;

    [Fact]
    public void EverySimTypeIsInCoreFrontOrTooling()
    {
        var stray = Sim.GetTypes()
            .Where(t => t.Namespace is { } ns && ns.StartsWith(Core, StringComparison.Ordinal) && ns != Core && !Outer.Contains(ns))
            .Select(t => t.FullName).ToList();
        Assert.Empty(stray);
    }

    [Fact]
    public void FrontAndToolingHoldTypes()
    {
        foreach (var ns in Outer)
            Assert.Contains(Sim.GetTypes(), t => t.Namespace == ns);
    }

    [Fact]
    public void TheCoreSignaturesNameNoFrontOrToolingType()
    {
        var hits = new List<string>();
        foreach (var type in Sim.GetTypes().Where(IsCore))
        {
            const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            if (IsOuter(type.BaseType) || type.GetInterfaces().Any(IsOuter)) hits.Add($"{type.FullName}: base");
            hits.AddRange(type.GetFields(all).Where(f => IsOuter(f.FieldType)).Select(f => $"{type.FullName}.{f.Name}"));
            hits.AddRange(type.GetProperties(all).Where(p => IsOuter(p.PropertyType)).Select(p => $"{type.FullName}.{p.Name}"));
            foreach (var m in type.GetMethods(all).Cast<MethodBase>().Concat(type.GetConstructors(all)))
                if ((m is MethodInfo mi && IsOuter(mi.ReturnType)) || m.GetParameters().Any(p => IsOuter(p.ParameterType)))
                    hits.Add($"{type.FullName}.{m.Name}");
        }
        Assert.Empty(hits);
    }

    [Fact]
    public void TheCoreMethodBodiesCallNoFrontOrToolingCode()
    {
        var hits = new List<string>();
        const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        foreach (var type in Sim.GetTypes().Where(IsCore))
            foreach (var method in type.GetMethods(all).Cast<MethodBase>().Concat(type.GetConstructors(all)))
                foreach (var member in Referenced(method))
                    if (IsOuter(member as Type ?? member.DeclaringType)
                        || member is MethodInfo mi && (IsOuter(mi.ReturnType) || mi.GetGenericArguments().Any(IsOuter))
                        || member is FieldInfo fi && IsOuter(fi.FieldType))
                        hits.Add($"{type.FullName}.{method.Name} -> {(member as Type ?? member.DeclaringType)?.FullName}.{member.Name}");
        Assert.Empty(hits.Distinct());
    }

    [Fact]
    public void TheBodyScanSeesACallIntoFront()
    {
        // The falsifier: a Front method that calls the core is seen by the same walk that guards the core.
        var front = Sim.GetTypes().Where(t => t.Namespace == Core + ".Front")
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .SelectMany(Referenced);
        Assert.Contains(front, m => (m as Type ?? m.DeclaringType)?.Namespace == Core);
    }

    /// <summary>Every member a method body names: the calls, the fields, the types it builds or casts to.</summary>
    static IEnumerable<MemberInfo> Referenced(MethodBase method)
    {
        byte[]? il;
        try { il = method.GetMethodBody()?.GetILAsByteArray(); }
        catch (Exception e) when (e is InvalidOperationException or NotSupportedException) { il = null; }
        if (il is null) yield break;
        var typeArgs = method.DeclaringType is { IsGenericType: true } dt ? dt.GetGenericArguments() : null;
        var methodArgs = method.IsGenericMethod ? method.GetGenericArguments() : null;
        for (var i = 0; i < il.Length;)
        {
            short value = il[i] == 0xFE ? (short)(0xFE00 | il[i + 1]) : il[i];
            i += il[i] == 0xFE ? 2 : 1;
            var op = OpCodesByValue[value];
            switch (op.OperandType)
            {
                case OperandType.InlineMethod or OperandType.InlineField or OperandType.InlineType or OperandType.InlineTok:
                    MemberInfo? member = null;
                    try { member = method.Module.ResolveMember(BitConverter.ToInt32(il, i), typeArgs, methodArgs); }
                    catch (ArgumentException) { }
                    if (member is not null) yield return member;
                    i += 4;
                    break;
                case OperandType.InlineSwitch:
                    i += 4 + 4 * BitConverter.ToInt32(il, i);
                    break;
                case OperandType.InlineI8 or OperandType.InlineR:
                    i += 8;
                    break;
                case OperandType.InlineNone:
                    break;
                case OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar:
                    i += 1;
                    break;
                case OperandType.InlineVar:
                    i += 2;
                    break;
                default:
                    i += 4;
                    break;
            }
        }
    }
}
