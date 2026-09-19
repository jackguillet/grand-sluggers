using System;
using System.IO;
using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// The data the window plays (#715): the shipped root beside the app, with the trial overlay a run names in
    /// <c>GRAND_SLUGGERS_TRIAL</c> — the variable <c>cli match</c> reads, so the window and the headless run play one table.
    /// Unset, this is the shipped root exactly as before. A named overlay that is not a folder stops the window rather than
    /// play the control under the trial's name (<see cref="DataRoot.NamedOverlay"/>).
    /// </summary>
    public static class DataProfile
    {
        static DataRoot _root;

        /// <summary>The shipped root: <c>Application.dataPath</c> is <c>&lt;app&gt;/Contents</c> in a player and <c>unity/Assets</c> in the editor.</summary>
        public static string ShippedRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "data"));

        /// <summary>This process's root and overlay.</summary>
        public static DataRoot Root => _root ??= DataRoot.FromEnvironment(ShippedRoot);

        /// <summary>What Call time names when a trial is on (<c>TRIAL  trials/c80</c>); null for the shipped table.</summary>
        public static string Label => Root.OverlayName is { } name ? "TRIAL  " + name : null;

        /// <summary>
        /// Before any scene object: a window playing a trial pins the process-wide table to the root it loads. <see cref="Diamond"/>
        /// and every helper without a catalog read <see cref="Rules.Default"/>, which finds its root from the binary; left alone,
        /// the bags could be the control's while the match plays the copy's rules. A shipped run sets nothing.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void PinProcessTable()
        {
            _root = null;
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(DataRoot.OverlayVariable))) return;
            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ContentCatalog.DataRootVariable))) return;
            Environment.SetEnvironmentVariable(ContentCatalog.DataRootVariable, ShippedRoot);
        }
    }
}
