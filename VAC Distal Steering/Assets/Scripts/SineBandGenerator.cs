using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class SineBandGenerator : MonoBehaviour
{
    [Header("End caps (visual start/end indicators)")]
    [Tooltip("Cap length measured along Lx (projected X length) in meters. If your whole SinePath is scaled with depth, keep this constant.")]
    public float capLengthLxRef = 0.05f;   // 5cm

    [Tooltip("Main corridor material")]
    public Material mainMat;

    [Tooltip("Start indicator material (green)")]
    public Material startMat;

    [Tooltip("End indicator material (red)")]
    public Material endMat;

    [Tooltip("direction: 0 = LR (green at left/start), 1 = RL (green at right/start)")]
    public int direction = 0;

    [Header("Sine parameters (meters, in THIS object's local space)")]
    [Tooltip("Projected X span (not arc length). Use SetByCenterlineLength to solve from L.")]
    public float Lx = 0.6f;

    [Tooltip("Corridor width W (band thickness)")]
    public float width = 0.05f;

    public float amplitude = 0.08f;
    public float wavelength = 0.8f;

    [Header("Sampling")]
    public int segments = 120;
    public bool regenerateEveryFrame = false;

    [Header("Debug")]
    public bool drawCenterlineGizmos = true;
    public bool drawCenterlineDebugLines = false;
    public float debugLineDuration = 0.1f;

    private MeshFilter mf;
    private MeshCollider mc;
    private MeshRenderer mr;

    // cache for gizmos (world space)
    private Vector3[] centerlineWorld;
    private bool hasCenterline = false;

    private Vector3[] centerLocal;
    private float[] cumLen;
    private float totalLen;

    void Awake()
    {
        mf = GetComponent<MeshFilter>();
        mc = GetComponent<MeshCollider>();
        mr = GetComponent<MeshRenderer>();

        Generate();
    }

    void Update()
    {
        if (regenerateEveryFrame) Generate();
    }

    /// <summary>
    /// Generate the sine corridor mesh and collider, and (optionally) color end caps.
    /// </summary>
    public void Generate()
    {
        segments = Mathf.Max(8, segments);
        float halfW = Mathf.Max(0.0001f, width * 0.5f);
        float lambda = Mathf.Max(1e-5f, wavelength);

        // vertices: 2 per segment sample (left/right)
        Vector3[] verts = new Vector3[(segments + 1) * 2];
        Vector2[] uvs = new Vector2[verts.Length];

        // Centerline cache for gizmos/debug
        if (centerlineWorld == null || centerlineWorld.Length != (segments + 1))
            centerlineWorld = new Vector3[segments + 1];

        // Local plane: X = progress, Z = lateral, Y = 0
        float k = 2f * Mathf.PI / lambda;

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float x = Mathf.Lerp(-Lx * 0.5f, Lx * 0.5f, t);

            // phase uses x shifted so start is phase 0 at left end
            float phase = k * (x + Lx * 0.5f);
            float z = amplitude * Mathf.Sin(phase);

            // tangent in XZ plane using derivative dz/dx
            float dzdx = amplitude * k * Mathf.Cos(phase);
            Vector3 tangent = new Vector3(1f, 0f, dzdx).normalized;

            // normal in plane (perpendicular to tangent)
            Vector3 normal = new Vector3(-tangent.z, 0f, tangent.x); // rotate tangent 90° in plane

            Vector3 center = new Vector3(x, 0f, z);
            Vector3 left = center - normal * halfW;
            Vector3 right = center + normal * halfW;

            int vi = i * 2;
            verts[vi + 0] = left;
            verts[vi + 1] = right;

            uvs[vi + 0] = new Vector2(t, 0f);
            uvs[vi + 1] = new Vector2(t, 1f);

            // cache world centerline point
            Vector3 centerWorld = transform.TransformPoint(center);
            centerlineWorld[i] = centerWorld;

            // Optional Debug.DrawLine (only visible while playing; mostly for quick checks)
            if (drawCenterlineDebugLines && i > 0)
            {
                Debug.DrawLine(centerlineWorld[i - 1], centerlineWorld[i], Color.yellow, debugLineDuration);
            }

            if (centerLocal == null || centerLocal.Length != segments + 1)
                centerLocal = new Vector3[segments + 1];
            if (cumLen == null || cumLen.Length != segments + 1)
                cumLen = new float[segments + 1];

            centerLocal[i] = center;
        }

        cumLen[0] = 0f;
        totalLen = 0f;
        for (int i = 1; i <= segments; i++)
        {
            float seg = Vector3.Distance(centerLocal[i - 1], centerLocal[i]);
            totalLen += seg;
            cumLen[i] = totalLen;
        }

        hasCenterline = true;

        // --- Submeshes (start cap / main / end cap) ---
        // cap length along Lx converted to segment count
        float capLx = Mathf.Clamp(capLengthLxRef, 0f, Mathf.Max(0f, Lx));
        int capSeg = Mathf.RoundToInt((capLx / Mathf.Max(1e-6f, Lx)) * segments);
        capSeg = Mathf.Clamp(capSeg, 1, Mathf.Max(1, segments / 2));

        var startTris = new List<int>(capSeg * 6 + 6);
        var mainTris = new List<int>(segments * 6);
        var endTris = new List<int>(capSeg * 6 + 6);

        for (int i = 0; i < segments; i++)
        {
            int vi = i * 2;

            // quad indices: left0, right0, left1, right1
            int left0 = vi;
            int right0 = vi + 1;
            int left1 = vi + 2;
            int right1 = vi + 3;

            bool isStartRegion = (i < capSeg);
            bool isEndRegion = (i >= segments - capSeg);

            // Swap start/end by direction (RL)
            if (direction == 1)
            {
                bool tmp = isStartRegion;
                isStartRegion = isEndRegion;
                isEndRegion = tmp;
            }

            List<int> bucket = mainTris;
            if (isStartRegion) bucket = startTris;
            else if (isEndRegion) bucket = endTris;

            // two triangles per quad (winding consistent)
            bucket.Add(left0); bucket.Add(left1); bucket.Add(right0);
            bucket.Add(right0); bucket.Add(left1); bucket.Add(right1);
        }

        Mesh m = mf.sharedMesh;
        if (m == null)
        {
            m = new Mesh();
            m.name = "SineBandMesh";
            mf.sharedMesh = m;
        }
        else
        {
            m.Clear();
        }

        m.vertices = verts;
        m.uv = uvs;

        // assign submeshes
        m.subMeshCount = 3;
        m.SetTriangles(startTris, 0);
        m.SetTriangles(mainTris, 1);
        m.SetTriangles(endTris, 2);

        m.RecalculateNormals();
        m.RecalculateBounds();

        // update collider (MeshCollider needs sharedMesh)
        mc.sharedMesh = null;
        mc.sharedMesh = m;

        // Assign materials (must match submesh count)
        if (mr != null && mainMat != null && startMat != null && endMat != null)
        {
            // order matches subMeshCount: 0=startcap, 1=main, 2=endcap
            mr.sharedMaterials = new Material[] { startMat, mainMat, endMat };
        }
    }

    public void EvalWorld(Vector3 worldPoint, out float progress, out float lateral)
    {
        progress = 0f;
        lateral = 0f;
        if (centerLocal == null || cumLen == null || centerLocal.Length < 2) return;

        Vector3 lp3 = transform.InverseTransformPoint(worldPoint);
        Vector2 p = new Vector2(lp3.x, lp3.z);

        float bestD2 = float.PositiveInfinity;
        int bestI = 0;
        float bestU = 0f;
        Vector2 bestQ = Vector2.zero;

        int n = centerLocal.Length;
        for (int i = 0; i < n - 1; i++)
        {
            Vector2 a = new Vector2(centerLocal[i].x, centerLocal[i].z);
            Vector2 b = new Vector2(centerLocal[i + 1].x, centerLocal[i + 1].z);
            Vector2 ab = b - a;
            float ab2 = ab.sqrMagnitude;
            if (ab2 < 1e-12f) continue;

            float u = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab2);
            Vector2 q = a + u * ab;
            float d2 = (p - q).sqrMagnitude;

            if (d2 < bestD2)
            {
                bestD2 = d2;
                bestI = i;
                bestU = u;
                bestQ = q;
            }
        }

        Vector2 a2 = new Vector2(centerLocal[bestI].x, centerLocal[bestI].z);
        Vector2 b2 = new Vector2(centerLocal[bestI + 1].x, centerLocal[bestI + 1].z);
        Vector2 t = (b2 - a2);
        float segLen = t.magnitude;
        if (segLen < 1e-8f) return;
        t /= segLen;

        Vector2 nrm = new Vector2(-t.y, t.x); // left normal
        lateral = Vector2.Dot(p - bestQ, nrm);

        progress = cumLen[bestI] + bestU * segLen;

        // direction: 0 LR (start at left), 1 RL (start at right)
        if (direction == 1)
            progress = totalLen - progress;
    }

    /// <summary>
    /// Set the sine band using centerline arc length L (target), and auto-solve Lx.
    /// Call this ONCE per trial.
    /// </summary>
    public void SetByCenterlineLength(float L_target, float W, float A, float lambda)
    {
        width = W;
        amplitude = A;
        wavelength = lambda;

        Lx = SolveLxForArcLength(L_target, amplitude, wavelength, 1e-4f, 40);

        float check = ArcLengthForLx(Lx, amplitude, wavelength);
        Debug.Log($"Target L={L_target:F4}, solved Lx={Lx:F4}, arc={check:F4}");

        Generate();
    }

    // ---------------- Solver helpers ----------------

    static float ArcLengthForLx(float Lx, float A, float lambda, int samples = 400)
    {
        float lam = Mathf.Max(1e-6f, lambda);
        float k = 2f * Mathf.PI / lam;

        float dx = Lx / Mathf.Max(1, samples);

        float length = 0f;
        float xPrev = 0f;
        float zPrev = A * Mathf.Sin(k * xPrev);

        for (int i = 1; i <= samples; i++)
        {
            float x = i * dx;
            float z = A * Mathf.Sin(k * x);

            float seg = Mathf.Sqrt((x - xPrev) * (x - xPrev) + (z - zPrev) * (z - zPrev));
            length += seg;

            xPrev = x;
            zPrev = z;
        }
        return length;
    }

    static float SolveLxForArcLength(float L_target, float A, float lambda, float tol, int maxIter)
    {
        float lo = 0f;
        float hi = Mathf.Max(L_target, 1e-3f);

        // expand upper bound if needed
        float sHi = ArcLengthForLx(hi, A, lambda);
        int expand = 0;
        while (sHi < L_target && expand < 20)
        {
            hi *= 1.5f;
            sHi = ArcLengthForLx(hi, A, lambda);
            expand++;
        }

        for (int it = 0; it < maxIter; it++)
        {
            float mid = 0.5f * (lo + hi);
            float sMid = ArcLengthForLx(mid, A, lambda);

            if (Mathf.Abs(sMid - L_target) < tol) return mid;
            if (sMid < L_target) lo = mid;
            else hi = mid;
        }

        return 0.5f * (lo + hi);
    }

    // ---------------- Gizmos (so your yellow line ALWAYS shows) ----------------
    // Debug.DrawLine only appears for a short time and only while playing.
    // Gizmos will show in Scene view with Gizmos enabled.

    void OnDrawGizmos()
    {
        if (!drawCenterlineGizmos) return;

        // If not generated yet (edit mode), try to create a lightweight centerline preview.
        if (!Application.isPlaying)
        {
            DrawPreviewGizmosInEditMode();
            return;
        }

        if (!hasCenterline || centerlineWorld == null || centerlineWorld.Length < 2) return;

        Gizmos.color = Color.yellow;
        for (int i = 1; i < centerlineWorld.Length; i++)
        {
            Gizmos.DrawLine(centerlineWorld[i - 1], centerlineWorld[i]);
        }
    }

    private void DrawPreviewGizmosInEditMode()
    {
        // Draw a preview even in edit mode without generating the mesh
        int seg = Mathf.Max(8, segments);
        float lam = Mathf.Max(1e-5f, wavelength);
        float k = 2f * Mathf.PI / lam;

        Vector3 prev = Vector3.zero;
        bool hasPrev = false;

        Gizmos.color = Color.yellow;

        for (int i = 0; i <= seg; i++)
        {
            float t = (float)i / seg;
            float x = Mathf.Lerp(-Lx * 0.5f, Lx * 0.5f, t);
            float phase = k * (x + Lx * 0.5f);
            float z = amplitude * Mathf.Sin(phase);

            Vector3 pWorld = transform.TransformPoint(new Vector3(x, 0f, z));

            if (hasPrev) Gizmos.DrawLine(prev, pWorld);
            prev = pWorld;
            hasPrev = true;
        }
    }
}