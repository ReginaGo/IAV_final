using UnityEngine;
using UnityEngine.AI;

namespace MonsterSandbox
{
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class MonsterMotor : MonoBehaviour
    {
        public SandboxPlayer player;
        public float eyeHeight = 1.5f;
        public float detectionRange = 28f;
        public string State { get; set; } = "Idle";
        public NavMeshAgent Agent { get; private set; }
        public Vector3 Home { get; private set; }
        private float nextPath;
        private float nextAttack;
        private Renderer[] visuals;

        private void Awake()
        {
            Agent = GetComponent<NavMeshAgent>();
            Home = transform.position;
            visuals = GetComponentsInChildren<Renderer>();
        }

        public bool Ready => player && !player.IsDead && Agent && Agent.isOnNavMesh;
        public float Distance => player ? Vector3.Distance(transform.position, player.transform.position) : float.PositiveInfinity;
        public bool ClearLine(Vector3 from, Vector3 to) => !Physics.Linecast(from, to, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        public bool SeesPlayer(float fieldOfView = 140f)
        {
            if (!Ready || Distance > detectionRange) return false;
            Vector3 direction = player.View.transform.position - (transform.position + Vector3.up * eyeHeight);
            return Vector3.Angle(transform.forward, direction) <= fieldOfView * .5f &&
                   ClearLine(transform.position + Vector3.up * eyeHeight, player.View.transform.position);
        }

        public bool IsObserved()
        {
            if (!Ready) return false;
            Bounds bounds = new Bounds(transform.position + Vector3.up * eyeHeight * .5f, new Vector3(.8f, eyeHeight, .8f));
            foreach (var renderer in visuals)
                if (renderer && renderer.enabled) bounds.Encapsulate(renderer.bounds);
            // Sample the body and extremities: seeing part of a monster counts, but walls occlude it.
            return VisiblePoint(bounds.center) || VisiblePoint(bounds.center + Vector3.up * bounds.extents.y * .85f) ||
                   VisiblePoint(bounds.center - Vector3.up * bounds.extents.y * .75f) ||
                   VisiblePoint(bounds.center + player.View.transform.right * bounds.extents.x) ||
                   VisiblePoint(bounds.center - player.View.transform.right * bounds.extents.x);
        }

        private bool VisiblePoint(Vector3 point)
        {
            Vector3 screen = player.View.WorldToViewportPoint(point);
            return screen.z > 0 && screen.z <= player.View.farClipPlane && screen.x >= 0 && screen.x <= 1 &&
                   screen.y >= 0 && screen.y <= 1 && ClearLine(player.View.transform.position, point);
        }

        public void MoveTo(Vector3 position, float speed)
        {
            if (!Ready) { Stop(); return; }
            Agent.isStopped = false;
            Agent.speed = speed;
            if (Time.time < nextPath) return;
            nextPath = Time.time + .15f;
            if (NavMesh.SamplePosition(position, out var hit, 3f, Agent.areaMask)) Agent.SetDestination(hit.position);
            else Stop();
        }

        public void Stop()
        {
            if (!Agent || !Agent.isOnNavMesh) return;
            Agent.isStopped = true;
            Agent.velocity = Vector3.zero;
        }

        public void Attack(float damage, float reach = 1.65f)
        {
            if (!Ready || Distance > reach || Time.time < nextAttack ||
                !ClearLine(transform.position + Vector3.up * eyeHeight, player.View.transform.position)) return;
            nextAttack = Time.time + 1f;
            player.TakeDamage(damage, name);
        }

        private void OnDisable() { Stop(); }
    }
}
