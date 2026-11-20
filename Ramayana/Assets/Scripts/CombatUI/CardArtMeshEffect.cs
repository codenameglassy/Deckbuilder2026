// Gives a card's rounded UI pieces (the art window, the type ribbon, the cost orb) the data the Card Art shader needs.
// It writes each vertex's position within its own rect (0 to 1) into the second uv channel, so the shader can round
// the corners from that instead of from the texture uvs, which point into a sprite atlas and would bend the shape.
// For an Image showing a sprite it also crops the sprite's uvs to fill the rect without stretching ("cover"): a wide
// painting loses its sides, a tall one its top and bottom, always centred. It turns on the second uv channel on its
// canvas and root canvas when it is enabled or moves to another canvas, because canvases strip that channel by
// default. Images must use the Simple type; sprites packed with rotation are not supported.
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace JourneyToLanka.CombatUI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Graphic))]
    public sealed class CardArtMeshEffect : BaseMeshEffect
    {
        private const AdditionalCanvasShaderChannels ShapeChannel = AdditionalCanvasShaderChannels.TexCoord1;

        public override void ModifyMesh(VertexHelper vertices)
        {
            if (!IsActive())
            {
                return;
            }

            Rect rect = graphic.rectTransform.rect;
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            bool crop = TryGetCoverUv(rect, out Vector4 uvRect);
            UIVertex vertex = default;
            int count = vertices.currentVertCount;
            for (int i = 0; i < count; i++)
            {
                vertices.PopulateUIVertex(ref vertex, i);
                float x = (vertex.position.x - rect.xMin) / rect.width;
                float y = (vertex.position.y - rect.yMin) / rect.height;
                vertex.uv1 = new Vector4(x, y, 0f, 0f);
                if (crop)
                {
                    float u = Mathf.Lerp(uvRect.x, uvRect.z, x);
                    float v = Mathf.Lerp(uvRect.y, uvRect.w, y);
                    vertex.uv0 = new Vector4(u, v, 0f, 0f);
                }

                vertices.SetUIVertex(vertex, i);
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            EnsureShapeChannel();
        }

        protected override void OnCanvasHierarchyChanged()
        {
            base.OnCanvasHierarchyChanged();
            EnsureShapeChannel();
        }

        private bool TryGetCoverUv(Rect rect, out Vector4 uvRect)
        {
            uvRect = default;
            var image = graphic as Image;
            Sprite sprite = image != null ? image.overrideSprite : null;
            if (sprite == null || sprite.rect.height <= 0f)
            {
                return false;
            }

            Vector4 outer = DataUtility.GetOuterUV(sprite);
            float spriteAspect = sprite.rect.width / sprite.rect.height;
            float rectAspect = rect.width / rect.height;
            uvRect = outer;
            if (spriteAspect > rectAspect)
            {
                float keep = rectAspect / spriteAspect;
                float middle = (outer.x + outer.z) * 0.5f;
                float half = (outer.z - outer.x) * keep * 0.5f;
                uvRect.x = middle - half;
                uvRect.z = middle + half;
            }
            else if (spriteAspect < rectAspect)
            {
                float keep = spriteAspect / rectAspect;
                float middle = (outer.y + outer.w) * 0.5f;
                float half = (outer.w - outer.y) * keep * 0.5f;
                uvRect.y = middle - half;
                uvRect.w = middle + half;
            }

            return true;
        }

        private void EnsureShapeChannel()
        {
            Canvas canvas = graphic != null ? graphic.canvas : null;
            if (canvas == null)
            {
                return;
            }

            AddShapeChannel(canvas);
            Canvas root = canvas.rootCanvas;
            if (root != null && root != canvas)
            {
                AddShapeChannel(root);
            }
        }

        private static void AddShapeChannel(Canvas canvas)
        {
            if ((canvas.additionalShaderChannels & ShapeChannel) == 0)
            {
                canvas.additionalShaderChannels |= ShapeChannel;
            }
        }
    }
}
