#if UNITY_3_5 || UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_4_8 || UNITY_4_9
#  define PRE_UNITY_5_0
#endif

using UnityEngine;

public class SS_MeshCombineUtility {
    public static Mesh Combine(MeshInstance[] combines) {
        int vertexCount = 0;
        int triangleCount = 0;
        foreach (MeshInstance combine in combines) {
            if (combine.mesh) {
                vertexCount += combine.mesh.vertexCount;
            }
        }

        // Precomputed how many triangles we need instead
        foreach (MeshInstance combine in combines) {
            if (combine.mesh) {
                triangleCount += combine.mesh.GetTriangles(combine.subMeshIndex).Length;
            }
        }

        Vector3[] vertices = new Vector3[vertexCount];
        Vector3[] normals = new Vector3[vertexCount];
        Vector4[] tangents = new Vector4[vertexCount];
        Vector2[] uv = new Vector2[vertexCount];
        Vector2[] uv1 = new Vector2[vertexCount];
        Color[] colors = new Color[vertexCount];

        int[] triangles = new int[triangleCount];

        int offset = 0;
        foreach (MeshInstance combine in combines) {
            if (combine.mesh) {
                Copy(combine.mesh.vertexCount, combine.mesh.vertices, vertices, ref offset, combine.transform);
            }
        }

        offset = 0;
        foreach (MeshInstance combine in combines) {
            if (combine.mesh) {
                Matrix4x4 invTranspose = combine.transform;
                invTranspose = invTranspose.inverse.transpose;
                CopyNormal(combine.mesh.vertexCount, combine.mesh.normals, normals, ref offset, invTranspose);
            }
        }
        offset = 0;
        foreach (MeshInstance combine in combines) {
            if (combine.mesh) {
                Matrix4x4 invTranspose = combine.transform;
                invTranspose = invTranspose.inverse.transpose;
                CopyTangents(combine.mesh.vertexCount, combine.mesh.tangents, tangents, ref offset, invTranspose);
            }
        }
        offset = 0;
        foreach (MeshInstance combine in combines) {
            if (combine.mesh) {
                Copy(combine.mesh.vertexCount, combine.mesh.uv, uv, ref offset);
            }
        }

        offset = 0;
        int offsetUV = 0;
        int row = (int) Mathf.Ceil(Mathf.Sqrt(combines.Length));
        float sizeUV = (float) 1 / row;
        foreach (MeshInstance combine in combines) {
            if (combine.mesh) {
#if PRE_UNITY_5_0
                CopyUV(combine.mesh.vertexCount, combine.mesh.uv1, uv1, sizeUV, row, ref offset, ref offsetUV);
#else
                CopyUV(combine.mesh.vertexCount, combine.mesh.uv2, uv1, sizeUV, row, ref offset, ref offsetUV);
#endif
            }
        }

        offset = 0;
        foreach (MeshInstance combine in combines) {
            if (combine.mesh) {
                CopyColors(combine.mesh.vertexCount, combine.mesh.colors, colors, ref offset);
            }
        }

        int triangleOffset = 0;
        int vertexOffset = 0;
        foreach (MeshInstance combine in combines) {
            if (combine.mesh) {
                int[] inputtriangles = combine.mesh.GetTriangles(combine.subMeshIndex);
                for (int i = 0; i < inputtriangles.Length; i++) {
                    triangles[i + triangleOffset] = inputtriangles[i] + vertexOffset;
                }
                triangleOffset += inputtriangles.Length;

                vertexOffset += combine.mesh.vertexCount;
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = "Combined Mesh";
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.colors = colors;
        mesh.uv = uv;
#if PRE_UNITY_5_0
        mesh.uv1 = uv1;
#else
        mesh.uv2 = uv1;
#endif
        mesh.tangents = tangents;
        mesh.triangles = triangles;

        return mesh;
    }

    private static void Copy(int vertexcount, Vector3[] src, Vector3[] dst, ref int offset, Matrix4x4 transform) {
        for (int i = 0; i < src.Length; i++) {
            dst[i + offset] = transform.MultiplyPoint(src[i]);
        }
        offset += vertexcount;
    }

    private static void CopyNormal(int vertexcount, Vector3[] src, Vector3[] dst, ref int offset, Matrix4x4 transform) {
        for (int i = 0; i < src.Length; i++) {
            dst[i + offset] = transform.MultiplyVector(src[i]).normalized;
        }
        offset += vertexcount;
    }

    private static void Copy(int vertexcount, Vector2[] src, Vector2[] dst, ref int offset) {
        for (int i = 0; i < src.Length; i++) {
            dst[i + offset] = src[i];
        }
        offset += vertexcount;
    }

    private static void CopyUV(int vertexcount, Vector2[] src, Vector2[] dst, float sizeUV, int row, ref int offset,
        ref int offsetUV) {
        int offsetUVX = offsetUV % row;
        int offsetUVY = offsetUV / row;
        for (int j = 0; j < vertexcount; j++) {
            if (j + offset >= dst.Length || j >= src.Length) {
                break;
            }

            dst[j + offset] = new Vector2(src[j].x * sizeUV + sizeUV * offsetUVX, src[j].y * sizeUV + sizeUV * offsetUVY);
        }
        offset += vertexcount;
        offsetUV += 1;
    }

    private static void CopyColors(int vertexcount, Color[] src, Color[] dst, ref int offset) {
        for (int i = 0; i < src.Length; i++) {
            dst[i + offset] = src[i];
        }
        offset += vertexcount;
    }

    private static void CopyTangents(int vertexcount, Vector4[] src, Vector4[] dst, ref int offset, Matrix4x4 transform) {
        for (int i = 0; i < src.Length; i++) {
            Vector4 p4 = src[i];
            Vector3 p = new Vector3(p4.x, p4.y, p4.z);
            p = transform.MultiplyVector(p).normalized;
            dst[i + offset] = new Vector4(p.x, p.y, p.z, p4.w);
        }

        offset += vertexcount;
    }

    public struct MeshInstance {
        public Mesh mesh;
        public int subMeshIndex;
        public Matrix4x4 transform;
    }
}