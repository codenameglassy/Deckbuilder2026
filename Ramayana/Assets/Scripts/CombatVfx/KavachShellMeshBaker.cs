// Bakes the extrusion directions a Kavach hull pushes its vertices along. Meshes with hard edges keep a separate
// normal per face at the same corner, so pushing each vertex along its own normal tears the hull apart at every edge;
// SmoothNormals averages the normals of all vertices that share a position, so every copy of a corner moves the same
// way (a cube's corners move along their diagonals and the hull stays a closed, larger cube). ToTangentSpace stores
// each smoothed normal relative to the vertex's own normal and tangent, because skinning moves normals and tangents
// but not extra UV channels: the shader rebuilds the direction from the skinned frame, so it bends with the body.
// Plain C# with no Unity object calls, covered by EditMode tests. Used by the editor builder that makes shell meshes.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace JourneyToLanka.CombatVfx
{
    public static class KavachShellMeshBaker
    {
        // Positions closer than this (in mesh units) count as the same corner.
        private const float WeldDistance = 1e-4f;

        public static Vector3[] SmoothNormals(Vector3[] positions, Vector3[] normals)
        {
            if (positions == null || normals == null || positions.Length != normals.Length)
            {
                throw new ArgumentException("Positions and normals must be non-null and the same length.");
            }

            var sums = new Dictionary<Vector3Int, Vector3>(positions.Length);
            var keys = new Vector3Int[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                Vector3Int key = Weld(positions[i]);
                keys[i] = key;
                sums.TryGetValue(key, out Vector3 sum);
                sums[key] = sum + normals[i];
            }

            var smoothed = new Vector3[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                Vector3 sum = sums[keys[i]];
                smoothed[i] = sum.sqrMagnitude > 1e-12f ? sum.normalized : normals[i];
            }

            return smoothed;
        }

        // Expresses each smoothed normal in its vertex's tangent frame: x along the tangent, y along the bitangent
        // (normal cross tangent, times the tangent's w sign, as Unity builds it), z along the normal.
        public static Vector3[] ToTangentSpace(Vector3[] smoothed, Vector3[] normals, Vector4[] tangents)
        {
            if (smoothed == null || normals == null || tangents == null
                || smoothed.Length != normals.Length || smoothed.Length != tangents.Length)
            {
                throw new ArgumentException("Smoothed normals, normals and tangents must be non-null and the same length.");
            }

            var result = new Vector3[smoothed.Length];
            for (int i = 0; i < smoothed.Length; i++)
            {
                Vector3 normal = normals[i].normalized;
                Vector3 tangent = new Vector3(tangents[i].x, tangents[i].y, tangents[i].z);
                tangent = (tangent - normal * Vector3.Dot(normal, tangent)).normalized;
                Vector3 bitangent = Vector3.Cross(normal, tangent) * (tangents[i].w < 0f ? -1f : 1f);
                Vector3 direction = smoothed[i];
                result[i] = new Vector3(Vector3.Dot(direction, tangent), Vector3.Dot(direction, bitangent), Vector3.Dot(direction, normal));
            }

            return result;
        }

        private static Vector3Int Weld(Vector3 position)
        {
            return new Vector3Int(
                Mathf.RoundToInt(position.x / WeldDistance),
                Mathf.RoundToInt(position.y / WeldDistance),
                Mathf.RoundToInt(position.z / WeldDistance));
        }
    }
}
