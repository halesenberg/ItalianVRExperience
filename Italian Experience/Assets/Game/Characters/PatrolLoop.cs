using UnityEngine;

namespace Bellavalle.Characters
{
    public class PatrolLoop : MonoBehaviour
    {
        [Header("Movimento")]
        [SerializeField] float distance = 3f;
        [SerializeField] float speed = 1.2f;
        [SerializeField] float turnSpeed = 5f;

        Vector3 _startPos;
        Vector3 _pointA;
        Vector3 _pointB;
        bool _goingToB = true;
        bool _turning;
        Quaternion _targetRotation;

        void Start()
        {
            _startPos = transform.position;
            _pointA = _startPos;
            _pointB = _startPos + transform.forward * distance;
        }

        void Update()
        {
            if (_turning)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation, Time.deltaTime * turnSpeed);
                if (Quaternion.Angle(transform.rotation, _targetRotation) < 1f)
                {
                    transform.rotation = _targetRotation;
                    _turning = false;
                }
                return;
            }

            Vector3 target = _goingToB ? _pointB : _pointA;
            Vector3 next = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            next.y = _startPos.y;
            transform.position = next;

            if (Vector3.Distance(transform.position, target) < 0.05f)
            {
                _goingToB = !_goingToB;
                _targetRotation = transform.rotation * Quaternion.Euler(0, 180, 0);
                _turning = true;
            }
        }
    }
}