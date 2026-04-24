using UnityEngine;

public class TunnelBuilder : MonoBehaviour
{
    [Header("Mesh Settings")]
    public int radialSegments = 32;
    public Material tunnelMaterial;

    public GameObject currentTunnelGO;

    /// <summary>
    /// Destroys any previous tunnel, builds a new mesh, and returns
    /// the corresponding TunnelSegment for math queries.
    /// </summary>
    public TunnelSegment BuildTunnel(TrialConfig cfg,
                                     Vector3 startPt,
                                     Vector3 endPt)
    {
        if (currentTunnelGO != null)
            Destroy(currentTunnelGO);

        float r0 = cfg.startWidth;
        float r1 = cfg.endWidth;

        currentTunnelGO = new GameObject("Tunnel");
        currentTunnelGO.transform.SetParent(transform, worldPositionStays: true);

        var mf = currentTunnelGO.AddComponent<MeshFilter>();
        var mr = currentTunnelGO.AddComponent<MeshRenderer>();
        mr.material = tunnelMaterial;

        mf.mesh = BuildFrustumMesh(startPt, endPt,
                                        r0/2, r1/2,
                                        radialSegments);

        return new TunnelSegment
        {
            startPoint = startPt,
            endPoint = endPt,
            startRadius = r0/2,
            endRadius = r1/2
        };
    }

    // -------------------------------------------------------------------------
    // Mesh generation
    // -------------------------------------------------------------------------

    Mesh BuildFrustumMesh(Vector3 startPt, Vector3 endPt,
                          float r0, float r1, int segs)
    {
        
        // Build in local space along +Z then rotate to match world axis
        Vector3 axisDir = (endPt - startPt).normalized;
        float length = Vector3.Distance(startPt, endPt);

        // We'll build the mesh in world space directly using the axis
        Quaternion rot = Quaternion.FromToRotation(Vector3.forward, axisDir);

        int ringCount = 2;
        var verts = new Vector3[ringCount * segs];
        var norms = new Vector3[ringCount * segs];
        var uvs = new Vector2[ringCount * segs];

        for (int ring = 0; ring < ringCount; ring++)
        {
            float t = ring / (float)(ringCount - 1);
            float r = Mathf.Lerp(r0, r1, t);
            Vector3 ringCenter = startPt + axisDir * (t * length);

            for (int i = 0; i < segs; i++)
            {
                float angle = i / (float)segs * Mathf.PI * 2f;
                Vector3 offset = rot * new Vector3(Mathf.Cos(angle) * r,
                                                   Mathf.Sin(angle) * r,
                                                   0f);
                int idx = ring * segs + i;
                verts[idx] = ringCenter + offset;
                norms[idx] = (-offset).normalized;   // inward-facing
                uvs[idx] = new Vector2(i / (float)segs, t);
            }
        }

        var tris = new int[(ringCount - 1) * segs * 6];
        int ti = 0;
        for (int ring = 0; ring < ringCount - 1; ring++)
        {
            for (int i = 0; i < segs; i++)
            {
                int next = (i + 1) % segs;
                int a = ring * segs + i;
                int b = ring * segs + next;
                int c = (ring + 1) * segs + i;
                int d = (ring + 1) * segs + next;

                // Reversed winding >> inward-facing normals
                tris[ti++] = a; tris[ti++] = c; tris[ti++] = b;
                tris[ti++] = b; tris[ti++] = c; tris[ti++] = d;
            }
        }

        var mesh = new Mesh { name = "TunnelFrustum" };
        mesh.vertices = verts;
        mesh.normals = norms;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateBounds();
        return mesh;
    }
}