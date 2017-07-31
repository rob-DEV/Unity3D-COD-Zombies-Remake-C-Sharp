using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Zombie : MonoBehaviour
{
    public Transform Player;

    public bool IsAlive = true;
    public float Health;
    public Transform[] hitPoints;
    public GameObject Ragdoll;

    private bool IsAtBarricade;
    private float barricadeTimer = 3;
    private Animator anim;
    private NavMeshAgent m_navMeshAgent;


    void Start()
    {
        Health = GameManager.instance.Health;
        m_navMeshAgent = GetComponent<NavMeshAgent>();
        if (GameManager.instance.round < 6)
            m_navMeshAgent.speed = Random.Range(0.5f, GameManager.instance.round * 1.2f);
        else
            m_navMeshAgent.speed = 5;

        Player = GameObject.FindGameObjectWithTag("Player").transform;

        anim = GetComponent<Animator>();
        //set a random walk
        anim.SetInteger("walkIndex", Random.Range(1, 3));

    }

    void Update()
    {
        Navigate();

        if (Health <= 0)
            Death();
    }

    void OnTriggerEnter(Collider col)
    {
        if (col.tag == "barricade" && !IsAtBarricade)
        {
            IsAtBarricade = true;
            anim.SetBool("atBarricade", true);
            //zombies needs to pick a random board to remove (if not already removed)
            StartCoroutine(BreakBoards(col.gameObject.GetComponent<Barricade>()));
        }
    }
    void Navigate()
    {
        //update animator
        anim.SetFloat("velocity", m_navMeshAgent.speed);
        anim.SetFloat("walkMultiplier", (1 + (m_navMeshAgent.speed)));
        if (m_navMeshAgent.enabled)
        {
            m_navMeshAgent.SetDestination(Player.position);
            
        }
    }
    IEnumerator BreakBoards(Barricade currentBarricade)
    {

        //stop all navigation;
        m_navMeshAgent.enabled = false;

        //tp zombie
        yield return new WaitForSeconds(0.5f);
        gameObject.transform.position = currentBarricade.nodePostion.position;
        gameObject.transform.rotation = currentBarricade.nodePostion.rotation;
        while (IsAlive && !currentBarricade.allBoardsRemoved)
        {
            //pick a random board
            int boardIndex = Random.Range(1, 6);
            //barricade is already removed pick a new one (should be quick enough only five options)
            if (currentBarricade.boards[boardIndex - 1].isRemoved)
            {
                continue;
            }
            else
            {
                //tell barricade to remove the board and player zombie animation
                anim.Play(string.Format("zomb_destroy_{0}", boardIndex));
                
                currentBarricade.RemoveBoard(boardIndex);
                //wait before picking the next one
                yield return new WaitForSeconds(barricadeTimer);
            }
        }

        //resume AI pathing
        m_navMeshAgent.enabled = true;
        anim.SetBool("atBarricade", false);
        //IsAtBarricade = false;
    }
    public void ApplyDamage(object[] args)
    {
        //infinite damage i.e. wonder weapon
        if ((float)args[0] < 0)
            Death();

        Health -= (float)args[0];
        //give player back score and kill bonus if needed
        Player player = (Player)args[1];
        if (Health > 0)       
            player.AddScore(10);
        //kill senario (i.e this bullet was the one which killed to zombie)
        if (Health <= 0)
        {
            player.AddScore(60);
            player.Kills++;
        }
    }
    private void Death()
    {
        //report the kill
        GameManager.instance.zombiesKilledInRound++;
        IsAlive = false;
        //instaniate ragdoll corpse and delete gameobject
        Instantiate(Ragdoll, gameObject.transform.position, gameObject.transform.rotation);
        Destroy(gameObject);
    }
}
