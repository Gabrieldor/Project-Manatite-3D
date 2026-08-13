using UnityEngine;

namespace Manatite.Environment
{
    /// <summary>
    /// Blue ring shown around a planet once its enemy encounter has been fully cleared —
    /// a permanent "this planet is done" signal. Sized in the parent's local space (the
    /// planet's unit-sphere <c>Visual</c>), so it scales automatically with each planet's
    /// randomized size. Starts hidden; <see cref="Show"/> reveals it.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class PlanetClearedRing : MonoBehaviour
    {
        [SerializeField] private float innerRadius = 0.6f;
        [SerializeField] private float outerRadius = 0.72f;
        [SerializeField] private int segments = 64;
        [SerializeField] private Color color = new Color(0.25f, 0.6f, 1f, 0.85f);

        private static Mesh _sharedRingMesh;
        private static MaterialPropertyBlock _propertyBlock;

        private void Awake()
        {
            if (_sharedRingMesh == null)
                _sharedRingMesh = BuildRingMesh(innerRadius, outerRadius, segments);

            GetComponent<MeshFilter>().sharedMesh = _sharedRingMesh;

            _propertyBlock ??= new MaterialPropertyBlock();
            _propertyBlock.SetColor("_BaseColor", color);
            GetComponent<MeshRenderer>().SetPropertyBlock(_propertyBlock);
        }

        public void Show() => gameObject.SetActive(true);

        private static Mesh BuildRingMesh(float inner, float outer, int segmentCount)
        {
            var vertices = new Vector3[segmentCount * 2];
            var triangles = new int[segmentCount * 6];

            for (int i = 0; i < segmentCount; i++)
            {
                float angle = i / (float)segmentCount * Mathf.PI * 2f;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);
                vertices[i * 2] = new Vector3(cos * inner, 0f, sin * inner);
                vertices[i * 2 + 1] = new Vector3(cos * outer, 0f, sin * outer);

                int next = (i + 1) % segmentCount;
                int a = i * 2, b = i * 2 + 1, c = next * 2, d = next * 2 + 1;
                int t = i * 6;
                triangles[t] = a;
                triangles[t + 1] = c;
                triangles[t + 2] = b;
                triangles[t + 3] = b;
                triangles[t + 4] = c;
                triangles[t + 5] = d;
            }

            var mesh = new Mesh { name = "PlanetClearedRing" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
