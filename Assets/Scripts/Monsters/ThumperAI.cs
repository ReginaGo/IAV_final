using UnityEngine;

namespace MonsterSandbox
{
    [RequireComponent(typeof(MonsterMotor))]
    public sealed class ThumperAI : MonoBehaviour
    {
        public enum Behaviour { Patrol, Chasing, Searching }
        public Behaviour CurrentState { get; private set; }
        [SerializeField] private float acceleration = 2.5f;
        [SerializeField] private float maximumSpeed = 10f;
        private MonsterMotor motor;
        private Vector3 lastSeen;
        private float lastSeenTime = -100f;
        private float speed = 2f;
        private int patrolPoint;
        public void ResetBehaviour() { lastSeenTime = -100f; speed = 2; patrolPoint = 0; CurrentState = Behaviour.Patrol; }
        private void Awake() { motor = GetComponent<MonsterMotor>(); }
        private void Update()
        {
            if (!motor.Ready) { motor.Stop(); return; }
            if (motor.SeesPlayer())
            {
                lastSeen = motor.player.transform.position;
                lastSeenTime = Time.time;
                CurrentState = Behaviour.Chasing;
            }
            else CurrentState = Time.time - lastSeenTime < 7f ? Behaviour.Searching : Behaviour.Patrol;

            if (CurrentState == Behaviour.Patrol)
            {
                speed = 2f;
                Vector3 offset = new Vector3(Mathf.Cos(patrolPoint * Mathf.PI * .5f), 0, Mathf.Sin(patrolPoint * Mathf.PI * .5f)) * 5;
                Vector3 goal = motor.Home + offset;
                if (Vector3.Distance(transform.position, goal) < 1.5f ||
                    (!motor.Agent.pathPending && motor.Agent.hasPath && motor.Agent.remainingDistance < 1f)) patrolPoint++;
                motor.MoveTo(goal, speed);
            }
            else
            {
                Vector3 desired = motor.Agent.steeringTarget - transform.position;
                desired.y = 0;
                float turn = Vector3.Angle(transform.forward, desired);
                float targetSpeed = turn > 25f ? 2f : maximumSpeed;
                speed = Mathf.MoveTowards(speed, targetSpeed, Time.deltaTime * (turn > 25f ? 18f : acceleration));
                motor.MoveTo(lastSeen, speed);
                if (CurrentState == Behaviour.Searching && Vector3.Distance(transform.position, lastSeen) < 1.3f) motor.Stop();
                motor.Attack(35);
            }
            motor.State = CurrentState + " / " + speed.ToString("0.0") + " m/s";
        }
    }
}
