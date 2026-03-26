using UnityEngine;
using UnityEngine.AI;

public class BloodyCrowNav : MonoBehaviour
{
    public float speed;
    public float navFollowHeight;
    public GameObject navObject;
    public NavMeshAgent navAgent;

    private PlayerInput player;
    public CapsuleCollider hitBox;

    public bool followingNav = false;
    public Vector3 target;
    public float damage;
    private Rigidbody rb;
    private float distance;
    private float callCD = 0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        player = FindFirstObjectByType<PlayerInput>();
        navAgent.Warp(transform.position);
        navAgent.SetDestination(player.projectedPosition);
    }

    // Update is called once per frame
    void Update()
    {
        distance = (transform.position - player.transform.position).magnitude;

        if (CheckLOS())
        {
            if (followingNav)
            {
                followingNav = false;
            }
        }
        else
        {
            if (!followingNav)
            {
                followingNav = true;
                SwitchToNav();
            }
        }

        if (followingNav)
        {
            navAgent.SetDestination(player.projectedPosition);
            target = new Vector3(navAgent.steeringTarget.x, navAgent.steeringTarget.y, navAgent.steeringTarget.z);
            //target.y = transform.position.y + (player.transform.position.y - transform.position.y) * ((target - transform.position).magnitude / distance);

            //If target has been reached, manually set navmesh to its next position and recalculate path.
            if ((target - transform.position).magnitude < 0.5f)
            {
                GetNextPoint();
            }
        }
        else
        {
            target = player.transform.position;
        }

        rb.linearVelocity = (target-transform.position).normalized * speed;
    }

    private void SwitchToNav()
    {
        NavMeshHit hit;
        NavMesh.Raycast(transform.position, new Vector3(transform.position.x, -1, transform.position.z), out hit, NavMesh.AllAreas);
        if (hit.hit)
        {
            navAgent.Warp(hit.position);
        }
        else
        {
            navAgent.Warp(transform.position);
        }
            navAgent.SetDestination(player.projectedPosition);
    }

    private bool CheckLOS()
    {
        if (player == null) return false;
        bool result = Physics.BoxCast(transform.position, new Vector3(0.3f, 0.3f, 0.3f), player.transform.position - transform.position,
                                Quaternion.identity, (player.transform.position - transform.position).magnitude, LayerMask.GetMask("Default", "Terrain"));
        return !result;
    }

    private void GetNextPoint()
    {
        if (callCD <= 0)
        {
            navAgent.Warp(navAgent.steeringTarget);
            callCD = 0.1f;
        }
        else
        {
            callCD -= Time.deltaTime;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == 3)
        {
            IDamageable damageable = other.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage, DamageType.Melee);
                Destroy(this.transform.parent.gameObject);
            }
        }
    }
}
