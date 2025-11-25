using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Collider))]
public class EnemyController : MonoBehaviour
{
    public enum Mode { StareIdle, Wandering, Chase, Vanishing }

    [Header("Look behaviour")]
    public float turnSpeed = 3f;

    [Header("Chase behaviour")]
    public float moveSpeed = 3f;
    public float chaseDelay = 0.4f;

    [Header("Vanish behaviour")]
    public float vanishTime = 1.2f;

    [Header("Animation")]
    public Animator animator;
    // Bools used: "Stare", "Wandering", "Chase", "Attack"

    Transform player;
    NavMeshAgent agent;
    bool hasAgent;
    Mode mode;
    System.Action onDespawn;

    public void Init(Transform playerT, string spawnStyle, System.Action onDespawnCallback)
    {
        player = playerT;
        onDespawn = onDespawnCallback;

        agent = GetComponent<NavMeshAgent>();
        hasAgent = agent && agent.enabled;
        if (hasAgent)
        {
            agent.speed = moveSpeed;
            agent.stoppingDistance = 1f;
            agent.updateRotation = false; // we rotate manually
        }

        EnterStareMode();          // spawn always starts in stare (sitting / peeping / etc.)
        mode = Mode.StareIdle;

        // NOTE: spawnStyle (sit/peep/etc.) only affects *where* we spawn.
        // Anim-wise it's all the same Stare state; you control the variants in the Animator graph.
    }

    void EnterStareMode()
    {
        if (!animator) return;

        animator.SetBool("Stare", true);
        animator.SetBool("Wandering", false);
        animator.SetBool("Chase", false);
        animator.SetBool("Attack", false);
    }

    void EnterChaseMode()
    {
        if (!animator) return;

        animator.SetBool("Stare", false);
        animator.SetBool("Wandering", false);
        animator.SetBool("Chase", true);
        // Attack is handled separately when in range
    }

    void EnterWanderMode()
    {
        if (!animator) return;

        animator.SetBool("Stare", false);
        animator.SetBool("Wandering", true);
        animator.SetBool("Chase", false);
        animator.SetBool("Attack", false);
    }

    void TriggerAttack()
    {
        if (!animator) return;

        animator.SetBool("Attack", true);
        // You can transition back to Chase/Idle via exit time in the Animator
    }

    void Update()
    {
        if (!player) return;

        switch (mode)
        {
            case Mode.StareIdle:
                LookAtPlayer();
                break;

            case Mode.Wandering:
                // Optional: implement simple wander logic if you want later
                LookAtPlayer(); // or not, if you prefer
                break;

            case Mode.Chase:
                ChasePlayer();
                break;

            case Mode.Vanishing:
                // handled in coroutine
                break;
        }
    }

    void LookAtPlayer()
    {
        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * turnSpeed);
    }

    void ChasePlayer()
    {
        if (hasAgent)
        {
            agent.speed = moveSpeed;
            agent.SetDestination(player.position);
        }
        else
        {
            Vector3 dir = (player.position - transform.position).normalized;
            transform.position += dir * (moveSpeed * Time.deltaTime);
        }

        LookAtPlayer();
    }

    // Called by EnemyDirector when player enters a room
    public void OnRoomContextChanged(RoomMeta room)
    {
        if (!room) return;

        switch (room.kind)
        {
            case RoomMeta.RoomKind.Aggro:
                if (mode != Mode.Chase)
                    StartCoroutine(StartChaseAfterDelay());
                break;

            case RoomMeta.RoomKind.Safe:
                if (mode != Mode.Vanishing)
                    StartCoroutine(VanishAndDespawn());
                break;

            case RoomMeta.RoomKind.Normal:
            default:
                // Stay in current mode (normally StareIdle)
                break;
        }
    }

    System.Collections.IEnumerator StartChaseAfterDelay()
    {
        yield return new WaitForSeconds(chaseDelay);

        mode = Mode.Chase;
        EnterChaseMode();
    }

    System.Collections.IEnumerator VanishAndDespawn()
    {
        mode = Mode.Vanishing;

        // Optional: use Attack or a special vanish sub-state here if you want
        if (animator)
        {
            // For example, you could make an Animator state that plays vanish
            // when Stare/Chase/Wandering are all false and uses a state machine trigger.
            animator.SetBool("Stare", false);
            animator.SetBool("Wandering", false);
            animator.SetBool("Chase", false);
            animator.SetBool("Attack", false);
        }

        float t = 0f;
        Vector3 startPos = transform.position;

        while (t < vanishTime)
        {
            t += Time.deltaTime;
            transform.position = startPos + Vector3.down * (t / vanishTime * 0.3f);
            yield return null;
        }

        onDespawn?.Invoke();
        Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.transform == player)
        {
            // Enemy reached player = Attack animation
            TriggerAttack();

            // Call the lose state if single-player GameManager exists
            var gm = GameManagerSinglePlayer.Instance;
            if (gm != null)
            {
                gm.HandlePlayerCaught();
            }
            else
            {
                // Fallback if something is missing
                UnityEngine.SceneManagement.SceneManager.LoadScene("StartScene");
            }
        }
    }
}
