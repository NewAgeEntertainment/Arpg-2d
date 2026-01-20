using UnityEngine;

public class NPC_PatrolGizmos : MonoBehaviour
{
    [SerializeField] private NPC npc;
    [SerializeField] private float radius = 0.12f;

    private void Reset()
    {
        npc = GetComponent<NPC>();
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (npc == null) npc = GetComponent<NPC>();
        if (npc == null) return;

        var pts = npc.patrolPoints;
        if (pts == null || pts.Length == 0) return;

        Gizmos.color = Color.cyan;

        for (int i = 0; i < pts.Length; i++)
        {
            Vector3 p = new Vector3(pts[i].x, pts[i].y, 0f);
            Gizmos.DrawSphere(p, radius);

            // draw line to next
            int next = i + 1;
            if (next >= pts.Length)
            {
                if (!npc.loopPatrol) break;
                next = 0;
            }

            Vector3 q = new Vector3(pts[next].x, pts[next].y, 0f);
            Gizmos.DrawLine(p, q);
        }
    }
#endif
}
