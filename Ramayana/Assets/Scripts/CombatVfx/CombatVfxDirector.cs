// Turns the fight's events into combat VFX. It sits beside CombatController and attaches to the fight it starts
// (CombatBegan, a plain C# event on the same object), as CombatImpactDirector does. The model names a hit's source
// only after the damage (CardPlayed and ArrowFired after their effects, RoundEnded after the burns), so every Damaged
// is recorded in a CombatHitBuffer, tagged when its source is announced, and played in one LateTick on the same frame.
// Each announcement resolves its hit profile through the style's HitVfxLibrarySO by the kind of source it is:
// CardPlayed by the card's type, ArrowFired by the arrow, RoundEnded as round-end damage, and an enemy's
// EnemyActionStarted by that enemy, held as the fallback source until the frame the enemy turn ends.
// For every hit that takes health it plays the profile's impact burst at the fighter's chest, its slash along the
// blow (attacker to target, when there is an attacker) and its sparks out of the far side, all scaled up for heavy
// hits and killing blows. Rama's cards and arrows also bloom the profile's cast mandala at his feet. Fighter anchors
// come from each view's collider, cached when the fight attaches. Hit effects sit on the camera-facing surface of the
// fighter's bounds, where the camera's line of sight to the chest anchor enters them, so they are never buried inside
// a body of any size; the shaders then push them a little further toward the camera. Without the combat camera anchor
// they fall back to the chest anchor itself.
// Each fighter is bound to the Kavach shell presenter through its view's KavachShellTarget when the fight attaches.
// A hit Kavach absorbed is reported to it with the blow's direction (a ripple, or a shatter when Kavach reaches zero,
// which also throws the Kavach shard burst); a fully absorbed hit plays no impact, slash or sparks.
// Accents are particle prefabs played through the PrefabVfxSpawner, each placed by its own look: a hit that takes
// health plays its profile's Accent and its tier accent (heavy hits and killing blows), and where an enemy falls the
// Death of that enemy's own profile plays; a Kavach shatter plays the Kavach profile's Break Accent, and every gain of
// Kavach its Gain Accent, tracked here from BlockChanged so it plays whether or not the fighter has a hull. Hit-stop
// has already begun by the LateTick, so hit effects open inside the freeze. The late tick is registered only while
// hits wait, so an idle fight costs nothing. It only presents and never changes combat state.
// Special arrows are presented from the flow's arrow events. When one is loosed (ArrowLoosed) its profile's cast
// mandala and Launch Accent play on Rama, and its Missile flies from Rama to the chosen enemy (or to every living
// enemy for an untargeted arrow) over exactly the pacing's arrow flight, so it arrives as the arrow resolves. When it
// lands (ArrowLanded) the profile's Arrival plays on each of those enemies; its damage hits are tagged by the model's
// ArrowFired like any other source.
// Statuses: each time a fighter is given a status (StatusApplied with a positive amount) the status profile's Apply
// Accent for it plays on that fighter, and the StatusAuraPresenter is bound to the fight to keep status auras on the
// fighters. The director is that presenter's IFighterVfxAnchors, so auras are placed exactly as accents are.
using System.Collections.Generic;
using JourneyToLanka.Combat.Arrows;
using JourneyToLanka.Combat.Effects;
using JourneyToLanka.Combat.Flow;
using JourneyToLanka.Combat.Model;
using JourneyToLanka.Combat.Statuses;
using JourneyToLanka.CombatScene;
using JourneyToLanka.Core.Services;
using JourneyToLanka.Core.Ticking;
using UnityEngine;

