using UnityEngine;

public class GameManager : MonoBehaviour {

    public static GameManager instance = null;

    public bool Active;

    public Transform[] zombieSpawnPoints;
    public GameObject[] zombiePrefabs;

    public float Health;
    private float oldHealth;

    public int round;
    public float RoundCountdown;
    public int zombiesInRound;
    public int zombiesSpawnedInRound;
    public int zombiesKilledInRound;
    public float zombieSpawnDelay = 2;
    private float roundCountdown;

    void Awake()
    {
        if (instance == null)
            instance = this;
        else if (instance != this)
            Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
    }

	void Start ()
    {
        if (zombieSpawnPoints.Length < 1)
        {
            Debug.LogWarning("You need Spawnpoints for zombies to spawn");
            return;
        }
        NewRound();
	}
	
	void Update () {
        if (zombieSpawnPoints.Length > 0)
        {
            //max of 24 zeds at one time
            if ((zombiesSpawnedInRound - zombiesKilledInRound < 24) && zombiesSpawnedInRound < zombiesInRound && roundCountdown == 0)
            {
                if (zombieSpawnDelay > 3)//Dont over crowd the scene with Zombies
                {
                    SpawnZombie();
                    zombieSpawnDelay = 0;
                }
                else
                {
                    zombieSpawnDelay += Time.deltaTime;
                }
            }
            else if (zombiesKilledInRound == zombiesInRound)
            {
                NewRound();
            }
            if (roundCountdown > 0)
                roundCountdown -= Time.deltaTime;
            else
                roundCountdown = 0; 
        }
    }

    void NewRound()
    {
        ++round;
        Health = 150;
        //set health based on round
        if (round > 1 && round < 10)
        {
            Health = 150 + (100 * (round - 1));
            oldHealth = Health;
        }
        else if(round > 9)
        {
            Health = oldHealth * 1.1f;
        }

        //reset variables and begin round
        if (round > 0)
            roundCountdown = RoundCountdown;
        else
            RoundCountdown = 0;

        zombiesSpawnedInRound = 0;
        zombiesKilledInRound = 0;
        zombiesInRound = 6 * round;
    }
    void SpawnZombie()
    {
        Vector3 randomSpawnPoint = zombieSpawnPoints[Random.Range(0, zombieSpawnPoints.Length)].position;
        GameObject randomzombie = zombiePrefabs[Random.Range(0, zombiePrefabs.Length)];
        Instantiate(randomzombie, randomSpawnPoint, Quaternion.identity);
        zombiesSpawnedInRound++;
    }
}
