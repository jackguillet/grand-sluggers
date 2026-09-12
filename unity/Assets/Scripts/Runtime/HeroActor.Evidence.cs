using GrandSluggers.Sim;
using Motion = GrandSluggers.Sim.Motion;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// Rendered-mesh measurements for the swing matrix and character stills.
    /// Reads what Unity draws, never bone-local axes. No gameplay path calls
    /// anything in this file.
    /// </summary>
    public sealed partial class HeroActor
    {
        internal bool TrySwingGeometry(
            out Vector3 leftHand, out Vector3 rightHand,
            out Vector3 grip, out Vector3 barrel,
            out Vector3 socketX, out Vector3 socketY, out Vector3 socketZ)
        {
            leftHand = rightHand = grip = barrel = socketX = socketY = socketZ = Vector3.zero;
            if (_lFore == null || _rFore == null || _batSocket == null || _batModel == null)
                return false;
            // hero-shared forearm bones are 0.70 ft head-to-palm.
            leftHand = _lFore.TransformPoint(Vector3.up * 0.70f);
            rightHand = _rFore.TransformPoint(Vector3.up * 0.70f);
            grip = _batSocket.position;
            barrel = _batModel.TransformPoint(
                new Vector3(
                    (float)SwingPresentation.ModelBarrelEnd.X,
                    (float)SwingPresentation.ModelBarrelEnd.Y,
                    (float)SwingPresentation.ModelBarrelEnd.Z));
            var reach = (float)SwingPresentation.BarrelReach;
            socketX = _batSocket.TransformPoint(Vector3.right * reach);
            socketY = _batSocket.TransformPoint(Vector3.up * reach);
            socketZ = _batSocket.TransformPoint(Vector3.forward * reach);
            return true;
        }

        internal readonly struct SwingHandEvidence
        {
            internal readonly Vector3 Center;
            internal readonly Vector3 Extents;
            internal readonly Vector3 RootCenter;
            internal readonly Vector3 RootExtents;

            internal SwingHandEvidence(
                Vector3 center, Vector3 extents, Vector3 rootCenter, Vector3 rootExtents)
            {
                Center = center;
                Extents = extents;
                RootCenter = rootCenter;
                RootExtents = rootExtents;
            }
        }

        internal readonly struct SwingBatEvidence
        {
            internal readonly Vector3 Grip;
            internal readonly Vector3 HandleEnd;
            internal readonly Vector3 BarrelStart;
            internal readonly Vector3 BarrelEnd;
            internal readonly Vector3 RootGrip;
            internal readonly Vector3 RootHandleEnd;
            internal readonly Vector3 RootBarrelStart;
            internal readonly Vector3 RootBarrelEnd;
            internal readonly float HandleRadius;
            internal readonly float BarrelRadius;
            internal readonly float RootHandleRadius;
            internal readonly float RootBarrelRadius;
            internal readonly Vector3 ExpectedDirection;

            internal SwingBatEvidence(
                Vector3 grip, Vector3 handleEnd, Vector3 barrelStart, Vector3 barrelEnd,
                Vector3 rootGrip, Vector3 rootHandleEnd,
                Vector3 rootBarrelStart, Vector3 rootBarrelEnd,
                float handleRadius, float barrelRadius,
                float rootHandleRadius, float rootBarrelRadius,
                Vector3 expectedDirection)
            {
                Grip = grip;
                HandleEnd = handleEnd;
                BarrelStart = barrelStart;
                BarrelEnd = barrelEnd;
                RootGrip = rootGrip;
                RootHandleEnd = rootHandleEnd;
                RootBarrelStart = rootBarrelStart;
                RootBarrelEnd = rootBarrelEnd;
                HandleRadius = handleRadius;
                BarrelRadius = barrelRadius;
                RootHandleRadius = rootHandleRadius;
                RootBarrelRadius = rootBarrelRadius;
                ExpectedDirection = expectedDirection;
            }
        }

        internal readonly struct BatMeshEvidence
        {
            internal readonly float MinY;
            internal readonly float MaxY;
            internal readonly float MaxRadius;
            internal readonly float GripMinY;
            internal readonly float GripMaxY;
            internal readonly bool HasGripMaterial;

            internal BatMeshEvidence(
                float minY, float maxY, float maxRadius,
                float gripMinY, float gripMaxY, bool hasGripMaterial)
            {
                MinY = minY;
                MaxY = maxY;
                MaxRadius = maxRadius;
                GripMinY = gripMinY;
                GripMaxY = gripMaxY;
                HasGripMaterial = hasGripMaterial;
            }
        }

        // Measure the visible body, not bone-local axes whose bind conventions
        // differ between the shared armature and Generic packages. SharedRig
        // hides the drop rig's authored eyes and rebuilds the face it draws, so
        // skip anything switched off: a hidden landmark is not the stance.
        // Where an extras kit hides an authored landmark and draws its own, the
        // joint carrying the replacement stands in — see TryVisibleFoot.
        internal bool TryRenderedBattingStance(
            out Vector3 chestForward, out Vector3 eyeForward, out Vector3 feetLine)
        {
            chestForward = eyeForward = feetLine = Vector3.zero;
            if (_root == null) return false;
            var centers = new System.Collections.Generic.Dictionary<string, Vector3>(
                System.StringComparer.OrdinalIgnoreCase);
            foreach (var renderer in _root.GetComponentsInChildren<Renderer>(true))
            {
                var name = renderer.name;
                if (name is not ("Stripe" or "Belly" or "torsoMesh" or "headMesh"
                    or "EyeL" or "EyeR" or "lShoe" or "rShoe" or "lFoot" or "rFoot")) continue;
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                if (TryPosedBounds(renderer, out var posed)) centers[name] = posed.Center;
            }
            if (!centers.TryGetValue("torsoMesh", out var torso)
                || !centers.TryGetValue("headMesh", out var head)
                || !centers.TryGetValue("EyeL", out var eyeL)
                || !centers.TryGetValue("EyeR", out var eyeR)) return false;
            if (!centers.TryGetValue("Stripe", out var chest)
                && !centers.TryGetValue("Belly", out chest)) return false;
            if (!TryVisibleFoot(centers, "lShoe", "lFoot", _lShin, out var footL)) return false;
            if (!TryVisibleFoot(centers, "rShoe", "rFoot", _rShin, out var footR)) return false;
            chestForward = Vector3.ProjectOnPlane(chest - torso, Vector3.up).normalized;
            eyeForward = Vector3.ProjectOnPlane((eyeL + eyeR) * 0.5f - head, Vector3.up).normalized;
            // From the back foot to the lead foot (BattingStance.LeadSide): a
            // right-handed batter's left foot stands nearer the pitcher, a
            // left-handed batter's right foot does. lShoe really is the
            // batter's left -- the blockout puts it at Blender -X facing +Y and
            // the import X reflection cancels against the -Z export facing --
            // so "right minus left" would read +Z for a right-handed batter
            // only when the right foot is forward, which is the hips turned out
            // of the box. Back-to-lead reads +Z from either box when the hips
            // face the plate.
            var leadFoot = _batsLeft ? footR : footL;
            var backFoot = _batsLeft ? footL : footR;
            feetLine = Vector3.ProjectOnPlane(leadFoot - backFoot, Vector3.up).normalized;
            return chestForward.sqrMagnitude > 0.9f && eyeForward.sqrMagnitude > 0.9f
                && feetLine.sqrMagnitude > 0.9f;
        }

        /// <summary>
        /// The foot a captain actually shows. An extras kit can replace the
        /// authored shoe with a dropped piece and hide the original — rio's
        /// sneakers do — so when the named mesh is not drawn, measure whatever
        /// the shin carries instead of reading a switched-off landmark.
        /// </summary>
        bool TryVisibleFoot(
            System.Collections.Generic.IDictionary<string, Vector3> centers,
            string authored, string alternate, Transform shin, out Vector3 center)
        {
            if (centers.TryGetValue(authored, out center)) return true;
            if (centers.TryGetValue(alternate, out center)) return true;
            return TryPlantedFootUnder(shin, out center);
        }

        /// <summary>
        /// Where the replacement foot is planted: the joint, not the centroid of
        /// the pieces hanging off it. A dropped extra carries its own local
        /// offset — rio's sneakers sit 0.22 forward of the shin — and the two
        /// shins hold different world rotations in a stance, so that offset turns
        /// into a lateral error on the line between the two feet. Measured off
        /// the drawn sneakers rio reads feetAlongPitch 0.877 against a 0.966
        /// threshold; off the joint it reads 0.996, beside vale's 1.000 on the
        /// same shared clip. The replacement is still what proves the foot is
        /// drawn — it just does not get to move where the batter stands.
        /// </summary>
        bool TryPlantedFootUnder(Transform bone, out Vector3 center)
        {
            center = Vector3.zero;
            if (bone == null) return false;
            foreach (var renderer in bone.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                if (!TryPosedBounds(renderer, out _)) continue;
                center = bone.position;
                return true;
            }
            return false;
        }

        internal bool TryRenderedSwingHands(
            out SwingHandEvidence left, out SwingHandEvidence right)
        {
            left = right = default;
            if (_root == null) return false;
            var foundLeft = false;
            var foundRight = false;
            foreach (var renderer in _root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.name.Equals("lHand", System.StringComparison.OrdinalIgnoreCase))
                {
                    foundLeft = TryPosedBounds(renderer, out left);
                }
                else if (renderer.name.Equals("rHand", System.StringComparison.OrdinalIgnoreCase))
                {
                    foundRight = TryPosedBounds(renderer, out right);
                }
            }
            return foundLeft && foundRight;
        }

        bool TryPosedBounds(Renderer renderer, out SwingHandEvidence evidence)
        {
            evidence = default;
            Bounds local;
            if (renderer is SkinnedMeshRenderer skinned)
            {
                var baked = new Mesh { name = "swing-hand-measure" };
                // Unity's scale-compensating overload returns the original
                // mesh size. Apply renderer.transform exactly once below;
                // default BakeMesh bakes ancestor scale into this snapshot.
                skinned.BakeMesh(baked, true);
                if (baked.vertexCount == 0)
                {
                    UnityEngine.Object.Destroy(baked);
                    return false;
                }
                baked.RecalculateBounds();
                local = baked.bounds;
                UnityEngine.Object.Destroy(baked);
            }
            else
            {
                var filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null) return false;
                local = filter.sharedMesh.bounds;
            }

            var center = renderer.transform.TransformPoint(local.center);
            var rootCenter = _root.InverseTransformPoint(center);
            var extents = Vector3.zero;
            var rootExtents = Vector3.zero;
            for (var ix = -1; ix <= 1; ix += 2)
            for (var iy = -1; iy <= 1; iy += 2)
            for (var iz = -1; iz <= 1; iz += 2)
            {
                var corner = renderer.transform.TransformPoint(
                    local.center + Vector3.Scale(local.extents, new Vector3(ix, iy, iz)));
                var delta = corner - center;
                extents.x = Mathf.Max(extents.x, Mathf.Abs(delta.x));
                extents.y = Mathf.Max(extents.y, Mathf.Abs(delta.y));
                extents.z = Mathf.Max(extents.z, Mathf.Abs(delta.z));
                var rootDelta = _root.InverseTransformPoint(corner) - rootCenter;
                rootExtents.x = Mathf.Max(rootExtents.x, Mathf.Abs(rootDelta.x));
                rootExtents.y = Mathf.Max(rootExtents.y, Mathf.Abs(rootDelta.y));
                rootExtents.z = Mathf.Max(rootExtents.z, Mathf.Abs(rootDelta.z));
            }
            evidence = new SwingHandEvidence(center, extents, rootCenter, rootExtents);
            return true;
        }

        internal bool TrySwingBatEvidence(out SwingBatEvidence evidence)
        {
            evidence = default;
            if (_batModel == null || _root == null) return false;
            var grip = SwingPresentation.ModelGrip;
            var modelGrip = _batModel.TransformPoint(new Vector3(
                (float)grip.X, (float)grip.Y, (float)grip.Z));
            var handleEnd = SwingPresentation.ModelHandleEnd;
            var modelHandleEnd = _batModel.TransformPoint(new Vector3(
                (float)handleEnd.X, (float)handleEnd.Y, (float)handleEnd.Z));
            var barrelStart = SwingPresentation.ModelBarrelStart;
            var modelBarrelStart = _batModel.TransformPoint(new Vector3(
                (float)barrelStart.X, (float)barrelStart.Y, (float)barrelStart.Z));
            var barrelEnd = SwingPresentation.ModelBarrelEnd;
            var modelBarrelEnd = _batModel.TransformPoint(new Vector3(
                (float)barrelEnd.X, (float)barrelEnd.Y, (float)barrelEnd.Z));
            var handleRadius = ModelRadiusInWorld((float)SwingPresentation.ModelHandleRadius);
            var barrelRadius = ModelRadiusInWorld((float)SwingPresentation.ModelBarrelRadius);
            var rootHandleRadius = ModelRadiusInRoot((float)SwingPresentation.ModelHandleRadius);
            var rootBarrelRadius = ModelRadiusInRoot((float)SwingPresentation.ModelBarrelRadius);
            var sampleT = _verb == Motion.Verb.ChargeSwing
                ? SwingPresentation.LoadSampleAt(_charge)
                : System.Math.Clamp(_poseT, 0f, (float)Motion.SwingDur);
            var key = SwingPresentation.At(sampleT, _batsLeft ? Hand.L : Hand.R);
            var local = new Vector3(
                (float)key.BarrelDirection.X,
                (float)key.BarrelDirection.Y,
                (float)key.BarrelDirection.Z);
            var expectedDirection = _root.TransformVector(local).normalized;
            evidence = new SwingBatEvidence(
                modelGrip, modelHandleEnd, modelBarrelStart, modelBarrelEnd,
                _root.InverseTransformPoint(modelGrip),
                _root.InverseTransformPoint(modelHandleEnd),
                _root.InverseTransformPoint(modelBarrelStart),
                _root.InverseTransformPoint(modelBarrelEnd),
                handleRadius, barrelRadius, rootHandleRadius, rootBarrelRadius,
                expectedDirection);
            return expectedDirection.sqrMagnitude > 0.99f;
        }

        float ModelRadiusInWorld(float radius) => Mathf.Max(
            _batModel.TransformVector(Vector3.right * radius).magnitude,
            _batModel.TransformVector(Vector3.forward * radius).magnitude);

        float ModelRadiusInRoot(float radius) => Mathf.Max(
            _root.InverseTransformVector(
                _batModel.TransformVector(Vector3.right * radius)).magnitude,
            _root.InverseTransformVector(
                _batModel.TransformVector(Vector3.forward * radius)).magnitude);

        internal bool TryBatMeshEvidence(out BatMeshEvidence evidence)
        {
            evidence = default;
            if (_batModel == null) return false;
            var minY = float.PositiveInfinity;
            var maxY = float.NegativeInfinity;
            var maxRadius = 0f;
            var gripMinY = float.PositiveInfinity;
            var gripMaxY = float.NegativeInfinity;
            var hasGrip = false;
            var found = false;
            foreach (var filter in _batModel.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = filter.sharedMesh;
                if (mesh == null || mesh.vertexCount == 0) continue;
                if (!mesh.isReadable) return false;
                found = true;
                var intoModel = _batModel.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                var vertices = mesh.vertices;
                foreach (var vertex in vertices)
                {
                    var p = intoModel.MultiplyPoint3x4(vertex);
                    minY = Mathf.Min(minY, p.y);
                    maxY = Mathf.Max(maxY, p.y);
                    maxRadius = Mathf.Max(maxRadius, Mathf.Sqrt(p.x * p.x + p.z * p.z));
                }

                var renderer = filter.GetComponent<MeshRenderer>();
                var materials = renderer != null ? renderer.sharedMaterials : System.Array.Empty<Material>();
                for (var sub = 0; sub < mesh.subMeshCount && sub < materials.Length; sub++)
                {
                    var material = materials[sub];
                    if (material == null || material.name.IndexOf(
                            "grip", System.StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                    hasGrip = true;
                    foreach (var index in mesh.GetIndices(sub))
                    {
                        var p = intoModel.MultiplyPoint3x4(vertices[index]);
                        gripMinY = Mathf.Min(gripMinY, p.y);
                        gripMaxY = Mathf.Max(gripMaxY, p.y);
                    }
                }
            }
            if (!found) return false;
            evidence = new BatMeshEvidence(
                minY, maxY, maxRadius, gripMinY, gripMaxY, hasGrip);
            return true;
        }

        internal bool TryBatVisual(
            out string visual, out bool visible,
            out Vector3 socketGrip, out Vector3 modelGrip)
        {
            visual = _batVisual;
            visible = _bat != null && _bat.gameObject.activeInHierarchy;
            socketGrip = modelGrip = Vector3.zero;
            if (_batSocket == null || _batModel == null) return false;
            socketGrip = _batSocket.position;
            var grip = SwingPresentation.ModelGrip;
            modelGrip = _batModel.TransformPoint(new Vector3(
                (float)grip.X, (float)grip.Y, (float)grip.Z));
            return true;
        }

    }
}
