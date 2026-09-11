using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    public sealed class HeroActor : MonoBehaviour
    {
        public enum Pose
        {
            Idle, Walk, Run,
            ChargePitch, ThrowPitch, Throw,
            ChargeSwing, Swing, CheckSwing, Bunt,
            Catch, Dive, Jump, StealLead, Slide, Cheer, Miss,
            Field, Spin, Charm, Clamber, Crouch, Scoop
        }

        Transform _root, _torso, _head, _cap, _lArm, _rArm, _lFore, _rFore, _sourceBatSocket, _batSocket, _bat, _batModel, _glove, _lThigh, _rThigh, _lShin, _rShin, _ring;
        Pose _pose = Pose.Idle;
        float _charge;
        float _chargeRing;
        string _pitchType = "fastball";
        float _t;
        float _poseT;
        float _lift;
        bool _grow;
        bool _lit;
        bool _hint;
        bool _you;
        bool _heldBat;
        bool _heldGlove;
        bool _batsLeft;
        bool _throwsLeft;
        bool _captain;
        bool _packageBody;
        PackageTransformBind[] _packageBindPose = System.Array.Empty<PackageTransformBind>();
        bool _packageSampledLastTick;
        bool _sharedSwingMissingReported;
        string _id = "";
        string _body = "rio";
        string _batVisual = "";
        string _gloveVisual = "";
        Vector3 _look = Vector3.forward;
        Vector3 _baseScale = Vector3.one;
        Vector3 _torsoRest = new Vector3(0, 2.28f, 0);
        float _hunchDeg;
        SharedRig.BoneBind _bind;
        Vector3 _ground;
        bool _hasGround;
        float _speed;
        bool _snap;
        MoveBones.Sample _lastMotion, _loadedMotion;
        bool _blendLoad;

        readonly struct PackageTransformBind
        {
            public readonly Transform Transform;
            public readonly Vector3 Position;
            public readonly Quaternion Rotation;
            public readonly Vector3 Scale;

            public PackageTransformBind(Transform transform)
            {
                Transform = transform;
                Position = transform.localPosition;
                Rotation = transform.localRotation;
                Scale = transform.localScale;
            }
        }

        public string Id => _id;
        public Pose Current => _pose;
        public float PoseTime => _poseT;
        public Transform CatchHand => _glove != null ? _glove : (_throwsLeft ? _rFore : _lFore);
        public Transform ThrowHand => _throwsLeft ? _lFore : _rFore;

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
                skinned.BakeMesh(baked);
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
            var sampleT = _pose == Pose.ChargeSwing
                ? SwingPresentation.LoadSampleAt(_charge)
                : System.Math.Clamp(_poseT, 0f, (float)MoveBones.SwingDur);
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

        public void Bind(Character who)
        {
            if (who.Id == _id && _root != null) return;
            _id = who.Id;
            if (_root != null) Destroy(_root.gameObject);
            DropRing();
            Build(who);
        }

        void OnDisable()
        {
            if (_ring != null) _ring.gameObject.SetActive(false);
        }

        void OnDestroy() => DropRing();

        void DropRing()
        {
            if (_ring == null) return;
            Destroy(_ring.gameObject);
            _ring = null;
        }

        public void SetPose(Pose pose, float charge = 0f, string pitchType = null)
        {
            if (_packageBody && pose != _pose)
                RestorePackageBindPose(preserveRootPresentation: false);
            else if (_packageBody && _packageSampledLastTick)
                RestorePackageBindPose(preserveRootPresentation: true);
            if (pose != _pose)
            {
                _blendLoad = (pose == Pose.Swing && _pose == Pose.ChargeSwing)
                    || (pose == Pose.ThrowPitch && _pose == Pose.ChargePitch);
                _loadedMotion = _lastMotion;
                _poseT = 0f;
            }
            _pose = pose;
            _charge = Mathf.Clamp01(charge);
            if (!string.IsNullOrEmpty(pitchType)) _pitchType = pitchType;
        }

        public void SetHeld(bool bat, bool glove)
        {
            _heldBat = bat;
            _heldGlove = glove;
        }

        public void SetGear(BatItem bat, GloveItem glove)
        {
            if (_root == null) return;
            var batVis = GearMesh.HittingBatVisual();
            var gloveVis = GearMesh.GloveVisual(glove);
            if (batVis != _batVisual) BuildBat(batVis);
            if (gloveVis != _gloveVisual) BuildGlove(gloveVis);
        }

        public void SetGrow(bool on) => _grow = on;

        public void SetHighlight(bool on) => _lit = on;

        public void SetHint(bool on) => _hint = on;

        public void SetYou(bool on) => _you = on;

        public void SetChargeRing(float charge01) => _chargeRing = Mathf.Clamp01(charge01);

        /// <summary>Still-gate: apply the pose in one step. Live play keeps the slerp.</summary>
        public void SnapTick(float poseT)
        {
            _poseT = poseT;
            _t = poseT;
            _snap = true;
            Tick(0f);
            _snap = false;
        }

        /// <summary>Sample a timed verb on the same clock as its ball event.</summary>
        public void SampleMotion(float poseTime)
        {
            _poseT = Mathf.Max(0, poseTime);
            Tick(0f);
        }

        public void Place(Vector3 pos, Vector3 look)
        {
            var ground = new Vector3(pos.x, 0f, pos.z);
            if (_hasGround && Time.deltaTime > 1e-5f)
            {
                var inst = Vector3.Distance(ground, _ground) / Time.deltaTime;
                _speed = Mathf.Lerp(_speed, inst, 0.4f);
            }
            _hasGround = true;
            _ground = ground;

            float lift;
            if (_pose == Pose.Jump || _pose == Pose.Clamber)
                lift = (float)MoveBones.JumpLift(_poseT);
            else if (_pose == Pose.Dive) lift = 0.2f;
            else if (_pose == Pose.Slide) lift = 0.15f;
            else if (_pose == Pose.Crouch || _pose == Pose.StealLead || _pose == Pose.Bunt) lift = -0.35f;
            else if (_pose == Pose.Cheer) lift = 0.25f;
            else if (TryAuthoredLift(out var authoredLift)) lift = authoredLift;
            else if (_pose == Pose.Run) lift = (float)MoveBones.Evaluate(MoveBones.Verb.Run, _t, _poseT).Lift;
            else lift = 0f;
            _lift = _pose is Pose.Jump or Pose.Clamber ? lift : Mathf.Lerp(_lift, lift, 0.28f);
            transform.position = pos + Vector3.up * _lift;
            _look = look.sqrMagnitude < 0.01f ? Vector3.forward : look.normalized;
            if (_pose != Pose.Spin)
            {
                var yaw = Quaternion.LookRotation(new Vector3(_look.x, 0f, _look.z));
                transform.rotation = Quaternion.Slerp(transform.rotation, yaw, 0.35f);
            }
        }

        /// <summary>
        /// Still-gate: stand on the dirt looking at a world point. Live Place()
        /// slerps and treats the vector as a direction — that put Fenn's shell in the lens.
        /// </summary>
        public void PlaceStill(Vector3 pos, Vector3 lookAt)
        {
            transform.position = pos;
            var d = lookAt - pos;
            d.y = 0f;
            if (d.sqrMagnitude < 0.01f) d = Vector3.back;
            transform.rotation = Quaternion.LookRotation(d);
            _look = d.normalized;
            _ground = new Vector3(pos.x, 0f, pos.z);
            _hasGround = true;
            _lift = 0f;
        }

        public void Tick(float dt)
        {
            _t += dt;
            _poseT += dt;
            if (_root != null)
            {
                var g = (_grow ? 1.45f : 1f) * (_lit ? 1.18f : _hint ? 1.12f : 1f);
                var bounce = 0f;
                if (_pose == Pose.Idle || _pose == Pose.Field)
                    bounce = 0.07f * Mathf.Abs(Mathf.Sin(_t * 5.4f));
                var squash = Vector3.one;
                if (_pose == Pose.Swing && _poseT >= 0.12f && _poseT < 0.32f)
                    squash = new Vector3(1.14f, 0.84f, 1.14f);
                else if (_pose == Pose.Dive)
                    squash = new Vector3(1.22f, 0.76f, 1.18f);
                else if (_pose == Pose.Jump || _pose == Pose.Clamber)
                    squash = new Vector3(0.86f, 1.18f, 0.86f);
                var want = Vector3.Scale(_baseScale * g, squash);
                _root.localScale = _snap
                    ? want
                    : Vector3.Lerp(_root.localScale, want, 0.22f);
                _root.localPosition = new Vector3(0f, bounce, 0f);
            }
            PlaceRing();
            Animate();
        }

        void PlaceRing()
        {
            if (_ring == null) return;
            var on = SetTells.YouRingOn(_you, _chargeRing);
            _ring.gameObject.SetActive(on);
            if (!on) return;
            if (_ring.parent != null)
                _ring.SetParent(null, true);
            var s = (float)SetTells.LiveRingScale(_you, _chargeRing);
            var pulse = s + 0.08f * Mathf.Sin(_t * 7f);
            var feetX = _hasGround ? _ground.x : transform.position.x;
            var feetZ = _hasGround ? _ground.z : transform.position.z;
            var at = SetTells.RingAt(feetX, feetZ, transform.position.y, _lift);
            _ring.SetPositionAndRotation(
                new Vector3((float)at.X, (float)at.Y, (float)at.Z),
                Quaternion.identity);
            _ring.localScale = Vector3.one * pulse;
        }

        void Build(Character who)
        {
            _body = Silhouette.BodyType(who);
            _captain = who.Captain;
            _batsLeft = who.Bats == Hand.L;
            _throwsLeft = who.Throws == Hand.L;
            var extras = ArtBinder.Art != null
                ? ArtBinder.SkinOf(who).Extras
                : System.Array.Empty<string>();
            var chain = SharedRig.Spawn(transform, who, extras);
            _root = chain.Root;
            _baseScale = chain.BaseScale;
            _torso = chain.Torso;
            _head = chain.Head;
            _cap = chain.Cap;
            _lArm = chain.LUpper;
            _lFore = chain.LFore;
            _rArm = chain.RUpper;
            _rFore = chain.RFore;
            _sourceBatSocket = _batSocket = chain.Bat;
            _lThigh = chain.LThigh;
            _lShin = chain.LShin;
            _rThigh = chain.RThigh;
            _rShin = chain.RShin;
            _ring = chain.Ring;
            _torsoRest = chain.TorsoRest;
            _hunchDeg = chain.HunchDeg;
            _bind = chain.Bind;
            _packageBody = ArtBinder.Art != null
                && CharacterPackage.IsUnique(ArtBinder.SkinOf(who).Bind)
                && ArtBinder.LoadBodyPrefab(who.Id) != null;
            if (_packageBody)
            {
                var controller = ArtBinder.LoadPackageController(who.Id);
                var animator = _root.GetComponentInChildren<Animator>(true);
                if (animator == null && controller != null) animator = _root.gameObject.AddComponent<Animator>();
                if (animator != null)
                {
                    animator.runtimeAnimatorController = controller;
                    animator.enabled = false;
                }
                CapturePackageBindPose();
            }
            else if (_batsLeft)
            {
                _batSocket = MirroredBatSocket(_batSocket, _lFore);
                _bind.Bat = _batSocket.localRotation;
            }
            BuildBat(GearMesh.HittingBatVisual());
            BuildGlove("glove-brown");
            if (_bat != null) _bat.gameObject.SetActive(false);
        }

        void BuildBat(string visual)
        {
            _batVisual = visual ?? "bat-wood";
            if (_bat != null) Destroy(_bat.gameObject);
            _batModel = null;
            if (_batSocket == null) return;
            var go = new GameObject("Bat");
            go.transform.SetParent(_batSocket, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            var model = new GameObject("Model").transform;
            model.SetParent(go.transform, false);
            // The DCC socket owns the swing. This one bind conversion maps the
            // bat-wood +Y mesh axis into that socket and rotates its authored
            // grip offset by the same quaternion so the handle stays at origin.
            var axis = new Vector3(
                (float)SwingPresentation.ModelBarrelAxisAtSocket.X,
                (float)SwingPresentation.ModelBarrelAxisAtSocket.Y,
                (float)SwingPresentation.ModelBarrelAxisAtSocket.Z);
            model.localRotation = Quaternion.FromToRotation(Vector3.up, axis);
            var modelGrip = new Vector3(
                (float)SwingPresentation.ModelGrip.X,
                (float)SwingPresentation.ModelGrip.Y,
                (float)SwingPresentation.ModelGrip.Z);
            model.localScale = Vector3.one * Silhouette.BatScale;
            model.localPosition = -(model.localRotation
                * (modelGrip * Silhouette.BatScale));
            FillBat(model, _batVisual);
            _batModel = model;
            _bat = go.transform;
        }

        static Transform MirroredBatSocket(Transform source, Transform hittingForearm)
        {
            if (hittingForearm == null) return source;
            var socket = new GameObject("bat-left").transform;
            socket.SetParent(hittingForearm, false);
            if (source == null)
            {
                socket.localPosition = new Vector3(0, -0.68f, 0.12f);
                socket.localRotation = Quaternion.identity;
                return socket;
            }
            socket.localPosition = new Vector3(-source.localPosition.x, source.localPosition.y, source.localPosition.z);
            var e = source.localRotation.eulerAngles;
            socket.localRotation = Quaternion.Euler(e.x, -e.y, -e.z);
            socket.localScale = source.localScale;
            return socket;
        }

        static bool TryDropToy(string id, Transform parent)
        {
            if (parent == null || string.IsNullOrWhiteSpace(id)) return false;
            var src = ArtBinder.LoadExtraMesh(id);
            if (src == null) return false;
            var go = UnityEngine.Object.Instantiate(src, parent);
            go.name = id;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            foreach (var anim in go.GetComponentsInChildren<Animator>(true))
                anim.enabled = false;
            return go.GetComponentInChildren<MeshRenderer>(true) != null;
        }

        void FillBat(Transform root, string visual)
        {
            if (TryDropToy(visual, root))
                return;
            switch (visual)
            {
                case "bat-spark":
                {
                    var wood = Look.Lit(new Color(0.78f, 0.18f, 0.16f), smooth: 0.22f);
                    var gold = Look.Lit(Colors.Gold, smooth: 0.45f);
                    Look.Prim(PrimitiveType.Cylinder, "Handle", root, new Vector3(0, -0.55f, 0), new Vector3(0.16f, 0.7f, 0.16f), wood);
                    Look.Prim(PrimitiveType.Cylinder, "Barrel", root, new Vector3(0, 0.55f, 0), new Vector3(0.28f, 1.05f, 0.28f), wood);
                    Look.Prim(PrimitiveType.Cube, "Spark", root, new Vector3(0, 0.7f, 0.16f), new Vector3(0.08f, 0.7f, 0.08f), gold);
                    break;
                }
                case "bat-wand":
                {
                    var ice = Look.Lit(new Color(0.72f, 0.88f, 1f), smooth: 0.55f);
                    var pink = Look.Lit(new Color(0.95f, 0.55f, 0.78f), smooth: 0.4f);
                    Look.Prim(PrimitiveType.Cylinder, "Shaft", root, Vector3.zero, new Vector3(0.1f, 1.85f, 0.1f), ice);
                    Look.Prim(PrimitiveType.Sphere, "Tip", root, new Vector3(0, 1.7f, 0), Vector3.one * 0.28f, pink);
                    break;
                }
                case "bat-short":
                {
                    var green = Look.Lit(new Color(0.18f, 0.72f, 0.38f), smooth: 0.25f);
                    var rainbow = Look.Lit(new Color(1f, 0.35f, 0.62f), smooth: 0.4f);
                    Look.Prim(PrimitiveType.Cylinder, "Stick", root, Vector3.zero, new Vector3(0.26f, 1.05f, 0.26f), green);
                    Look.Prim(PrimitiveType.Cylinder, "Ring", root, new Vector3(0, 0.55f, 0), new Vector3(0.34f, 0.1f, 0.34f), rainbow);
                    break;
                }
                case "bat-brick":
                {
                    var gold = Look.Lit(new Color(0.9f, 0.72f, 0.16f), smooth: 0.18f);
                    var grip = Look.Lit(new Color(0.35f, 0.22f, 0.08f), smooth: 0.1f);
                    Look.Prim(PrimitiveType.Cylinder, "Handle", root, new Vector3(0, -0.7f, 0), new Vector3(0.2f, 0.55f, 0.2f), grip);
                    Look.Prim(PrimitiveType.Cube, "Brick", root, new Vector3(0, 0.45f, 0), new Vector3(0.7f, 1.35f, 0.42f), gold);
                    break;
                }
                case "bat-barrel":
                {
                    var wood = Look.Lit(new Color(0.5f, 0.3f, 0.12f), smooth: 0.12f);
                    var hoop = Look.Lit(new Color(0.72f, 0.62f, 0.28f), smooth: 0.3f);
                    Look.Prim(PrimitiveType.Cylinder, "Handle", root, new Vector3(0, -0.65f, 0), new Vector3(0.18f, 0.55f, 0.18f), wood);
                    Look.Prim(PrimitiveType.Cylinder, "Cask", root, new Vector3(0, 0.5f, 0), new Vector3(0.55f, 0.95f, 0.55f), wood);
                    Look.Prim(PrimitiveType.Cylinder, "HoopA", root, new Vector3(0, 0.15f, 0), new Vector3(0.6f, 0.07f, 0.6f), hoop);
                    Look.Prim(PrimitiveType.Cylinder, "HoopB", root, new Vector3(0, 0.85f, 0), new Vector3(0.6f, 0.07f, 0.6f), hoop);
                    break;
                }
                case "bat-furnace":
                {
                    var iron = Look.Lit(new Color(0.12f, 0.08f, 0.1f), smooth: 0.08f);
                    var fire = Look.Lit(Colors.EmberFire, smooth: 0.35f);
                    Look.Prim(PrimitiveType.Cylinder, "Handle", root, new Vector3(0, -0.55f, 0), new Vector3(0.2f, 0.65f, 0.2f), iron);
                    Look.Prim(PrimitiveType.Cylinder, "Club", root, new Vector3(0, 0.55f, 0), new Vector3(0.38f, 1.05f, 0.38f), iron);
                    for (var i = 0; i < 4; i++)
                    {
                        var a = i * 90f * Mathf.Deg2Rad;
                        Look.Prim(PrimitiveType.Cube, "Spike" + i, root,
                            new Vector3(Mathf.Cos(a) * 0.28f, 0.7f, Mathf.Sin(a) * 0.28f),
                            new Vector3(0.14f, 0.45f, 0.14f), fire);
                    }
                    break;
                }
                case "bat-gold":
                {
                    var gold = Look.Lit(Colors.Gold, smooth: 0.5f);
                    Look.Prim(PrimitiveType.Cylinder, "Bat", root, Vector3.zero, new Vector3(0.22f, 1.7f, 0.22f), gold);
                    break;
                }
                case "bat-staff":
                {
                    var wood = Look.Lit(new Color(0.42f, 0.26f, 0.12f), smooth: 0.12f);
                    var cream = Look.Lit(Colors.FenCream, smooth: 0.28f);
                    Look.Prim(PrimitiveType.Cylinder, "Shaft", root, Vector3.zero, new Vector3(0.14f, 1.85f, 0.14f), wood);
                    Look.Prim(PrimitiveType.Sphere, "Knob", root, new Vector3(0, 1.72f, 0), Vector3.one * 0.28f, cream);
                    Look.Prim(PrimitiveType.Cylinder, "Ferrule", root, new Vector3(0, -1.65f, 0), new Vector3(0.16f, 0.12f, 0.16f), cream);
                    break;
                }
                default:
                {
                    if (TryDropToy("bat-wood", root)) return;
                    var wood = Look.Lit(new Color(0.45f, 0.28f, 0.12f), smooth: 0.15f);
                    Look.Prim(PrimitiveType.Cylinder, "Bat", root, Vector3.zero, new Vector3(0.22f, 1.7f, 0.22f), wood);
                    break;
                }
            }
        }

        void BuildGlove(string visual)
        {
            _gloveVisual = visual ?? "glove-brown";
            if (_glove != null) Destroy(_glove.gameObject);
            var hand = _throwsLeft ? _rFore : _lFore;
            if (hand == null) return;
            var go = new GameObject("Glove");
            go.transform.SetParent(hand, false);
            go.transform.localPosition = new Vector3(0, -0.72f, 0.12f);
            go.transform.localRotation = Quaternion.Euler(20, 0, 0);
            var gold = _gloveVisual == "glove-gold";
            go.transform.localScale = Vector3.one * ToyMesh.GloveRootScale(gold);
            if (TryDropToy(_gloveVisual, go.transform) || TryDropToy("glove-brown", go.transform))
            {
                _glove = go.transform;
                return;
            }
            var leather = gold
                ? Look.Lit(new Color(0.92f, 0.74f, 0.18f), smooth: 0.32f)
                : Look.Lit(new Color(0.42f, 0.24f, 0.12f), smooth: 0.12f);
            Look.Prim(PrimitiveType.Sphere, "Palm", go.transform, Vector3.zero, Vector3.one * 0.7f, leather);
            Look.Prim(PrimitiveType.Cube, "Web", go.transform, new Vector3(0, 0.05f, 0.28f), new Vector3(0.55f, 0.08f, 0.42f), leather);
            Look.Prim(PrimitiveType.Capsule, "Thumb", go.transform, new Vector3(-0.32f, 0.05f, 0.1f), new Vector3(0.22f, 0.32f, 0.22f), leather);
            Look.Prim(PrimitiveType.Capsule, "Fingers", go.transform, new Vector3(0.12f, 0.22f, 0.08f), new Vector3(0.42f, 0.28f, 0.22f), leather);
            _glove = go.transform;
        }

        void Animate()
        {
            var pose = Locomotion(_pose);
            var bob = 0.04f * Mathf.Sin(_t * 2.4f);
            if (pose == Pose.Cheer) bob = Mathf.Abs(Mathf.Sin(_t * 6f)) * 0.12f;
            if (_torso != null) _torso.localPosition = _torsoRest + new Vector3(0, bob, 0);

            var batOn = _heldBat;
            var gloveOn = _heldGlove;
            var motionVerb = ToVerb(pose);
            if (_packageBody && pose == Pose.Idle) motionVerb = MoveBones.Verb.Idle;
            if (motionVerb is MoveBones.Verb verb)
            {
                batOn = pose is Pose.ChargeSwing or Pose.Swing or Pose.CheckSwing or Pose.Bunt or Pose.Miss;
                gloveOn = pose is Pose.ChargePitch or Pose.ThrowPitch or Pose.Throw
                    or Pose.Jump or Pose.Clamber or Pose.Scoop;
                if (pose is Pose.ChargeSwing or Pose.Swing or Pose.Slide) gloveOn = false;
                MoveBones.Sample sample;
                var clipId = ClipId(verb);
                // Unique packages never play Rio's authored takes or clip FBX.
                // CharacterMotion flexes THIS rest pose in bone-local space.
                MoveBones.Sample authored = default;
                var authoredPose = false;
                if (!_packageBody && pose is not (Pose.ChargeSwing or Pose.Swing))
                {
                    if (pose is Pose.ChargePitch or Pose.ChargeSwing)
                    {
                        authoredPose = TryAuthoredAt(pose == Pose.ChargePitch ? "pitch" : "swing", 0, out authored);
                        if (authoredPose)
                            authored = MoveBones.Mix(MoveBones.Evaluate(verb, _t, 0, 0, _pitchType), authored, _charge);
                    }
                    else authoredPose = TryAuthored(clipId, out authored);
                }
                if (_packageBody)
                    sample = CharacterMotion.Evaluate(verb, _t, _poseT, _charge);
                else if (authoredPose)
                    sample = authored;
                else
                    sample = MoveBones.Evaluate(verb, _t, _poseT, _charge, _pitchType);
                if (_blendLoad)
                    sample = AtBatMotion.FromLoad(_loadedMotion, sample, _poseT,
                        pose == Pose.Swing ? MoveBones.SwingContact : MoveBones.PitchRelease);
                _lastMotion = sample;
                if ((pose is Pose.ChargeSwing or Pose.Swing) && _batsLeft)
                    sample = MoveBones.MirrorSwing(sample);
                if ((pose is Pose.ChargePitch or Pose.ThrowPitch or Pose.Throw) && _throwsLeft)
                    sample = MoveBones.MirrorArms(sample);
                var clipT = _poseT;
                if (!string.IsNullOrEmpty(clipId) && ArtBinder.Art != null
                    && ArtBinder.Art.TryClip(clipId, out var slot) && slot.Loop)
                    clipT = _t;
                // Authored eulers are offsets on the bind pose (Q(e)*bind).
                // SampleAnimation replaces bind and laid the scoop mesh on its side.
                var playedPackage = _packageBody && ArtBinder.Art != null
                    && ArtBinder.Art.TryPackageVerb(_id, verb, out var packageVerb)
                    && TrySamplePackage(packageVerb);
                var sharedSwingTake = !_packageBody && pose is (Pose.ChargeSwing or Pose.Swing);
                if (sharedSwingTake && _bat != null)
                    _bat.localRotation = Quaternion.identity;
                var playedDrop = sharedSwingTake
                    ? TrySampleDrop("swing", pose == Pose.Swing
                        ? (float)AtBatMotion.SwingClipTime(_poseT, _charge)
                        : (float)SwingPresentation.LoadSampleAt(_charge))
                    : !_packageBody && !authoredPose && TrySampleDrop(clipId, clipT);
                if (sharedSwingTake && !playedDrop && !_sharedSwingMissingReported)
                {
                    Debug.LogError("Shared swing FBX is missing; using the visible MoveBones placeholder for " + _id);
                    _sharedSwingMissingReported = true;
                }
                else if (sharedSwingTake && playedDrop)
                    _sharedSwingMissingReported = false;
                if (!playedPackage && !playedDrop)
                {
                    if (_packageBody && _packageSampledLastTick)
                        RestorePackageBindPose(preserveRootPresentation: true);
                    var boneSnap = pose is Pose.Swing or Pose.ThrowPitch or Pose.Throw or Pose.Jump or Pose.Scoop or Pose.Slide;
                    var timed = pose is Pose.Swing or Pose.ThrowPitch;
                    if (_packageBody)
                        ApplyPackage(sample,
                            _snap || timed ? 1f : boneSnap ? 0.55f : 0.32f,
                            _snap || timed ? 1f : boneSnap ? 0.48f : 0.34f);
                    else
                        Apply(sample,
                            _snap || timed ? 1f : boneSnap ? 0.55f : 0.32f,
                            _snap || timed ? 1f : boneSnap ? 0.48f : 0.34f);
                    _packageSampledLastTick = false;
                }
                else if ((pose is Pose.ChargeSwing or Pose.Swing) && _batsLeft)
                    MirrorBoundSwing();
                else if ((pose is Pose.ThrowPitch or Pose.Throw) && _throwsLeft)
                    MirrorBoundArms();
                // A ready Generic package owns its authored socket. Shared-rig
                // swings own it through swing.json / MoveBones. Aim only the
                // explicit package-local fallback when that package has no take.
                if (_packageBody && !playedPackage && pose is (Pose.ChargeSwing or Pose.Swing))
                    AimBatSocket(pose == Pose.Swing ? _poseT : 0f);
                if (_bat != null)
                {
                    _bat.gameObject.SetActive(batOn);
                }
                if (_glove != null) _glove.gameObject.SetActive(gloveOn && !batOn);
                return;
            }

            var lArm = Quaternion.Euler(12, 0, 18);
            var rArm = Quaternion.Euler(12, 0, -18);
            var lLeg = Quaternion.identity;
            var rLeg = Quaternion.identity;
            var batRot = Quaternion.Euler(0, 0, 20);
            var torsoRot = Quaternion.identity;
            var headRot = Quaternion.identity;

            switch (pose)
            {
                case Pose.Walk:
                {
                    var s = Mathf.Sin(_t * 8f);
                    lArm = Quaternion.Euler(28f * s, 0, 16);
                    rArm = Quaternion.Euler(-28f * s, 0, -16);
                    lLeg = Quaternion.Euler(22f * s, 0, 0);
                    rLeg = Quaternion.Euler(-22f * s, 0, 0);
                    break;
                }
                case Pose.Run:
                {
                    var stride = Mathf.Repeat(_t * 2.55f, 1f);
                    float k;
                    if (stride < 0.25f)
                    {
                        k = stride / 0.25f;
                        lLeg = Quaternion.Euler(LerpK(8, 58, k), 0, 0);
                        rLeg = Quaternion.Euler(LerpK(-8, -42, k), 0, 0);
                        lArm = Quaternion.Euler(LerpK(-20, -62, k), 6, 8);
                        rArm = Quaternion.Euler(LerpK(20, 58, k), -6, -8);
                        torsoRot = Quaternion.Euler(14, LerpK(0, 8, k), 0);
                    }
                    else if (stride < 0.5f)
                    {
                        k = (stride - 0.25f) / 0.25f;
                        lLeg = Quaternion.Euler(LerpK(58, -8, k), 0, 0);
                        rLeg = Quaternion.Euler(LerpK(-42, 8, k), 0, 0);
                        lArm = Quaternion.Euler(LerpK(-62, 20, k), 6, 8);
                        rArm = Quaternion.Euler(LerpK(58, -20, k), -6, -8);
                        torsoRot = Quaternion.Euler(16, LerpK(8, 0, k), 0);
                    }
                    else if (stride < 0.75f)
                    {
                        k = (stride - 0.5f) / 0.25f;
                        lLeg = Quaternion.Euler(LerpK(-8, -42, k), 0, 0);
                        rLeg = Quaternion.Euler(LerpK(8, 58, k), 0, 0);
                        lArm = Quaternion.Euler(LerpK(20, 58, k), 6, 8);
                        rArm = Quaternion.Euler(LerpK(-20, -62, k), -6, -8);
                        torsoRot = Quaternion.Euler(14, LerpK(0, -8, k), 0);
                    }
                    else
                    {
                        k = (stride - 0.75f) / 0.25f;
                        lLeg = Quaternion.Euler(LerpK(-42, 8, k), 0, 0);
                        rLeg = Quaternion.Euler(LerpK(58, -8, k), 0, 0);
                        lArm = Quaternion.Euler(LerpK(58, -20, k), 6, 8);
                        rArm = Quaternion.Euler(LerpK(-62, 20, k), -6, -8);
                        torsoRot = Quaternion.Euler(16, LerpK(-8, 0, k), 0);
                    }
                    break;
                }
                case Pose.ChargePitch:
                    lArm = Quaternion.Euler(12, 0, 28);
                    rArm = PitchSlot(-35 - 70 * _charge, 18, -38);
                    lLeg = Quaternion.Euler(8 + 25 * _charge, 0, 0);
                    rLeg = Quaternion.Euler(-10 * _charge, 0, 0);
                    torsoRot = Quaternion.Euler(-8 - 18 * _charge, 12, 0);
                    gloveOn = true;
                    break;
                case Pose.ThrowPitch:
                {
                    gloveOn = true;
                    var wind = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_poseT / 0.10f));
                    var stride = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((_poseT - 0.10f) / 0.12f));
                    var rel = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((_poseT - 0.22f) / 0.12f));
                    var fol = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((_poseT - 0.34f) / 0.16f));
                    lArm = Quaternion.Slerp(
                        Quaternion.Euler(12, 0, 28),
                        Quaternion.Euler(40, 0, 22),
                        Mathf.Clamp01(wind * 0.35f + stride * 0.35f + rel * 0.3f));
                    var back = PitchSlot(-105, 18, -38);
                    var slot = PitchSlot(10, 5, -20);
                    var outArm = PitchSlot(78, -10, -10);
                    var wrap = PitchSlot(105, -22, 12);
                    if (_poseT < 0.10f) rArm = Quaternion.Slerp(back, back, wind);
                    else if (_poseT < 0.22f) rArm = Quaternion.Slerp(back, slot, stride);
                    else if (_poseT < 0.34f) rArm = Quaternion.Slerp(slot, outArm, rel);
                    else rArm = Quaternion.Slerp(outArm, wrap, fol);
                    lLeg = Quaternion.Euler(18 + 22 * stride, 0, 0);
                    rLeg = Quaternion.Euler(-8 + 28 * rel, 0, 0);
                    torsoRot = Quaternion.Euler(-18 + 36 * rel, 12 - 30 * rel, 0);
                    break;
                }
                case Pose.Throw:
                {
                    gloveOn = true;
                    var k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_poseT / 0.22f));
                    var f = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((_poseT - 0.22f) / 0.18f));
                    lArm = Quaternion.Euler(32 - 8 * k, 0, 22);
                    var dirt = Quaternion.Euler(55, 8, -12);
                    var whip = Quaternion.Euler(70, -8, -18);
                    var follow = Quaternion.Euler(110, -18, 8);
                    rArm = Quaternion.Slerp(dirt, Quaternion.Slerp(whip, follow, f), k);
                    torsoRot = Quaternion.Euler(18 - 6 * k, -18 * k, 0);
                    lLeg = Quaternion.Euler(28 - 12 * k, 0, 0);
                    rLeg = Quaternion.Euler(12, 0, 0);
                    break;
                }
                case Pose.ChargeSwing:
                    batOn = true;
                    gloveOn = false;
                    lArm = Quaternion.Euler(-10, 20, 30);
                    rArm = Quaternion.Euler(-35 - 50 * _charge, -40, -55);
                    batRot = Quaternion.Euler(75 + 28 * _charge, 0, 10);
                    break;
                case Pose.Swing:
                {
                    batOn = true;
                    gloveOn = false;
                    var loadL = Quaternion.Euler(-10, 20, 30);
                    var loadR = Quaternion.Euler(-85, -40, -55);
                    var loadBat = Quaternion.Euler(103, 0, 10);
                    var loadT = Quaternion.Euler(0, -8, 0);
                    var slotL = Quaternion.Euler(10, -10, 18);
                    var slotR = Quaternion.Euler(-10, 10, -20);
                    var slotBat = Quaternion.Euler(20, 40, 8);
                    var slotT = Quaternion.Euler(8, 20, -4);
                    var cutL = Quaternion.Euler(28, -48, 6);
                    var cutR = Quaternion.Euler(22, 72, 28);
                    var cutBat = Quaternion.Euler(-55, 110, 12);
                    var cutT = Quaternion.Euler(10, 55, -8);
                    var wrapL = Quaternion.Euler(40, -70, -10);
                    var wrapR = Quaternion.Euler(8, 95, 40);
                    var wrapBat = Quaternion.Euler(-70, 155, 20);
                    var wrapT = Quaternion.Euler(6, 78, -12);
                    if (_poseT < 0.12f)
                    {
                        var k = Mathf.SmoothStep(0f, 1f, _poseT / 0.12f);
                        lArm = Quaternion.Slerp(loadL, slotL, k);
                        rArm = Quaternion.Slerp(loadR, slotR, k);
                        batRot = Quaternion.Slerp(loadBat, slotBat, k);
                        torsoRot = Quaternion.Slerp(loadT, slotT, k);
                    }
                    else if (_poseT < 0.28f)
                    {
                        var k = Mathf.SmoothStep(0f, 1f, (_poseT - 0.12f) / 0.16f);
                        lArm = Quaternion.Slerp(slotL, cutL, k);
                        rArm = Quaternion.Slerp(slotR, cutR, k);
                        batRot = Quaternion.Slerp(slotBat, cutBat, k);
                        torsoRot = Quaternion.Slerp(slotT, cutT, k);
                    }
                    else
                    {
                        var k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((_poseT - 0.28f) / 0.22f));
                        lArm = Quaternion.Slerp(cutL, wrapL, k);
                        rArm = Quaternion.Slerp(cutR, wrapR, k);
                        batRot = Quaternion.Slerp(cutBat, wrapBat, k);
                        torsoRot = Quaternion.Slerp(cutT, wrapT, k);
                        headRot = Quaternion.Euler(8 * k, 18 * k, 0);
                    }
                    lLeg = Quaternion.Euler(12, 0, 0);
                    rLeg = Quaternion.Euler(-18, 20, 0);
                    break;
                }
                case Pose.CheckSwing:
                    batOn = true;
                    gloveOn = false;
                    lArm = Quaternion.Euler(-6, 12, 22);
                    rArm = Quaternion.Euler(-18, -22, -28);
                    batRot = Quaternion.Euler(42, 18, 8);
                    torsoRot = Quaternion.Euler(6, 12, 0);
                    break;
                case Pose.Bunt:
                    batOn = true;
                    gloveOn = false;
                    lArm = Quaternion.Euler(8, 35, 8);
                    rArm = Quaternion.Euler(8, -35, -8);
                    batRot = Quaternion.Euler(90, 0, 90);
                    torsoRot = Quaternion.Euler(12, 0, 0);
                    lLeg = Quaternion.Euler(28, 6, 8);
                    rLeg = Quaternion.Euler(18, -6, -8);
                    break;
                case Pose.Catch:
                {
                    var k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_poseT / 0.16f));
                    lArm = Quaternion.Slerp(Quaternion.Euler(25, 0, 25), Quaternion.Euler(-58, 0, 18), k);
                    rArm = Quaternion.Slerp(Quaternion.Euler(25, 0, -25), Quaternion.Euler(-58, 0, -18), k);
                    gloveOn = true;
                    break;
                }
                case Pose.Scoop:
                {
                    gloveOn = true;
                    var drop = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_poseT / 0.12f));
                    var pick = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((_poseT - 0.12f) / 0.16f));
                    var up = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((_poseT - 0.28f) / 0.16f));
                    lArm = Quaternion.Slerp(
                        Quaternion.Euler(12, 0, 18),
                        Quaternion.Slerp(Quaternion.Euler(62, 8, 10), Quaternion.Euler(28, 0, 16), up),
                        Mathf.Max(drop, pick));
                    rArm = Quaternion.Slerp(
                        Quaternion.Euler(12, 0, -18),
                        Quaternion.Slerp(Quaternion.Euler(70, -12, -8), Quaternion.Euler(22, 0, -16), up),
                        Mathf.Max(drop, pick));
                    torsoRot = Quaternion.Euler(LerpK(8, 42, drop) - 22 * up, 0, 0);
                    lLeg = Quaternion.Euler(38 + 18 * drop - 12 * up, 8, 10);
                    rLeg = Quaternion.Euler(28 + 22 * drop - 10 * up, -6, -8);
                    break;
                }
                case Pose.Field:
                    lArm = Quaternion.Euler(25, 0, 25);
                    rArm = Quaternion.Euler(25, 0, -25);
                    gloveOn = true;
                    break;
                case Pose.Dive:
                    lArm = Quaternion.Euler(-80, 0, 10);
                    rArm = Quaternion.Euler(-80, 0, -10);
                    torsoRot = Quaternion.Euler(70, 0, 0);
                    gloveOn = true;
                    break;
                case Pose.Jump:
                case Pose.Clamber:
                    lArm = Quaternion.Euler(-70, 0, 15);
                    rArm = Quaternion.Euler(-70, 0, -15);
                    lLeg = Quaternion.Euler(40, 8, 0);
                    rLeg = Quaternion.Euler(40, -8, 0);
                    gloveOn = true;
                    break;
                case Pose.Spin:
                    lArm = Quaternion.Euler(10, 0, 70);
                    rArm = Quaternion.Euler(10, 0, -70);
                    transform.rotation *= Quaternion.Euler(0, 720f * Time.deltaTime, 0);
                    gloveOn = true;
                    break;
                case Pose.Charm:
                    lArm = Quaternion.Euler(0, 0, 40);
                    rArm = Quaternion.Euler(0, 0, -40);
                    break;
                case Pose.Slide:
                {
                    gloveOn = false;
                    var tuck = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_poseT / 0.18f));
                    var pop = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((_poseT - 0.18f) / 0.22f));
                    lArm = Quaternion.Slerp(Quaternion.Euler(10, 8, 18), Quaternion.Euler(-28, 12, 22), tuck);
                    rArm = Quaternion.Slerp(Quaternion.Euler(10, -8, -18), Quaternion.Euler(-48, -18, -12), tuck);
                    lLeg = Quaternion.Slerp(Quaternion.Euler(20, 0, 0), Quaternion.Slerp(Quaternion.Euler(88, 10, 12), Quaternion.Euler(42, 6, 6), pop), tuck);
                    rLeg = Quaternion.Slerp(Quaternion.Euler(12, 0, 0), Quaternion.Slerp(Quaternion.Euler(102, -8, -10), Quaternion.Euler(28, -4, -6), pop), tuck);
                    torsoRot = Quaternion.Euler(LerpK(12, 62, tuck) - 28 * pop, 0, 0);
                    break;
                }
                case Pose.StealLead:
                    lArm = Quaternion.Euler(28, 8, 22);
                    rArm = Quaternion.Euler(12, -12, -28);
                    lLeg = Quaternion.Euler(42, 10, 10);
                    rLeg = Quaternion.Euler(18, -6, -8);
                    torsoRot = Quaternion.Euler(22, 12, 0);
                    gloveOn = false;
                    break;
                case Pose.Crouch:
                    lArm = Quaternion.Euler(40, 0, 18);
                    rArm = Quaternion.Euler(40, 0, -18);
                    lLeg = Quaternion.Euler(68, 8, 12);
                    rLeg = Quaternion.Euler(68, -8, -12);
                    torsoRot = Quaternion.Euler(32, 0, 0);
                    gloveOn = true;
                    break;
                case Pose.Cheer:
                    lArm = Quaternion.Euler(-110, 0, 18);
                    rArm = Quaternion.Euler(-110, 0, -18);
                    batOn = false;
                    gloveOn = false;
                    break;
                case Pose.Miss:
                    batOn = true;
                    gloveOn = false;
                    lArm = Quaternion.Euler(8, -12, 12);
                    rArm = Quaternion.Euler(22, 28, 18);
                    batRot = Quaternion.Euler(-12, 55, 8);
                    torsoRot = Quaternion.Euler(14, -8, 0);
                    headRot = Quaternion.Euler(22, -16, 0);
                    break;
            }

            var batting = pose is Pose.ChargeSwing or Pose.Swing or Pose.CheckSwing or Pose.Bunt or Pose.Miss;
            var pitching = pose is Pose.ChargePitch or Pose.ThrowPitch or Pose.Throw;
            if (batting && _batsLeft) MirrorArms(ref lArm, ref rArm);
            if (pitching && _throwsLeft) MirrorArms(ref lArm, ref rArm);

            var poseSnap = pose is Pose.Swing or Pose.ThrowPitch or Pose.Throw or Pose.Scoop or Pose.Slide;
            var kArm = _snap ? 1f : poseSnap ? 0.55f : 0.2f;
            var kLeg = _snap ? 1f : poseSnap ? 0.45f : 0.25f;
            if (_hunchDeg != 0f)
                torsoRot = Quaternion.Euler(_hunchDeg, 0, 0) * torsoRot;
            if (_torso != null)
                _torso.localRotation = Quaternion.Slerp(_torso.localRotation, torsoRot * _bind.Torso, kArm);
            if (_head != null)
                _head.localRotation = Quaternion.Slerp(_head.localRotation, headRot * _bind.Head, kArm);
            if (_lArm != null) _lArm.localRotation = Quaternion.Slerp(_lArm.localRotation, lArm * _bind.LUpper, kArm);
            if (_rArm != null) _rArm.localRotation = Quaternion.Slerp(_rArm.localRotation, rArm * _bind.RUpper, kArm);
            if (_lThigh != null) _lThigh.localRotation = Quaternion.Slerp(_lThigh.localRotation, lLeg * _bind.LThigh, kLeg);
            if (_rThigh != null) _rThigh.localRotation = Quaternion.Slerp(_rThigh.localRotation, rLeg * _bind.RThigh, kLeg);
            if (_lShin != null) _lShin.localRotation = Quaternion.Slerp(_lShin.localRotation, Quaternion.Euler(12, 0, 0) * _bind.LShin, kLeg);
            if (_rShin != null) _rShin.localRotation = Quaternion.Slerp(_rShin.localRotation, Quaternion.Euler(12, 0, 0) * _bind.RShin, kLeg);
            if (_bat != null)
            {
                _bat.gameObject.SetActive(batOn);
                _bat.localRotation = batRot;
            }
            if (_glove != null)
                _glove.gameObject.SetActive(gloveOn && !batOn);
        }

        static MoveBones.Verb? ToVerb(Pose pose) => pose switch
        {
            Pose.Walk => MoveBones.Verb.Walk,
            Pose.Run => MoveBones.Verb.Run,
            Pose.Jump or Pose.Clamber => MoveBones.Verb.Jump,
            Pose.ChargePitch => MoveBones.Verb.ChargePitch,
            Pose.ThrowPitch => MoveBones.Verb.Pitch,
            Pose.ChargeSwing => MoveBones.Verb.ChargeSwing,
            Pose.Swing => MoveBones.Verb.Swing,
            Pose.Throw => MoveBones.Verb.Throw,
            Pose.Scoop => MoveBones.Verb.Scoop,
            Pose.Slide => MoveBones.Verb.Slide,
            _ => null
        };

        bool TryAuthoredLift(out float lift)
        {
            lift = 0f;
            if (ToVerb(_pose) is not MoveBones.Verb verb) return false;
            if (!TryAuthored(ClipId(verb), out var sample)) return false;
            lift = (float)sample.Lift;
            return true;
        }

        bool TryAuthored(string clipId, out MoveBones.Sample sample)
        {
            sample = default;
            if (clipId == null || ArtBinder.Art == null) return false;
            var t = _t;
            if (ArtBinder.Art.TryClip(clipId, out var clip) && !clip.Loop)
                t = _poseT;
            return ArtBinder.Art.TryAuthored(clipId, t, out sample);
        }

        bool TryAuthoredAt(string clipId, float t, out MoveBones.Sample sample)
        {
            sample = default;
            if (string.IsNullOrEmpty(clipId) || ArtBinder.Art == null) return false;
            return ArtBinder.Art.TryAuthored(clipId, t, out sample);
        }

        static string ClipId(MoveBones.Verb verb) => verb switch
        {
            MoveBones.Verb.Idle => "idle",
            MoveBones.Verb.Walk => "walk",
            MoveBones.Verb.Run => "run",
            MoveBones.Verb.Jump => "jump",
            MoveBones.Verb.Swing => "swing",
            MoveBones.Verb.Pitch => "pitch",
            MoveBones.Verb.Scoop => "scoop",
            MoveBones.Verb.Slide => "slide",
            MoveBones.Verb.Throw => "throw",
            _ => null
        };

        /// <summary>
        /// Unique drop: offset the authored rest pose in bone-local space
        /// (bind * Q). MoveBones Q * bind assumes Rio's axes and inverts a turtle.
        /// </summary>
        void ApplyPackage(MoveBones.Sample s, float kArm, float kLeg)
        {
            EaseLocal(ref _torso, s.Torso, kArm, _bind.Torso);
            EaseLocal(ref _head, s.Head, kArm, _bind.Head);
            EaseLocal(ref _lArm, s.LUpper, kArm, _bind.LUpper);
            EaseLocal(ref _lFore, s.LFore, kArm, _bind.LFore);
            EaseLocal(ref _rArm, s.RUpper, kArm, _bind.RUpper);
            EaseLocal(ref _rFore, s.RFore, kArm, _bind.RFore);
            EaseLocal(ref _lThigh, s.LThigh, kLeg, _bind.LThigh);
            EaseLocal(ref _lShin, s.LShin, kLeg, _bind.LShin);
            EaseLocal(ref _rThigh, s.RThigh, kLeg, _bind.RThigh);
            EaseLocal(ref _rShin, s.RShin, kLeg, _bind.RShin);
            EaseLocal(ref _batSocket, s.Bat, kArm, _bind.Bat);
        }

        static void EaseLocal(ref Transform tf, MoveBones.Euler e, float k, Quaternion bind)
        {
            if (tf == null) return;
            tf.localRotation = Quaternion.Slerp(tf.localRotation, bind * Q(e), k);
        }

        void Apply(MoveBones.Sample s, float kArm, float kLeg)
        {
            var torso = s.Torso;
            if (_hunchDeg != 0f)
                torso = new MoveBones.Euler(torso.X + _hunchDeg, torso.Y, torso.Z);
            // Scoop eulers were authored for identity rest. Q(e)*FBX-bind rolls the
            // mesh onto its back. Other verbs keep the imported bind.
            var scoop = _pose == Pose.Scoop;
            var id = Quaternion.identity;
            Ease(ref _torso, torso, kArm, scoop ? id : _bind.Torso);
            Ease(ref _head, s.Head, kArm, scoop ? id : _bind.Head);
            Ease(ref _lArm, s.LUpper, kArm, scoop ? id : _bind.LUpper);
            Ease(ref _lFore, s.LFore, kArm, scoop ? id : _bind.LFore);
            Ease(ref _rArm, s.RUpper, kArm, scoop ? id : _bind.RUpper);
            Ease(ref _rFore, s.RFore, kArm, scoop ? id : _bind.RFore);
            Ease(ref _lThigh, s.LThigh, kLeg, scoop ? id : _bind.LThigh);
            Ease(ref _lShin, s.LShin, kLeg, scoop ? id : _bind.LShin);
            Ease(ref _rThigh, s.RThigh, kLeg, scoop ? id : _bind.RThigh);
            Ease(ref _rShin, s.RShin, kLeg, scoop ? id : _bind.RShin);
            Ease(ref _batSocket, s.Bat, kArm, scoop ? id : _bind.Bat);
        }

        void AimBatSocket(float poseT)
        {
            if (_batSocket == null || _root == null) return;
            var key = SwingPresentation.At(poseT, _batsLeft ? Hand.L : Hand.R);
            var local = new Vector3(
                (float)key.BarrelDirection.X,
                (float)key.BarrelDirection.Y,
                (float)key.BarrelDirection.Z);
            var world = _root.TransformVector(local).normalized;
            if (world.sqrMagnitude < 0.01f) return;
            if (_bat != null) _bat.localRotation = Quaternion.identity;
            var modelAxis = new Vector3(
                (float)SwingPresentation.ModelBarrelAxisAtSocket.X,
                (float)SwingPresentation.ModelBarrelAxisAtSocket.Y,
                (float)SwingPresentation.ModelBarrelAxisAtSocket.Z);
            _batSocket.rotation = Quaternion.FromToRotation(modelAxis, world);
        }

        static void Ease(ref Transform tf, MoveBones.Euler e, float k, Quaternion bind)
        {
            if (tf == null) return;
            tf.localRotation = Quaternion.Slerp(tf.localRotation, Q(e) * bind, k);
        }

        bool TrySamplePackage(PackageVerbSlot verb)
        {
            var clip = ArtBinder.LoadPackageClip(_id, verb);
            if (clip == null || _root == null) return false;
            RestorePackageBindPose(preserveRootPresentation: true);
            var t = verb.Clock.Equals(CharacterPackage.WorldClock, System.StringComparison.OrdinalIgnoreCase)
                ? _t
                : verb.Clock.Equals(CharacterPackage.ChargeClock, System.StringComparison.OrdinalIgnoreCase)
                    ? _charge * clip.length
                    : _poseT;
            if (t < 0f) t = 0f;
            if (clip.length > 1e-4f)
            {
                if (verb.Loop) t %= clip.length;
                else t = Mathf.Min(t, clip.length);
            }
            var scale = _root.localScale;
            var pos = _root.localPosition;
            clip.SampleAnimation(_root.gameObject, t);
            _root.localScale = scale;
            _root.localPosition = pos;
            _packageSampledLastTick = true;
            return true;
        }

        void CapturePackageBindPose()
        {
            if (_root == null)
            {
                _packageBindPose = System.Array.Empty<PackageTransformBind>();
                return;
            }
            var transforms = _root.GetComponentsInChildren<Transform>(true);
            _packageBindPose = new PackageTransformBind[transforms.Length];
            for (var i = 0; i < transforms.Length; i++)
                _packageBindPose[i] = new PackageTransformBind(transforms[i]);
            _packageSampledLastTick = false;
        }

        void RestorePackageBindPose(bool preserveRootPresentation)
        {
            if (_packageBindPose == null || _packageBindPose.Length == 0) return;
            var rootPosition = _root != null ? _root.localPosition : Vector3.zero;
            var rootScale = _root != null ? _root.localScale : Vector3.one;
            for (var i = 0; i < _packageBindPose.Length; i++)
            {
                var bind = _packageBindPose[i];
                if (bind.Transform == null) continue;
                bind.Transform.localPosition = bind.Position;
                bind.Transform.localRotation = bind.Rotation;
                bind.Transform.localScale = bind.Scale;
            }
            if (preserveRootPresentation && _root != null)
            {
                _root.localPosition = rootPosition;
                _root.localScale = rootScale;
            }
            // The HeroActor transform owns world facing. This local rotation is the
            // imported DCC basis and must return to bind before another verb.
            _packageSampledLastTick = false;
        }

        bool TrySampleDrop(string clipId, float t)
        {
            var clip = ArtBinder.LoadClip(clipId);
            if (clip == null || _root == null) return false;
            if (t < 0f) t = 0f;
            if (clip.length > 1e-4f) t = Mathf.Min(t, clip.length);
            var scale = _root.localScale;
            var pos = _root.localPosition;
            clip.SampleAnimation(_root.gameObject, t);
            _root.localScale = scale;
            _root.localPosition = pos;
            if (_torso != null) _torso.localPosition = _torsoRest;
            return true;
        }

        void MirrorBoundArms()
        {
            MirrorLocal(ref _lArm, ref _rArm);
            MirrorLocal(ref _lFore, ref _rFore);
        }

        void MirrorBoundSwing()
        {
            MirrorOne(_torso);
            MirrorOne(_head);
            MirrorLocal(ref _lArm, ref _rArm);
            MirrorLocal(ref _lFore, ref _rFore);
            MirrorLocal(ref _lThigh, ref _rThigh);
            MirrorLocal(ref _lShin, ref _rShin);
            if (_sourceBatSocket != null && _sourceBatSocket != _batSocket)
            {
                var p = _sourceBatSocket.localPosition;
                _batSocket.localPosition = new Vector3(-p.x, p.y, p.z);
                var e = _sourceBatSocket.localRotation.eulerAngles;
                _batSocket.localRotation = Quaternion.Euler(e.x, -e.y, -e.z);
                _batSocket.localScale = _sourceBatSocket.localScale;
            }
            else MirrorOne(_batSocket);
        }

        static void MirrorOne(Transform tf)
        {
            if (tf == null) return;
            var e = tf.localRotation.eulerAngles;
            tf.localRotation = Quaternion.Euler(e.x, -e.y, -e.z);
        }

        static void MirrorLocal(ref Transform a, ref Transform b)
        {
            if (a == null || b == null) return;
            var ea = a.localRotation.eulerAngles;
            var eb = b.localRotation.eulerAngles;
            a.localRotation = Quaternion.Euler(eb.x, -eb.y, -eb.z);
            b.localRotation = Quaternion.Euler(ea.x, -ea.y, -ea.z);
        }

        static Quaternion Q(MoveBones.Euler e) =>
            Quaternion.Euler((float)e.X, (float)e.Y, (float)e.Z);

        Pose Locomotion(Pose pose)
        {
            if (pose is not (Pose.Idle or Pose.Field or Pose.Walk or Pose.Run))
                return pose;
            if (_speed > CartoonJuice.RunFtPerSec) return Pose.Run;
            if (_speed > CartoonJuice.WalkFtPerSec) return Pose.Walk;
            if (pose is Pose.Walk or Pose.Run)
                return _heldGlove ? Pose.Field : Pose.Idle;
            return pose;
        }

        static float LerpK(float a, float b, float u) => a + (b - a) * u;

        static void MirrorArms(ref Quaternion lArm, ref Quaternion rArm)
        {
            var l = lArm.eulerAngles;
            var r = rArm.eulerAngles;
            lArm = Quaternion.Euler(r.x, -r.y, -r.z);
            rArm = Quaternion.Euler(l.x, -l.y, -l.z);
        }

        Quaternion PitchSlot(float x, float y, float z)
        {
            return _pitchType switch
            {
                "curve" => Quaternion.Euler(x - 25, y, z - 12),
                "slider" => Quaternion.Euler(x + 28, y + 18, z + 8),
                _ => Quaternion.Euler(x, y, z)
            };
        }
    }
}
