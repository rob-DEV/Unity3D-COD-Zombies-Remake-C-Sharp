using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MysteryBox : MonoBehaviour {

    public string finalSelectedWeapon;
    public GameObject mysteryBox;
    public Transform weaponSpawnNode;

    public AudioClip mysteryBoxMain;
    public AudioClip mysteryBoxClose;

    private GameObject[] weaponModels;
    private Animator anim;
    private AudioSource audioSource;
    private bool boxOpened;
    private bool playerCanTakeWeapon = false;
    private bool takenWeapon = false;

    void Start()
    {
        weaponModels = GameObject.FindGameObjectWithTag("WeaponManager").GetComponent<WeaponList>().WeaponModels;
        anim = mysteryBox.GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
    }
    void OnTriggerStay(Collider col)
    {
        if (col.tag == "Player" && !boxOpened)
        {
            UI ui = col.GetComponent<UI>();
            ui.PromptText.text = "Press F to use the box - $950";

            Player player = col.GetComponent<Player>();

            if (Input.GetButton("Use") && player.GetScore() >= 950)
            {
                player.MinusScore(950);
                StartCoroutine(OpenMagicBox(player));
            }
        }else if (col.tag == "Player" && playerCanTakeWeapon)
        {
            UI ui = col.GetComponent<UI>();
            ui.PromptText.text = "Press F to take the Weapon";

            if (Input.GetButton("Use") && !takenWeapon)
            {
                takenWeapon = true;
                Player player = col.GetComponent<Player>();

                //give the player that weapon
                player.GiveWeapon(finalSelectedWeapon);

                CloseBox();
                StartCoroutine(ResetBox());
            }
        }
    }
    void OnTriggerExit(Collider col)
    {
        if (col.tag == "Player")
        {
            col.GetComponent<UI>().PromptText.text = "";
        }
    }

    IEnumerator OpenMagicBox(Player player)
    {

        OpenBox();

        //wait until the box is opened
        yield return new WaitForSeconds(anim.GetCurrentAnimatorStateInfo(0).length);

        //move the weapon spawn up overtime
        StartCoroutine(MoveWeaponNode());
        //Spool through all the weapon models
        GameObject randomWeapon;

        for (int i = 0; i < 25; i++)
        {
            bool finalWeaponGenerated = false;
            while (!finalWeaponGenerated)
            {
                randomWeapon = weaponModels[Random.Range(0, weaponModels.Length)];
                if (!player.HasWeapon(randomWeapon.name))
                {
                    randomWeapon = Instantiate(randomWeapon, weaponSpawnNode, false);
                    yield return new WaitForSeconds(0.15f);                   
                    finalWeaponGenerated = true;
                    if (i < 24)
                        Destroy(randomWeapon);
                    finalSelectedWeapon = randomWeapon.name.Replace("(Clone)", "");
                }
            }
        }

        //let the user take the weapon
        while (playerCanTakeWeapon)
        {
            yield return new WaitForEndOfFrame();
        }
        //close box
        CloseBox();
        ResetBox();
    }
    IEnumerator MoveWeaponNode()
    {
        //move upwards over 2 seconds and then back down
        float elapsedUpTime = 0; float timeToRise = 2.5f;
        while (elapsedUpTime < timeToRise)
        {
            weaponSpawnNode.localPosition = Vector3.Lerp(Vector3.zero, Vector3.up, (elapsedUpTime / timeToRise));
            elapsedUpTime += Time.deltaTime;
            yield return new WaitForEndOfFrame();

        }
        playerCanTakeWeapon = true;


        //let the weapon sit up for a while
        yield return new WaitForSeconds(3);

        float elapsedDownTime = 0; float timeToFall = 10;
        while (elapsedDownTime < timeToFall)
        {
            weaponSpawnNode.localPosition = Vector3.Lerp(Vector3.up, Vector3.zero, (elapsedDownTime / timeToFall));
            elapsedDownTime += Time.deltaTime;
            yield return new WaitForEndOfFrame();

        }

        playerCanTakeWeapon = false;
    }
    void OpenBox()
    {
        boxOpened = true;
        anim.Play("box_open");
        audioSource.PlayOneShot(mysteryBoxMain, 0.5f);
    }
    void CloseBox()
    {
        //tp weapon
        StopAllCoroutines();
        weaponSpawnNode.localPosition = Vector3.zero;
        anim.Play("box_close");
        audioSource.PlayOneShot(mysteryBoxClose, 0.2f);
        Destroy(weaponSpawnNode.GetChild(0).gameObject);
        StartCoroutine(ResetBox());
    }
    IEnumerator ResetBox()
    {
        yield return new WaitForSeconds(1);
        boxOpened = false;
        playerCanTakeWeapon = false;
        takenWeapon = false;
        finalSelectedWeapon = "";

    }

}
