// EditMode tests for KavachShellMeshBaker: a hard-edged cube's corners each get one shared diagonal direction, a
// vertex alone at its position keeps its own normal, the vertex count and order never change, and a direction stored
// in tangent space rebuilds to the same world direction from the vertex's frame, including a mirrored (negative w)
// tangent.
using NUnit.Framework;
using UnityEngine;

namespace JourneyToLanka.CombatVfx.Tests
{
    public sealed class KavachShellMeshBakerTests
    {
        [Test]
        public void Cube_Corner_Copies_Share_One_Diagonal()
        {
            var positions = new[] { new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.5f, 0.5f, 0.5f) };
            var normals = new[] { Vector3.right, Vector3.up, Vector3.forward };

            Vector3[] smoothed = KavachShellMeshBaker.SmoothNormals(positions, normals);

            Vector3 diagonal = new Vector3(1f, 1f, 1f).normalized;
            for (int i = 0; i < smoothed.Length; i++)
            {
                Assert.AreEqual(0f, Vector3.Distance(diagonal, smoothed[i]), 1e-5f);
            }
        }

        [Test]
        public void Lone_Vertex_Keeps_Its_Normal_And_Order_Is_Kept()
        {
            var positions = new[] { Vector3.zero, new Vector3(1f, 0f, 0f), Vector3.zero };
            var normals = new[] { Vector3.up, Vector3.left, Vector3.right };

            Vector3[] smoothed = KavachShellMeshBaker.SmoothNormals(positions, normals);

            Assert.AreEqual(3, smoothed.Length);
            Assert.AreEqual(0f, Vector3.Distance(Vector3.left, smoothed[1]), 1e-5f);
            Assert.AreEqual(0f, Vector3.Distance(smoothed[0], smoothed[2]), 1e-5f);
        }

        [TestCase(1f)]
        [TestCase(-1f)]
        public void Tangent_Space_Rebuilds_The_Same_Direction(float handedness)
        {
            Vector3 normal = Vector3.up;
            var tangent = new Vector4(1f, 0f, 0f, handedness);
            Vector3 direction = new Vector3(0.3f, 0.8f, -0.5f).normalized;

            Vector3 stored = KavachShellMeshBaker.ToTangentSpace(new[] { direction }, new[] { normal }, new[] { tangent })[0];

            Vector3 tangentAxis = new Vector3(tangent.x, tangent.y, tangent.z);
            Vector3 bitangent = Vector3.Cross(normal, tangentAxis) * handedness;
            Vector3 rebuilt = tangentAxis * stored.x + bitangent * stored.y + normal * stored.z;
            Assert.AreEqual(0f, Vector3.Distance(direction, rebuilt), 1e-5f);
        }
    }
}
