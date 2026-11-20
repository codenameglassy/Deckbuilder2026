// Editor-only painter for baking simple icon sprites from signed distance functions. Shapes live in a square from -1
// to 1 (y up). Paint draws one part painter-style: a dark outline along its edge, then its fill (lit slightly from
// the top), so overlapping parts read like inked Mithila shapes. Overlay tints the existing pixels inside a shape
// without growing the icon's outline (rings, studs, ridges). Encode writes the result as a PNG, with alpha taken from
// every painted part plus its outline. Static distance helpers cover the shapes the icon builders need. Used only by
// asset builders; no runtime code touches it.
using System;
using UnityEngine;

namespace JourneyToLanka.CombatScene.EditorTools
{
    public sealed class SdfIconCanvas
    {
        private readonly int _size;
        private readonly Color[] _colour;
        private readonly float[] _union;
        private readonly float _pixel;
        private readonly float _outlineWidth;
        private readonly Color _outlineColour;

        public SdfIconCanvas(int size, float outlineWidth, Color outlineColour)
        {
            _size = size;
            _colour = new Color[size * size];
            _union = new float[size * size];
            _pixel = 2f / size;
            _outlineWidth = outlineWidth;
            _outlineColour = outlineColour;
            for (int i = 0; i < _union.Length; i++)
            {
                _union[i] = float.MaxValue;
            }
        }

        public void Paint(Func<Vector2, float> distance, Color fill)
        {
            float half = _outlineWidth * 0.5f;
            for (int y = 0; y < _size; y++)
            {
                for (int x = 0; x < _size; x++)
                {
                    int index = y * _size + x;
                    Vector2 p = PointAt(x, y);
                    float d = distance(p);
                    _union[index] = Mathf.Min(_union[index], d);

                    Color colour = _colour[index];
                    colour = Color.Lerp(colour, _outlineColour, Coverage(Mathf.Abs(d) - half));
                    float shade = Mathf.Lerp(0.82f, 1.15f, (p.y + 1f) * 0.5f);
                    Color lit = new Color(fill.r * shade, fill.g * shade, fill.b * shade, 1f);
                    colour = Color.Lerp(colour, lit, Coverage(d + half));
                    _colour[index] = colour;
                }
            }
        }

        public void Overlay(Func<Vector2, float> distance, Color colour, float strength)
        {
            for (int y = 0; y < _size; y++)
            {
                for (int x = 0; x < _size; x++)
                {
                    int index = y * _size + x;
                    float amount = Coverage(distance(PointAt(x, y))) * strength;
                    _colour[index] = Color.Lerp(_colour[index], new Color(colour.r, colour.g, colour.b, 1f), amount);
                }
            }
        }

        public byte[] Encode()
        {
            var pixels = new Color32[_colour.Length];
            float half = _outlineWidth * 0.5f;
            for (int i = 0; i < pixels.Length; i++)
            {
                Color c = _colour[i];
                c.a = Coverage(_union[i] - half);
                pixels[i] = c;
            }

            var texture = new Texture2D(_size, _size, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply();
            byte[] png = texture.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(texture);
            return png;
        }

        public static float Circle(Vector2 p, Vector2 centre, float radius)
        {
            return (p - centre).magnitude - radius;
        }

        public static float Box(Vector2 p, Vector2 centre, Vector2 halfSize, float radius)
        {
            float qx = Mathf.Abs(p.x - centre.x) - halfSize.x + radius;
            float qy = Mathf.Abs(p.y - centre.y) - halfSize.y + radius;
            float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
            return outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
        }

        // Isosceles triangle pointing up: tip at tip, base halfWidth wide at height below the tip.
        public static float Triangle(Vector2 p, Vector2 tip, float halfWidth, float height)
        {
            float x = Mathf.Abs(p.x - tip.x);
            float y = tip.y - p.y;
            var q = new Vector2(halfWidth, height);
            var local = new Vector2(x, y);
            float t = Mathf.Clamp01(Vector2.Dot(local, q) / Vector2.Dot(q, q));
            Vector2 a = local - q * t;
            Vector2 b = local - new Vector2(q.x * Mathf.Clamp01(x / q.x), q.y);
            float squared = Mathf.Min(a.sqrMagnitude, b.sqrMagnitude);
            float sign = Mathf.Min(-(x * q.y - y * q.x), -(y - q.y));
            return -Mathf.Sqrt(squared) * Mathf.Sign(sign);
        }

        public static Vector2 Rotate(Vector2 p, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float c = Mathf.Cos(radians);
            float s = Mathf.Sin(radians);
            return new Vector2(p.x * c + p.y * s, -p.x * s + p.y * c);
        }

        private Vector2 PointAt(int x, int y)
        {
            return new Vector2((x + 0.5f) / _size * 2f - 1f, (y + 0.5f) / _size * 2f - 1f);
        }

        private float Coverage(float distance)
        {
            return Mathf.Clamp01(0.5f - distance / _pixel);
        }
    }
}
