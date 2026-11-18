using UnityEngine;

namespace BallQuest
{
    /// <summary>Contact normals plus a short nonalloc probe; support may belong to any solid track layer.</summary>
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public sealed class BallGroundDetector : MonoBehaviour
    {
        public LayerMask groundLayers = ~0;
        [Range(0, 80)] public float maximumSlope = 58;
        [Min(.01f)] public float probeTolerance = .13f;
        public bool Grounded { get; private set; }
        public Vector3 Normal { get; private set; } = Vector3.up;
        public Vector3 Point { get; private set; }
        public Rigidbody SupportBody { get; private set; }
        public Collider SupportCollider { get; private set; }
        public Vector3 Up { get; set; } = Vector3.up;
        public Vector3 SupportVelocity => SupportBody ? SupportBody.GetPointVelocity(Point) : Vector3.zero;
        public float Radius { get; private set; } = .5f;
        readonly RaycastHit[] hits = new RaycastHit[16];
        Rigidbody body;
        SphereCollider sphere;
        float contactTime = -100;
        Vector3 contactNormal;
        Vector3 contactPoint;
        Rigidbody contactBody;
        Collider contactCollider;
        float ignoreUntil;

        void Awake()
        {
            body = GetComponent<Rigidbody>(); sphere = GetComponent<SphereCollider>();
            Vector3 scale = transform.lossyScale;
            Radius = sphere.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        }
        void FixedUpdate() => Sample();
        public void Sample()
        {
            Grounded = false; Normal = Up; SupportBody = null; SupportCollider = null;
            if (Time.time < ignoreUntil) return;
            float cosine = Mathf.Cos(maximumSlope * Mathf.Deg2Rad);
            float probeRadius = Radius * .82f;
            Vector3 start = body.worldCenterOfMass + Up * .04f;
            int count = Physics.SphereCastNonAlloc(start, probeRadius, -Up, hits,
                Radius - probeRadius + probeTolerance + .04f, groundLayers, QueryTriggerInteraction.Ignore);
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                if (!Valid(hit.collider) || Vector3.Dot(hit.normal, Up) < cosine || hit.distance >= nearest) continue;
                nearest = hit.distance; Grounded = true; Normal = hit.normal; Point = hit.point; SupportBody = hit.rigidbody; SupportCollider = hit.collider;
            }
            if (!Grounded && Time.fixedTime - contactTime <= Time.fixedDeltaTime * 1.5f && Vector3.Dot(contactNormal, Up) >= cosine)
            {
                Grounded = true; Normal = contactNormal; Point = contactPoint; SupportBody = contactBody; SupportCollider = contactCollider;
            }
        }
        bool Valid(Collider collider)
        {
            if (!collider || collider.isTrigger || collider.attachedRigidbody == body) return false;
            if ((groundLayers.value & (1 << collider.gameObject.layer)) == 0) return false;
            // A racing rival is not a driveable track, even during a brief ball pile-up.
            return !collider.attachedRigidbody || !collider.attachedRigidbody.GetComponent<BallController>();
        }
        void OnCollisionEnter(Collision collision) => Contacts(collision);
        void OnCollisionStay(Collision collision) => Contacts(collision);
        void Contacts(Collision collision)
        {
            if (!Valid(collision.collider)) return;
            float cosine = Mathf.Cos(maximumSlope * Mathf.Deg2Rad);
            for (int i = 0; i < collision.contactCount; i++)
            {
                var contact = collision.GetContact(i);
                if (Vector3.Dot(contact.normal, Up) < cosine) continue;
                contactTime = Time.fixedTime; contactNormal = contact.normal; contactPoint = contact.point; contactBody = collision.rigidbody; contactCollider = collision.collider;
                break;
            }
        }
        public void Ignore(float seconds)
        {
            ignoreUntil = Time.time + Mathf.Max(0, seconds); contactTime = -100;
            Grounded = false; Normal = Up; SupportBody = null; SupportCollider = null;
        }
        public void ResetSupport()
        {
            ignoreUntil = 0; contactTime = -100; Grounded = false; Normal = Up; SupportBody = null; SupportCollider = null;
        }
    }
}
