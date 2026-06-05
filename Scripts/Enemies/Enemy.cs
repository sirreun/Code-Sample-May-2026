using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Enemy : NetworkBehaviour
{
    public bool CanBeDamaged = true;
    protected Damageable damageable;

    public bool AttacksPlayer = true;
    protected DamagingEntity damagingEntity;

    [Header("Movement")]
    public float Speed = 3f;
    public float Gravity = -9.8f;
    public float RotationSpeed = 40f;
    public int WanderingRange = 5;
    protected bool UsingNodes = true;
    public ANode CurrentNode;
    public List<ANode> CurrentPath;
    protected Transform trackingTransform;
    protected ANode destination;

    [Header("Vision")]
    public Transform EyeLevelTransform;
    public float visionDistance = 15f;
    public LayerMask visionMask; // TODO: Default: 

    [SerializeField] protected State currentState = State.Wandering;

    protected enum State
    {
        Wandering, // not yet aware of player
        Tracking, // aware of player
        Attacking, // engaging player
        Fleeing, // some condition scares the enemy (ex. low health)
        Custom // unique state for enemy (ex. for power tracking - locking on to energy signature)
    }

    #region /// Initializing ///
    void Awake()
    {
        AStarManager.SetEnemySpawns += SetSpawnPosition;
    }
    
    // Start is called before the first frame update
    void Start()
    {
        if (CanBeDamaged)
        {
            damageable = this.gameObject.GetComponent<Damageable>();

            if (damageable == null)
            {
                Debug.LogWarning("This enemy requries a Damageable component");
            }
        }

        if (AttacksPlayer)
        {
            damagingEntity = GetComponent<DamagingEntity>();

            if (damagingEntity == null)
            {
                Debug.LogWarning("This enemy requries a damaging entity component");
            }
        }

        EnemyStart();
    }

    protected virtual void SetSpawnPosition()
    {
        if (IsSpawned && !IsHost) return;

        if (AStarManager.instance.AllNodes.Count > 0)
        {
            CurrentNode = AStarManager.instance.AllNodes[(Random.Range(0, AStarManager.instance.AllNodes.Count - 1))];
            transform.position = CurrentNode.transform.position;
        }
        else
        {
            Debug.LogWarning("list of ANodes is empty");
        }
        
    }

    protected virtual void EnemyStart()
    {

    }
    #endregion

    // Update is called once per frame
    void Update()
    {
        if (!IsHost) return;

        UpdatingTarget();
        if (UsingNodes)
        {
            switch (currentState)
            {
                case State.Wandering:
                    DeterminePath();
                    break;
                case State.Tracking:
                    TransformToDestinationANode();
                    break;
                case State.Attacking:
                    TransformToDestinationANode();
                    break;
                case State.Custom:
                    CustomStateUpdate();
                    break;
                default:
                    break;
            }
        }
        EnemyUpdate();
    }

    protected virtual void CustomStateUpdate()
    {

    }

    protected virtual void EnemyUpdate()
    {

    }

    /// <summary>
    /// Checks if player has been spotted by enemy.
    /// </summary>
    /// <returns></returns>
    public virtual void UpdatingTarget()
    {
        Ray ray = new Ray(EyeLevelTransform.position, EyeLevelTransform.forward);

        RaycastHit hitInformation;

        //Debug.DrawRay(EyeLevelTransform.position, EyeLevelTransform.forward * visionDistance, Color.green);

        if (Physics.Raycast(ray, out hitInformation, visionDistance, visionMask))
        {
            if (hitInformation.collider.GetComponent<Player>() != null)
            {
                switch (currentState)
                {
                    case State.Attacking:
                    case State.Tracking:
                        break;
                    default:
                        //Debug.Log("Player found");
                        ChangeState(State.Tracking);
                        break;
                }
                
                trackingTransform = hitInformation.collider.gameObject.transform;

                // Get distance between player and enemy
                float distance = Vector3.Distance(transform.position, trackingTransform.position);
                
                if (AttacksPlayer)
                {
                    if (damagingEntity.AttackDistance >= distance)
                    {
                        //Debug.Log(">>>> Attacking Player <<<<");
                        if (currentState != State.Attacking)
                        {
                            ChangeState(State.Attacking, false);
                        }
                        
                        // attack // check if player still alive
                        bool playerDead = damagingEntity.Attack(hitInformation.collider.GetComponent<PlayerManager>());
                        if (playerDead)
                        {
                            //Debug.Log("Target eliminated");
                            ChangeState(State.Wandering);
                        }
                        
                    }
                }
            }
            else
            {
                switch (currentState)
                {
                    case State.Attacking:
                    case State.Tracking:
                        //Debug.Log("No longer tracking player");
                        ChangeState(State.Wandering);
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Returns a random path for the enemy in a wandering state.
    /// </summary>
    protected void DeterminePath()
    {
        if (CurrentPath.Count > WanderingRange)
        {
            CurrentPath.Clear();
        }

        if (CurrentPath.Count > 0)
        {
            int i = 0;
            Vector3 pathPosition = new Vector3(CurrentPath[i].transform.position.x, CurrentPath[i].transform.position.y, CurrentPath[i].transform.position.z);
            Vector3 movementDirection = pathPosition - transform.position;
            movementDirection.y = 0f;
            transform.position = Vector3.MoveTowards(transform.position, pathPosition, Speed * Time.deltaTime);
            if (movementDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(movementDirection.normalized), Time.deltaTime * RotationSpeed);
            }
            
            //Debug.Log("movement dir: " + movementDirection.normalized);


            if (Vector2.Distance(transform.position, CurrentPath[i].transform.position) < 0.1f)
            {
                CurrentNode = CurrentPath[i];
                CurrentPath.RemoveAt(i);
            }
        }
        else 
        {
            while (CurrentPath == null || CurrentPath.Count == 0 && AStarManager.instance.AllNodes.Count > 0)
            {
                CurrentPath = AStarManager.instance.DeterminePath(CurrentNode, AStarManager.instance.AllNodes[(Random.Range(0, AStarManager.instance.AllNodes.Count - 1))]); // TODO: pick a wandering range
            }
        }
    }

    /// <summary>
    /// Sets the enemy destination to ANode closest to trackingTransform.
    /// </summary>
    protected void TransformToDestinationANode()
    {
        if (trackingTransform)
        {
            ANode previousDestination = destination;
            destination = ClosestANode(trackingTransform.position);
            if (previousDestination != destination)
            {
                while (CurrentPath == null || CurrentPath.Count == 0 && AStarManager.instance.AllNodes.Count > 0)
                {
                    CurrentPath = AStarManager.instance.DeterminePath(CurrentNode, destination);
                }
            }
            else
            {
                //Debug.Log("Same destination: " + trackingTransform.position.ToString());
                if (CurrentPath.Count > 0)
                {
                    //Debug.Log(">>>same dest: path has length over 0<<<");
                    int i = 0;
                    Vector3 pathPosition = new Vector3(CurrentPath[i].transform.position.x, CurrentPath[i].transform.position.y, CurrentPath[i].transform.position.z);
                    Vector3 movementDirection = pathPosition - transform.position;
                    movementDirection.y = 0f;
                    transform.position = Vector3.MoveTowards(transform.position, pathPosition, Speed * Time.deltaTime);
                    //Debug.Log(">>> NEW POS: " + transform.position);
                    if (movementDirection != Vector3.zero)
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(movementDirection.normalized), Time.deltaTime * RotationSpeed);
                    }

                    if (Vector2.Distance(transform.position, CurrentPath[i].transform.position) < 0.1f)
                    {
                        //Debug.Log("reached next node");
                        CurrentNode = CurrentPath[i];
                        CurrentPath.RemoveAt(i);
                    }
                }
                else
                {
                    //Debug.Log("Same dest: path is empty, getting new route");
                    while (CurrentPath == null || CurrentPath.Count == 0 && AStarManager.instance.AllNodes.Count > 0)
                    {
                        CurrentPath = AStarManager.instance.DeterminePath(CurrentNode, destination);
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("no transform for tracking set, but is in state: tracking");
        }
    }

    protected ANode ClosestANode(Vector3 position)
    {
        float shortestDistance = -1;
        ANode output = null;
        foreach (ANode node in AStarManager.instance.AllNodes)
        {
            float newDistance = Vector3.Distance(node.transform.position, position);
            if (shortestDistance < 0 || newDistance < shortestDistance)
            {
                shortestDistance = newDistance;
                output = node;
            }
        }

        if (shortestDistance == -1)
        {
            Debug.LogError(" no shortest distance found, something has gone terribly wrong");
        }

        return output;
    }

    // Causes to state change:
    // Wandering: destination reached
    // Not Attacking: player noticed (priority is by default closet, which can change every 15ish secs?) (other enemies might have other priorities)
    // Tracking: close enought to player to attack
    // Determines the new state for the enemy
    protected virtual void ChangeState(State newState, bool clearPath = true)
    {
        //Debug.Log("Changing state  to :" +  newState);
        currentState = newState;

        if (clearPath)
        {
            CurrentPath.Clear();
        }
        
    }

    // Last death function to be added
    public virtual void OnDeath()
    {
        Destroy(gameObject);
    }
}
