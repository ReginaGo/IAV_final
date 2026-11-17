using UnityEngine;

namespace MonsterSandbox
{
    [RequireComponent(typeof(MonsterMotor))]
    public sealed class CoilHeadAI : MonoBehaviour
    {
        private MonsterMotor motor;
        private bool alerted;
        public void ResetBehaviour() { alerted = false; }
        private void Awake() { motor = GetComponent<MonsterMotor>(); }
        private void Update()
        {
            if (!motor.Ready) { motor.Stop(); return; }
            // No reaction delay: stop both navigation and residual velocity in the observed frame.
            if (motor.IsObserved())
            {
                alerted = true;
                motor.State = "Frozen (observado)";
                motor.Stop();
                return;
            }
            alerted |= motor.SeesPlayer(360f);
            if (!alerted) { motor.State = "Idle"; motor.Stop(); return; }
            motor.State = "Chasing";
            motor.MoveTo(motor.player.transform.position, 9f);
            motor.Attack(40);
        }
    }
}
