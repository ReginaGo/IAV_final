using UnityEngine;

namespace MonsterSandbox
{
    [RequireComponent(typeof(MonsterMotor))]
    public sealed class BrackenAI : MonoBehaviour
    {
        public enum Behaviour { Stalking, Retreating, Enraged }
        public Behaviour CurrentState { get; private set; }
        [SerializeField] private float stareToEnrage = 2.5f;
        private MonsterMotor motor;
        private float stare;
        private float retreatUntil;
        private float angryUntil;

        private void Awake() { motor = GetComponent<MonsterMotor>(); }
        public void ResetBehaviour() { stare = retreatUntil = angryUntil = 0; CurrentState = Behaviour.Stalking; }
        private void Update()
        {
            if (!motor.Ready) { motor.Stop(); return; }
            bool seen = motor.IsObserved();
            stare = seen ? stare + Time.deltaTime : Mathf.Max(0, stare - Time.deltaTime * .5f);
            if (stare >= stareToEnrage) { angryUntil = Time.time + 6f; stare = 0; }
            if (Time.time < angryUntil)
            {
                CurrentState = Behaviour.Enraged;
                motor.MoveTo(motor.player.transform.position, 6f);
                motor.Attack(100);
            }
            else if (seen || Time.time < retreatUntil)
            {
                CurrentState = Behaviour.Retreating;
                if (seen) retreatUntil = Time.time + 2.5f;
                Vector3 away = (transform.position - motor.player.transform.position).normalized;
                motor.MoveTo(transform.position + away * 6f, 4f);
            }
            else
            {
                CurrentState = Behaviour.Stalking;
                Vector3 behind = motor.player.transform.position - motor.player.transform.forward * 2f;
                motor.MoveTo(motor.Distance < motor.detectionRange ? behind : motor.Home, 2.6f);
                if (motor.Distance < motor.detectionRange) motor.Attack(100, 2.3f);
            }
            motor.State = CurrentState.ToString();
        }
    }
}
