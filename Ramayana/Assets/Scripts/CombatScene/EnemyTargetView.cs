// Makes an enemy's stand-in object pickable as a card target. It builds a flat marker disc that shows while the enemy
// is the highlighted target, and reports pointer hover and clicks on the object's collider through plain C# events.
// Pointer events reach it through the EventSystem, so the scene camera needs a PhysicsRaycaster; UI drawn over the
// enemy blocks them as it should. CombatController binds it to its enemy's field slot. It never plays cards itself.
// Unhovered fires when the pointer leaves, so inspect widgets can hide what Hovered showed. TopCenter is the top of
// the enemy's collider in world space (its transform position when it has none), so UI can aim at or sit above an
// enemy of any size or scale. The disc renders with the marker style's material asset: a primitive created at runtime
// gets the built-in default material in a player build, which URP cannot draw (it shows pink), so it is always
// replaced.
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

namespace JourneyToLanka.CombatScene
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Journey to Lanka/Combat/Enemy Target View")]
    public sealed class EnemyTargetView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private TargetMarkerStyleSO _style;

        private Collider _collider;
        private Transform _marker;
        private Renderer _markerRenderer;
        private MaterialPropertyBlock _propertyBlock;

        public event Action<EnemyTargetView> Clicked;

        public event Action<EnemyTargetView> Hovered;

        public event Action<EnemyTargetView> Unhovered;

        public int EnemyIndex { get; private set; } = -1;

        public Vector3 TopCenter
        {
            get
            {
                if (_collider == null)
                {
                    return transform.position;
                }

                Bounds bounds = _collider.bounds;
                return new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
            }
        }

        private void Awake()
        {
            TryGetComponent(out _collider);
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "TargetMarker";
            Destroy(marker.GetComponent<Collider>());
            _marker = marker.transform;
            _marker.SetParent(transform, false);
            _markerRenderer = marker.GetComponent<Renderer>();
            _markerRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _markerRenderer.receiveShadows = false;
            _propertyBlock = new MaterialPropertyBlock();

            ApplyStyle();
            SetHighlighted(false);
        }

        private void OnEnable()
        {
            if (_style != null)
            {
                _style.Changed += ApplyStyle;
            }
        }

        private void OnDisable()
        {
            if (_style != null)
            {
                _style.Changed -= ApplyStyle;
            }

            SetHighlighted(false);
        }

        private void OnDestroy()
        {
            Clicked = null;
            Hovered = null;
            Unhovered = null;
        }

        public void Bind(int enemyIndex)
        {
            EnemyIndex = enemyIndex;
            SetHighlighted(false);
        }

        public void SetHighlighted(bool highlighted)
        {
            if (_marker != null)
            {
                _marker.gameObject.SetActive(highlighted);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                Clicked?.Invoke(this);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Hovered?.Invoke(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Unhovered?.Invoke(this);
        }

        private void ApplyStyle()
        {
            if (_style == null || _marker == null)
            {
                return;
            }

            if (_style.Material != null)
            {
                _markerRenderer.sharedMaterial = _style.Material;
            }
            else
            {
                Debug.LogError($"{name}: the Target Marker Style has no material, so the marker cannot render in a build. Run Tools > Journey to Lanka > Create Default Assets > Targeting.", this);
            }

            _marker.localPosition = _style.Offset;
            _marker.localScale = _style.Scale;
            _propertyBlock.SetColor(BaseColorId, _style.Color);
            _markerRenderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
