using UnityEngine;
using UnityEngine.Events;

namespace Blockout.Discovery
{
    public class PrototypeMover : MonoBehaviour
    {
        [Tooltip("Moving child, with a collider. Keep the prefab root at scale one.")]
        public Transform MovingPart;
        [Tooltip("Local displacement from its starting position, in metres.")]
        public Vector3 Travel = new Vector3(0, 3, 0);
        [Min(.01f)] public float Speed = 1;
        [Tooltip("Metres/second squared. Zero uses constant speed; a low value gives heavy machinery a slow start and stop.")]
        [Min(0)] public float Acceleration;
        public bool Automatic;
        [Min(0)] public float EndPause = 2;
        public bool StartAtEnd;
        public UnityEvent OnArrivedAtStart = new UnityEvent();
        public UnityEvent OnArrivedAtEnd = new UnityEvent();
        public bool TargetEnd { get; private set; }
        public bool AtEnd => TargetEnd && arrived;
        Vector3 origin;
        bool arrived;
        float resume;
        float currentSpeed;
        void Awake()
        {
            if (!MovingPart) { enabled = false; return; }
            origin = MovingPart.localPosition;
            TargetEnd = StartAtEnd;
            if (StartAtEnd) MovingPart.localPosition = origin + Travel;
        }
        void Update()
        {
            if (arrived)
            {
                if (!Automatic || Time.time < resume) return;
                SetEnd(!TargetEnd);
            }
            var target = origin + (TargetEnd ? Travel : Vector3.zero);
            float distance = Vector3.Distance(MovingPart.localPosition, target);
            float desiredSpeed = Acceleration > 0 ? Mathf.Min(Speed, Mathf.Sqrt(2 * Acceleration * distance)) : Speed;
            currentSpeed = Acceleration > 0 ? Mathf.MoveTowards(currentSpeed, desiredSpeed, Acceleration * Time.deltaTime) : Speed;
            MovingPart.localPosition = Vector3.MoveTowards(MovingPart.localPosition, target, Mathf.Max(.001f, currentSpeed) * Time.deltaTime);
            if ((MovingPart.localPosition - target).sqrMagnitude > .000001f) return;
            MovingPart.localPosition = target; currentSpeed = 0;
            arrived = true; resume = Time.time + EndPause;
            if (TargetEnd) OnArrivedAtEnd.Invoke(); else OnArrivedAtStart.Invoke();
        }
        public void SetEnd(bool end) { TargetEnd = end; arrived = false; }
        public void Toggle() { SetEnd(!TargetEnd); }
        public void Open() { SetEnd(true); }
        public void Close() { SetEnd(false); }
        void OnDrawGizmosSelected()
        {
            if (!MovingPart) return;
            var from = Application.isPlaying ? origin : MovingPart.localPosition;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.TransformPoint(from), transform.TransformPoint(from + Travel));
            Gizmos.DrawWireCube(transform.TransformPoint(from + Travel), Vector3.one);
        }
    }
}
