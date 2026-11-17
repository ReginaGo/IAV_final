using UnityEngine;
using UnityEngine.InputSystem;

namespace MonsterSandbox
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class SandboxPlayer : MonoBehaviour
    {
        public Camera View;
        public MonsterMotor[] monsters;
        public bool IsDead => health <= 0;
        private CharacterController controller;
        private float health = 100;
        private float pitch;
        private float vertical;
        private Vector3 spawn;
        private Vector3[] enemySpawns;
        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            spawn = transform.position;
            enemySpawns = new Vector3[monsters.Length];
            for (int i = 0; i < monsters.Length; i++) enemySpawns[i] = monsters[i].transform.position;
        }
        private void Update()
        {
            var keys = Keyboard.current;
            var mouse = Mouse.current;
            if (keys == null || mouse == null) return;
            if (keys.escapeKey.wasPressedThisFrame) Unlock();
            if (mouse.leftButton.wasPressedThisFrame && !IsDead) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
            if (keys.rKey.wasPressedThisFrame) ResetSandbox();
            if (IsDead || Cursor.lockState != CursorLockMode.Locked) return;
            Vector2 look = mouse.delta.ReadValue() * .12f;
            transform.Rotate(0, look.x, 0);
            pitch = Mathf.Clamp(pitch - look.y, -85, 85);
            View.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            Vector2 move = new Vector2((keys.dKey.isPressed ? 1 : 0) - (keys.aKey.isPressed ? 1 : 0), (keys.wKey.isPressed ? 1 : 0) - (keys.sKey.isPressed ? 1 : 0));
            Vector3 direction = transform.TransformDirection(new Vector3(move.x, 0, move.y).normalized);
            if (controller.isGrounded) vertical = -2;
            vertical += Physics.gravity.y * Time.deltaTime;
            controller.Move((direction * (keys.leftShiftKey.isPressed ? 7 : 4.5f) + Vector3.up * vertical) * Time.deltaTime);
        }
        public void TakeDamage(float amount, string source)
        {
            health = Mathf.Max(0, health - amount);
            if (IsDead) Unlock();
        }
        private void ResetSandbox()
        {
            // Reset locally so the sandbox also works outside Build Settings.
            controller.enabled = false;
            transform.SetPositionAndRotation(spawn, Quaternion.identity);
            controller.enabled = true;
            health = 100; vertical = pitch = 0;
            View.transform.localRotation = Quaternion.identity;
            for (int i = 0; i < monsters.Length; i++)
            {
                var motor = monsters[i];
                motor.Agent.Warp(enemySpawns[i]);
                motor.Agent.ResetPath();
                motor.transform.rotation = Quaternion.identity;
                foreach (var ai in motor.GetComponents<MonoBehaviour>())
                    if (ai is BrackenAI || ai is CoilHeadAI || ai is ThumperAI) ai.SendMessage("ResetBehaviour", SendMessageOptions.DontRequireReceiver);
            }
        }
        private void Unlock() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        private void OnDisable() { Unlock(); }
    }
}
