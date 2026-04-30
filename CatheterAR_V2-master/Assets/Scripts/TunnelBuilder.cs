using UnityEngine;

public class TunnelBuilder : MonoBehaviour
{
    [Header("Mesh Settings")]
    public int radialSegments = 32;
    public bool doubleSided = true;

    [Header("Tunnel Materials")]
    public Material tunnelMaterial;

    [Header("Three-Part Materials")]
    public bool useThreePartMaterials = false;
    public Material firstThirdMaterial;
    public Material secondThirdMaterial;
    public Material finalThirdMaterial;

    [Header("Boundary Rings")]
    public bool addBoundaryRings = true;
    public Material boundaryMaterial;
    public float boundaryWidth = 0.02f;

    public GameObject currentTunnelGO;

    public TunnelSegment BuildTunnel(
        TrialConfig cfg,
        Vector3 startPt,
        Vector3 endPt)
    {
        if (currentTunnelGO != null)
            Destroy(currentTunnelGO);

        float r0 = cfg.startWidth / 2f;
        float r1 = cfg.endWidth / 2f;

        currentTunnelGO = new GameObject("Tunnel");
        currentTunnelGO.transform.SetParent(transform, worldPositionStays: true);

        MeshFilter mf = currentTunnelGO.AddComponent<MeshFilter>();
        MeshRenderer mr = currentTunnelGO.AddComponent<MeshRenderer>();

        if (useThreePartMaterials)
        {
            mr.materials = new Material[]
            {
                firstThirdMaterial,
                secondThirdMaterial,
                finalThirdMaterial
            };
        }
        else
        {
            mr.material = tunnelMaterial;
        }

        mf.mesh = BuildFrustumMesh(
            startPt,
            endPt,
            r0,
            r1,
            radialSegments,
            doubleSided,
            useThreePartMaterials
        );

        if (addBoundaryRings && boundaryMaterial != null)
        {
            AddBoundaryRings(startPt, endPt, r0, r1);
        }

        return new TunnelSegment
        {
            startPoint = startPt,
            endPoint = endPt,
            startRadius = r0,
            endRadius = r1
        };
    }

    Mesh BuildFrustumMesh(
        Vector3 startPt,
        Vector3 endPt,
        float r0,
        float r1,
        int segs,
        bool makeDoubleSided,
        bool threeParts)
    {
        Vector3 axisDir = (endPt - startPt).normalized;
        float length = Vector3.Distance(startPt, endPt);
        Quaternion rot = Quaternion.FromToRotation(Vector3.forward, axisDir);

        int ringCount = threeParts ? 4 : 2;

        Vector3[] verts = new Vector3[ringCount * segs];
        Vector3[] norms = new Vector3[ringCount * segs];
        Vector2[] uvs = new Vector2[ringCount * segs];

        for (int ring = 0; ring < ringCount; ring++)
        {
            float t = ring / (float)(ringCount - 1);
            float r = Mathf.Lerp(r0, r1, t);
            Vector3 ringCenter = startPt + axisDir * (t * length);

            for (int i = 0; i < segs; i++)
            {
                float angle = i / (float)segs * Mathf.PI * 2f;

                Vector3 offset = rot * new Vector3(
                    Mathf.Cos(angle) * r,
                    Mathf.Sin(angle) * r,
                    0f
                );

                int idx = ring * segs + i;

                verts[idx] = ringCenter + offset;
                norms[idx] = offset.normalized;
                uvs[idx] = new Vector2(i / (float)segs, t);
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = threeParts ? "Tunnel_ThreePart" : "Tunnel_SingleMaterial";

        mesh.vertices = verts;
        mesh.normals = norms;
        mesh.uv = uvs;

        if (threeParts)
        {
            mesh.subMeshCount = 3;

            for (int part = 0; part < 3; part++)
            {
                mesh.SetTriangles(
                    BuildTrianglesForPart(part, segs, makeDoubleSided),
                    part
                );
            }
        }
        else
        {
            mesh.triangles = BuildTrianglesForPart(0, segs, makeDoubleSided);
        }

        mesh.RecalculateBounds();
        return mesh;
    }

    int[] BuildTrianglesForPart(
        int part,
        int segs,
        bool makeDoubleSided)
    {
        int triangleMultiplier = makeDoubleSided ? 12 : 6;
        int[] tris = new int[segs * triangleMultiplier];

        int ring = part;
        int ti = 0;

        for (int i = 0; i < segs; i++)
        {
            int next = (i + 1) % segs;

            int a = ring * segs + i;
            int b = ring * segs + next;
            int c = (ring + 1) * segs + i;
            int d = (ring + 1) * segs + next;

            // Outside-facing
            tris[ti++] = a;
            tris[ti++] = b;
            tris[ti++] = c;

            tris[ti++] = b;
            tris[ti++] = d;
            tris[ti++] = c;

            if (makeDoubleSided)
            {
                // Inside-facing
                tris[ti++] = a;
                tris[ti++] = c;
                tris[ti++] = b;

                tris[ti++] = b;
                tris[ti++] = c;
                tris[ti++] = d;
            }
        }

        return tris;
    }

    void AddBoundaryRings(
        Vector3 startPt,
        Vector3 endPt,
        float r0,
        float r1)
    {
        if (useThreePartMaterials)
        {
            CreateRingAtT("StartRing", startPt, endPt, r0, r1, 0f);
            CreateRingAtT("OneThirdRing", startPt, endPt, r0, r1, 1f / 3f);
            CreateRingAtT("TwoThirdRing", startPt, endPt, r0, r1, 2f / 3f);
            CreateRingAtT("EndRing", startPt, endPt, r0, r1, 1f);
        }
        else
        {
            CreateRingAtT("StartRing", startPt, endPt, r0, r1, 0f);
            CreateRingAtT("EndRing", startPt, endPt, r0, r1, 1f);
        }
    }

    void CreateRingAtT(
        string name,
        Vector3 startPt,
        Vector3 endPt,
        float r0,
        float r1,
        float t)
    {
        GameObject ringGO = new GameObject(name);
        ringGO.transform.SetParent(currentTunnelGO.transform, worldPositionStays: true);

        LineRenderer lr = ringGO.AddComponent<LineRenderer>();
        lr.material = boundaryMaterial;
        lr.useWorldSpace = true;
        lr.loop = true;
        lr.positionCount = radialSegments;
        lr.widthMultiplier = boundaryWidth;

        Vector3 axisDir = (endPt - startPt).normalized;
        float length = Vector3.Distance(startPt, endPt);

        Vector3 center = startPt + axisDir * (t * length);
        float radius = Mathf.Lerp(r0, r1, t);

        Quaternion rot = Quaternion.FromToRotation(Vector3.forward, axisDir);

        for (int i = 0; i < radialSegments; i++)
        {
            float angle = i / (float)radialSegments * Mathf.PI * 2f;

            Vector3 offset = rot * new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                0f
            );

            lr.SetPosition(i, center + offset);
        }
    }
}