namespace JourneyToLanka.CombatVfx
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CombatController))]
    [AddComponentMenu("Journey to Lanka/Combat VFX/Combat VFX Director")]
    public sealed class CombatVfxDirector : MonoBehaviour, ILateTickable, IFighterVfxAnchors
    {
        [SerializeField] private CombatVfxStyleSO _style;
        [SerializeField] private QuadVfxSpawner _quads;
        [SerializeField] private SparkBurstSpawner _sparks;
        [SerializeField] private KavachShellPresenter _kavach;
        [SerializeField] private PrefabVfxSpawner _accents;
        [SerializeField] private StatusAuraPresenter _auras;
        [SerializeField] private CombatCameraAnchorSO _cameraAnchor;

        private readonly CombatHitBuffer _hits = new CombatHitBuffer();
        private readonly List<HitRecord> _drained = new List<HitRecord>(16);
        private CombatController _controller;
        private CombatFlow _flow;
        private ITickService _ticks;
        private Collider _playerCollider;
        private Collider[] _enemyColliders = System.Array.Empty<Collider>();
        private KavachShellTarget _playerShellTarget;
        private KavachShellTarget[] _enemyShellTargets = System.Array.Empty<KavachShellTarget>();
        private int[] _lastBlocks = System.Array.Empty<int>();
        private readonly List<int> _missileTargets = new List<int>(4);
        private HitVfxProfileSO _flyingProfile;
        private bool _registered;
        private bool _releaseFallback;

        private void Awake()
        {
            _controller = GetComponent<CombatController>();
            if (_style == null || _quads == null || _sparks == null || _kavach == null || _accents == null || _auras == null)
            {
                Debug.LogError($"{name}: Combat VFX Director is missing its style, a spawner, the Kavach presenter, the accent spawner or the status aura presenter. Run Tools > Journey to Lanka > Wire Selected > Combat VFX.", this);
            }
        }

        private void OnEnable()
        {
            _controller.CombatBegan += Attach;
            if (_controller.Flow != null)
            {
                Attach(_controller.Flow);
            }
        }

        private void OnDisable()
        {
            _controller.CombatBegan -= Attach;
            Detach();
        }

        public void LateTick(float deltaTime)
        {
            _drained.Clear();
            _hits.Drain(_drained);

            if (_releaseFallback)
            {
                _releaseFallback = false;
                _hits.ClearFallback();
            }

            for (int i = 0; i < _drained.Count; i++)
            {
                PlayHit(_drained[i]);
            }

            _drained.Clear();
            SetRegistered(false);
        }

        private void Attach(CombatFlow flow)
        {
            if (flow == _flow)
            {
                return;
            }

            Detach();
            if (_ticks == null)
            {
                ServiceLocator.TryGet(out _ticks);
            }

            _flow = flow;
            CombatModel model = flow.Model;
            CacheColliders(model.Enemies.Count);
            BindKavach(model);
            CacheBlocks(model);

            flow.EnemyActionStarted += HandleEnemyActionStarted;
            flow.ArrowLoosed += HandleArrowLoosed;
            flow.ArrowLanded += HandleArrowLanded;
            flow.PhaseChanged += HandlePhaseChanged;
            model.CardPlayed += HandleCardPlayed;
            model.ArrowFired += HandleArrowFired;
            model.RoundEnded += HandleRoundEnded;
            model.Player.Damaged += HandlePlayerDamaged;
            model.Player.BlockChanged += HandleBlockChanged;
            model.Player.StatusApplied += HandleStatusApplied;
            for (int i = 0; i < model.Enemies.Count; i++)
            {
                model.Enemies[i].Fighter.Damaged += HandleEnemyDamaged;
                model.Enemies[i].Fighter.BlockChanged += HandleBlockChanged;
                model.Enemies[i].Fighter.StatusApplied += HandleStatusApplied;
            }

            if (_auras != null)
            {
                _auras.Bind(model, this);
            }
        }

        private void Detach()
        {
            if (_flow == null)
            {
                return;
            }

            CombatModel model = _flow.Model;
            _flow.EnemyActionStarted -= HandleEnemyActionStarted;
            _flow.ArrowLoosed -= HandleArrowLoosed;
            _flow.ArrowLanded -= HandleArrowLanded;
            _missileTargets.Clear();
            _flyingProfile = null;
            _flow.PhaseChanged -= HandlePhaseChanged;
            model.CardPlayed -= HandleCardPlayed;
            model.ArrowFired -= HandleArrowFired;
            model.RoundEnded -= HandleRoundEnded;
            model.Player.Damaged -= HandlePlayerDamaged;
            model.Player.BlockChanged -= HandleBlockChanged;
            model.Player.StatusApplied -= HandleStatusApplied;
            for (int i = 0; i < model.Enemies.Count; i++)
            {
                model.Enemies[i].Fighter.Damaged -= HandleEnemyDamaged;
                model.Enemies[i].Fighter.BlockChanged -= HandleBlockChanged;
                model.Enemies[i].Fighter.StatusApplied -= HandleStatusApplied;
            }

            if (_kavach != null)
            {
                _kavach.UnbindAll();
            }

            if (_auras != null)
            {
                _auras.UnbindAll();
            }

            _flow = null;
            _hits.Clear();
            _drained.Clear();
            _releaseFallback = false;
            SetRegistered(false);
        }

        private void CacheColliders(int enemyCount)
        {
            CombatantView playerView = _controller.PlayerView;
            _playerCollider = null;
            _playerShellTarget = null;
            if (playerView != null)
            {
                playerView.TryGetComponent(out _playerCollider);
                playerView.TryGetComponent(out _playerShellTarget);
            }

            if (_enemyColliders.Length != enemyCount)
            {
                _enemyColliders = new Collider[enemyCount];
                _enemyShellTargets = new KavachShellTarget[enemyCount];
            }

            for (int i = 0; i < enemyCount; i++)
            {
                CombatantView view = _controller.GetEnemyView(i);
                Collider found = null;
                KavachShellTarget shellTarget = null;
                if (view != null)
                {
                    view.TryGetComponent(out found);
                    view.TryGetComponent(out shellTarget);
                }

                _enemyColliders[i] = found;
                _enemyShellTargets[i] = shellTarget;
            }
        }

        private void BindKavach(CombatModel model)
        {
            if (_kavach == null)
            {
                return;
            }

            _kavach.Bind(model.Player, _playerShellTarget);
            for (int i = 0; i < model.Enemies.Count; i++)
            {
                _kavach.Bind(model.Enemies[i].Fighter, _enemyShellTargets[i]);
            }
        }

        // Last known Kavach per fighter, indexed by slot + 1 (Rama first), so a rise can be told from a fall.
        private void CacheBlocks(CombatModel model)
        {
            int count = model.Enemies.Count + 1;
            if (_lastBlocks.Length != count)
            {
                _lastBlocks = new int[count];
            }

            _lastBlocks[0] = model.Player.Block;
            for (int i = 0; i < model.Enemies.Count; i++)
            {
                _lastBlocks[i + 1] = model.Enemies[i].Fighter.Block;
            }
        }

        private void HandleBlockChanged(Combatant fighter)
        {
            int slot;
            if (fighter == _flow.Model.Player)
            {
                slot = HitSource.PlayerSlot;
            }
            else
            {
                slot = IndexOfEnemy(fighter);
                if (slot < 0)
                {
                    return;
                }
            }

            int index = slot + 1;
            if (index >= _lastBlocks.Length)
            {
                return;
            }

            int previous = _lastBlocks[index];
            _lastBlocks[index] = fighter.Block;
            KavachVfxProfileSO kavach = _style != null ? _style.Kavach : null;
            if (fighter.Block > previous && kavach != null)
            {
                PlayAccent(kavach.GainAccent, slot, Vector3.zero, VfxScale.One);
            }
        }

        private void HandlePlayerDamaged(Combatant player, DamageResult result)
        {
            Record(HitSource.PlayerSlot, result);
        }

        private void HandleEnemyDamaged(Combatant enemy, DamageResult result)
        {
            int slot = IndexOfEnemy(enemy);
            if (slot >= 0)
            {
                Record(slot, result);
            }
        }

        private void HandleCardPlayed(CardInstance card, ICombatant target)
        {
            HitVfxLibrarySO library = Library;
            HitVfxProfileSO profile = library != null ? library.ForCardType(card.Definition.Type) : null;
            _hits.TagUntagged(new HitSource(card.Definition.Type, HitSource.PlayerSlot, profile));
            PlayCast(profile);
        }

        private void HandleArrowFired(ArrowDefinitionSO arrow, ICombatant target)
        {
            HitVfxLibrarySO library = Library;
            HitVfxProfileSO profile = library != null ? library.ForArrow(arrow) : null;
            _hits.TagUntagged(new HitSource(arrow, HitSource.PlayerSlot, profile));
        }

        // The arrow is still in its quiver slot here; it resolves one arrow flight later.
        private void HandleArrowLoosed(int slot, int targetIndex)
        {
            _missileTargets.Clear();
            _flyingProfile = null;

            CombatModel model = _flow.Model;
            if (!model.Quiver.HasArrow(slot) || _style == null)
            {
                return;
            }

            HitVfxLibrarySO library = Library;
            HitVfxProfileSO profile = library != null ? library.ForArrow(model.Quiver[slot]) : null;
            if (profile == null)
            {
                profile = _style.DefaultHit;
            }

            PlayCast(profile);
            if (profile == null)
            {
                return;
            }

            PlayAccent(profile.LaunchAccent, HitSource.PlayerSlot, Vector3.zero, VfxScale.One);
            if (!profile.Missile.IsEnabled && !profile.Arrival.IsEnabled)
            {
                return;
            }

            _flyingProfile = profile;
            CollectMissileTargets(model, targetIndex);
            for (int i = 0; i < _missileTargets.Count; i++)
            {
                LaunchMissile(profile.Missile, _missileTargets[i]);
            }
        }

        private void HandleArrowLanded(int slot, int targetIndex)
        {
            HitVfxProfileSO profile = _flyingProfile;
            _flyingProfile = null;
            if (profile != null && TryGetAnchor(HitSource.PlayerSlot, 0.5f, out Vector3 shooter))
            {
                for (int i = 0; i < _missileTargets.Count; i++)
                {
                    int target = _missileTargets[i];
                    Vector3 direction = TryGetAnchor(target, 0.5f, out Vector3 point) ? point - shooter : Vector3.zero;
                    PlayAccent(profile.Arrival, target, direction, VfxScale.One);
                }
            }

            _missileTargets.Clear();
        }

        // The chosen enemy for a targeted arrow; every living enemy for an untargeted one, since an arrow that only
        // affects Rama carries no missile or arrival in its profile.
        private void CollectMissileTargets(CombatModel model, int targetIndex)
        {
            var enemies = model.Enemies;
            if (targetIndex >= 0)
            {
                if (targetIndex < enemies.Count && enemies[targetIndex].IsAlive)
                {
                    _missileTargets.Add(targetIndex);
                }

                return;
            }

            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i].IsAlive)
                {
                    _missileTargets.Add(i);
                }
            }
        }

        private void LaunchMissile(PrefabVfxLook missile, int targetSlot)
        {
            if (_accents == null || !missile.IsEnabled || _flow.Pacing.ArrowFlight <= 0f
                || !TryGetAnchor(HitSource.PlayerSlot, _style.MissileLaunchHeight, out Vector3 from)
                || !TryGetAnchor(targetSlot, missile.Height, out Vector3 to))
            {
                return;
            }

            if (missile.OnCameraSurface)
            {
                to = ToCameraFacingSurface(targetSlot, to);
            }

            to += missile.Offset;
            Vector3 toward = to - from;
            toward.y = 0f;
            if (toward.sqrMagnitude > 1e-6f)
            {
                from += toward.normalized * _style.MissileLaunchForward;
            }

            _accents.Launch(missile, from, to, _flow.Pacing.ArrowFlight, _style.MissileArcHeight,
                missile.ScaleFor(VfxScale.One));
        }

        private void HandleRoundEnded()
        {
            HitVfxLibrarySO library = Library;
            _hits.TagUntagged(new HitSource(null, HitSource.NoSlot, library != null ? library.RoundEnd : null));
        }

        private void HandleEnemyActionStarted(int enemyIndex)
        {
            var enemies = _flow.Model.Enemies;
            if (enemyIndex < 0 || enemyIndex >= enemies.Count)
            {
                return;
            }

            HitVfxLibrarySO library = Library;
            EnemyCombatant enemy = enemies[enemyIndex];
            HitVfxProfileSO profile = library != null ? library.ForEnemy(enemy.Definition) : null;
            _releaseFallback = false;
            _hits.SetFallback(new HitSource(enemy.Definition, enemyIndex, profile));
        }

        private void HandlePhaseChanged(CombatPhase phase)
        {
            if (phase == CombatPhase.EnemyTurn || !_hits.HasFallback)
            {
                return;
            }

            // Hits from the enemy's last action may still wait for this frame's LateTick, so the fallback is released
            // after they drain rather than now.
            _releaseFallback = true;
            SetRegistered(true);
        }

        private void Record(int targetSlot, in DamageResult result)
        {
            _hits.Record(targetSlot, result);
            SetRegistered(true);
        }

        private void PlayHit(in HitRecord record)
        {
            DamageResult result = record.Result;
            Combatant fighter = GetFighter(record.TargetSlot);
            if (_style == null || fighter == null || !TryGetAnchor(record.TargetSlot, _style.ImpactHeight, out Vector3 anchor))
            {
                return;
            }

            Vector3 direction = Vector3.zero;
            if (record.Source.HasAttacker && TryGetAnchor(record.Source.AttackerSlot, _style.ImpactHeight, out Vector3 attackerAnchor))
            {
                direction = anchor - attackerAnchor;
            }

            Vector3 point = ToCameraFacingSurface(record.TargetSlot, anchor);

            if (result.Blocked > 0 && _kavach != null && _kavach.Absorb(fighter, result.Blocked, direction))
            {
                KavachVfxProfileSO kavach = _style.Kavach;
                if (kavach != null)
                {
                    if (_sparks != null)
                    {
                        _sparks.Spawn(kavach.Shards, point, VfxScale.One, Vector3.zero);
                    }

                    PlayAccent(kavach.BreakAccent, record.TargetSlot, direction, VfxScale.One);
                }
            }

            HitVfxProfileSO profile = record.Source.Profile != null ? record.Source.Profile : _style.DefaultHit;
            if (result.HealthLost <= 0 || profile == null)
            {
                return;
            }

            VfxScale scale = _style.GetHitScale(result, fighter.MaxHealth);

            if (_quads != null)
            {
                _quads.Spawn(profile.Impact, point, scale, direction);
                if (direction.sqrMagnitude > 1e-6f)
                {
                    _quads.Spawn(profile.Slash, point, scale, direction);
                }
            }

            if (_sparks != null)
            {
                _sparks.Spawn(profile.Sparks, point, scale, direction);
            }

            bool heavy = _style.IsHeavy(result, fighter.MaxHealth);
            PlayAccent(profile.Accent, record.TargetSlot, direction, scale);
            PlayAccent(profile.TierAccent(heavy, result.Killed), record.TargetSlot, direction, scale);
            if (result.Killed)
            {
                PlayDeath(record.TargetSlot, direction);
            }
        }

        // An enemy falls with the Death accent of its own profile (the one its blows use); Rama has none.
        private void PlayDeath(int slot, Vector3 direction)
        {
            var enemies = _flow.Model.Enemies;
            if (slot < 0 || slot >= enemies.Count)
            {
                return;
            }

            HitVfxLibrarySO library = Library;
            HitVfxProfileSO profile = library != null ? library.ForEnemy(enemies[slot].Definition) : null;
            if (profile == null)
            {
                profile = _style.DefaultHit;
            }

            if (profile != null)
            {
                PlayAccent(profile.Death, slot, direction, VfxScale.One);
            }
        }

        public bool TryPlace(int slot, PrefabVfxLook look, out Vector3 point)
        {
            if (look == null || !TryGetAnchor(slot, look.Height, out point))
            {
                point = default;
                return false;
            }

            if (look.OnCameraSurface)
            {
                point = ToCameraFacingSurface(slot, point);
            }

            point += look.Offset;
            return true;
        }

        private void PlayAccent(PrefabVfxLook look, int slot, Vector3 direction, in VfxScale hitScale)
        {
            if (_accents == null || look == null || !look.IsEnabled || !TryPlace(slot, look, out Vector3 point))
            {
                return;
            }

            _accents.Spawn(look, point, direction, look.ScaleFor(hitScale));
        }

        private void HandleStatusApplied(Combatant fighter, StatusDefinitionSO status, int amount)
        {
            StatusVfxProfileSO statuses = _style != null ? _style.Statuses : null;
            StatusVfxEntry entry = amount > 0 && statuses != null ? statuses.Find(status) : null;
            if (entry == null)
            {
                return;
            }

            int slot = fighter == _flow.Model.Player ? HitSource.PlayerSlot : IndexOfEnemy(fighter);
            if (slot == HitSource.PlayerSlot && fighter != _flow.Model.Player)
            {
                return;
            }

            PlayAccent(entry.ApplyAccent, slot, Vector3.zero, VfxScale.One);
        }

        private void PlayCast(HitVfxProfileSO profile)
        {
            if (_style == null || _quads == null)
            {
                return;
            }

            if (profile == null)
            {
                profile = _style.DefaultHit;
            }

            if (profile != null && TryGetAnchor(HitSource.PlayerSlot, _style.CastHeight, out Vector3 point))
            {
                point.y += _style.CastLift;
                _quads.Spawn(profile.Cast, point, VfxScale.One, Vector3.zero);
            }
        }

        // A point on a fighter: height 0 is the bottom of its collider's bounds and 1 the top, centred on the bounds.
        // Without a collider the view's position stands in for the whole body.
        private bool TryGetAnchor(int slot, float height, out Vector3 point)
        {
            CombatantView view = slot == HitSource.PlayerSlot ? _controller.PlayerView : _controller.GetEnemyView(slot);
            if (view == null)
            {
                point = default;
                return false;
            }

            Collider body = GetCollider(slot);
            if (body == null)
            {
                point = view.transform.position;
                return true;
            }

            Bounds bounds = body.bounds;
            point = new Vector3(bounds.center.x, Mathf.LerpUnclamped(bounds.min.y, bounds.max.y, height), bounds.center.z);
            return true;
        }

        // Where the camera's line of sight to the anchor enters the fighter's collider bounds, moved Surface Offset
        // further toward the camera. The anchor itself when there is no camera, no collider, or the camera is inside
        // the bounds.
        private Vector3 ToCameraFacingSurface(int slot, Vector3 anchor)
        {
            Collider body = GetCollider(slot);
            if (body == null || _cameraAnchor == null || !_cameraAnchor.TryGetValue(out Camera viewCamera))
            {
                return anchor;
            }

            Vector3 cameraPosition = viewCamera.transform.position;
            Vector3 toAnchor = anchor - cameraPosition;
            if (toAnchor.sqrMagnitude < 1e-6f)
            {
                return anchor;
            }

            var sightLine = new Ray(cameraPosition, toAnchor);
            if (!body.bounds.IntersectRay(sightLine, out float distance) || distance <= 0f)
            {
                return anchor;
            }

            float surfaceDistance = Mathf.Max(0f, distance - _style.SurfaceOffset);
            return cameraPosition + sightLine.direction * surfaceDistance;
        }

        private Collider GetCollider(int slot)
        {
            if (slot == HitSource.PlayerSlot)
            {
                return _playerCollider;
            }

            return slot >= 0 && slot < _enemyColliders.Length ? _enemyColliders[slot] : null;
        }

        private HitVfxLibrarySO Library => _style != null ? _style.HitLibrary : null;

        private Combatant GetFighter(int slot)
        {
            if (_flow == null)
            {
                return null;
            }

            CombatModel model = _flow.Model;
            if (slot == HitSource.PlayerSlot)
            {
                return model.Player;
            }

            return slot >= 0 && slot < model.Enemies.Count ? model.Enemies[slot].Fighter : null;
        }

        private int IndexOfEnemy(Combatant enemy)
        {
            var enemies = _flow.Model.Enemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i].Fighter == enemy)
                {
                    return i;
                }
            }

            return -1;
        }

        private void SetRegistered(bool registered)
        {
            if (_ticks == null || _registered == registered)
            {
                return;
            }

            _registered = registered;
            if (registered)
            {
                _ticks.AddLateTickable(this);
            }
            else
            {
                _ticks.RemoveLateTickable(this);
            }
        }
    }
}
