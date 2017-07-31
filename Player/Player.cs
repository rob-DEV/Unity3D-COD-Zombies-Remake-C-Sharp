using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Utility;
using Random = UnityEngine.Random;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
public class Player : MonoBehaviour
{
    #region StandardFPS
    [SerializeField] private bool m_IsWalking;
    [SerializeField] private float m_WalkSpeed;
    private float m_WalkSpeedStore;
    [SerializeField] private float m_RunSpeed;
    [SerializeField] [Range(0f, 1f)] private float m_RunstepLenghten;
    [SerializeField] private float m_JumpSpeed;
    [SerializeField] private float m_StickToGroundForce;
    [SerializeField] private float m_GravityMultiplier;
    [SerializeField] private UnityStandardAssets.Characters.FirstPerson.MouseLook m_MouseLook;
    [SerializeField] private bool m_UseFovKick;
    [SerializeField] private FOVKick m_FovKick = new FOVKick();
    [SerializeField] private bool m_UseHeadBob;
    [SerializeField] private CurveControlledBob m_HeadBob = new CurveControlledBob();
    [SerializeField] private LerpControlledBob m_JumpBob = new LerpControlledBob();
    [SerializeField] private float m_StepInterval;
    [SerializeField] private AudioClip[] m_FootstepSounds;    // an array of footstep sounds that will be randomly selected from.
    [SerializeField] private AudioClip m_JumpSound;           // the sound played when character leaves the ground.
    [SerializeField] private AudioClip m_LandSound;           // the sound played when character touches back on ground.

    private Camera m_Camera;
    private bool m_Jump;
    private float m_YRotation;
    private Vector2 m_Input;
    private Vector3 m_MoveDir = Vector3.zero;
    private CharacterController m_CharacterController;
    private CollisionFlags m_CollisionFlags;
    private bool m_PreviouslyGrounded;
    private Vector3 m_OriginalCameraPosition;
    private float m_StepCycle;
    private float m_NextStep;
    private bool m_Jumping;
    private AudioSource m_AudioSource;

    #endregion

    #region Player Variables
    //Player
    public int PlayerHealth;
    public int PlayerScore;
    public int Kills;
    private Animator anim;
    private UI ui;
    private MovementState PlayerMovementState = MovementState.Idle;

    //Weapons
    [SerializeField] private int PrimaryWeaponIndex = 0;
    [SerializeField] private int SecondaryWeaponIndex = 1;
    [SerializeField] private Weapon CurrentWeapon;
    [SerializeField] private List<Weapon> PlayerWeapons;
    [SerializeField] private List<Weapon> AllWeapons;
    public WeaponList GlobalWeaponList;
    [SerializeField] private Transform ADSHolder;
    [SerializeField] private Transform RecoilHolder;
    private AudioSource weaponAudioSource;

    [Header("Bullet")]
    [SerializeField]
    private Transform BulletInstantiationPoint;
    [SerializeField]
    private GameObject PointHitCube;
    [SerializeField]
    private LayerMask IgnoredLayers;

    [Header("Perks")]
    public PerkManagement Perks;

    [Header("Equipment")]
    public EquipmentManager Equipment;

    public Animator LegRigAnimator;

    //Miscellaneous
    public bool IsShooting;
    public bool CanShoot;
    private bool IsSwitching;
    private bool IsAiming;
    public bool IsReloading;
    private float shootTime = 0;

    public int curWeap;
    private float switchTime = 0;

    private Vector3 CurrentRecoil1;
    private Vector3 CurrentRecoil2;
    private Vector3 CurrentRecoil3;
    private Vector3 CurrentRecoil4;
    #endregion

    private void Start()
    {
        #region StandardFPS
        m_CharacterController = GetComponent<CharacterController>();
        m_Camera = Camera.main;
        m_OriginalCameraPosition = m_Camera.transform.localPosition;
        m_FovKick.Setup(m_Camera);
        m_HeadBob.Setup(m_Camera, m_StepInterval);
        m_StepCycle = 0f;
        m_NextStep = m_StepCycle / 2f;
        m_Jumping = false;
        m_AudioSource = GetComponent<AudioSource>();
        m_MouseLook.Init(transform, m_Camera.transform);
        m_WalkSpeedStore = m_WalkSpeed;
        #endregion
        PlayerInit();
    }
    private void Update()
    {
        //Network Local Check
        if (!ClientNetworkManager.instance.isLocalPlayer)
            return;

        #region StandardFPS
        RotateView();
        // the jump state needs to read here to make sure it is not missed
        if (!m_Jump)
        {
            m_Jump = CrossPlatformInputManager.GetButtonDown("Jump");
        }

        if (!m_PreviouslyGrounded && m_CharacterController.isGrounded)
        {
            StartCoroutine(m_JumpBob.DoBobCycle());
            PlayLandingSound();
            m_MoveDir.y = 0f;
            m_Jumping = false;
        }
        if (!m_CharacterController.isGrounded && !m_Jumping && m_PreviouslyGrounded)
        {
            m_MoveDir.y = 0f;
        }

        m_PreviouslyGrounded = m_CharacterController.isGrounded;
        #endregion
        //Call these functions every frame
        WalkingStateController();
        WeaponController();
        ADSController();

        AnimationController();
        RecoilController();
        ReloadController();
        ShootController();
        UIController();
        PerkController();
    }
    private void FixedUpdate()
    {
        #region StandardFPS
        float speed;
        GetInput(out speed);
        // always move along the camera forward as it is the direction that it being aimed at
        Vector3 desiredMove = transform.forward * m_Input.y + transform.right * m_Input.x;

        // get a normal for the surface that is being touched to move along it
        RaycastHit hitInfo;
        Physics.SphereCast(transform.position, m_CharacterController.radius, Vector3.down, out hitInfo,
                           m_CharacterController.height / 2f, Physics.AllLayers, QueryTriggerInteraction.Ignore);
        desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;

        m_MoveDir.x = desiredMove.x * speed;
        m_MoveDir.z = desiredMove.z * speed;


        if (m_CharacterController.isGrounded)
        {
            m_MoveDir.y = -m_StickToGroundForce;

            if (m_Jump)
            {
                m_MoveDir.y = m_JumpSpeed;
                PlayJumpSound();
                m_Jump = false;
                m_Jumping = true;
            }
        }
        else
        {
            m_MoveDir += Physics.gravity * m_GravityMultiplier * Time.fixedDeltaTime;
        }
        m_CollisionFlags = m_CharacterController.Move(m_MoveDir * Time.fixedDeltaTime);

        ProgressStepCycle(speed);
        UpdateCameraPosition(speed);

        m_MouseLook.UpdateCursorLock();

        #endregion

    }
    private void PlayerInit()
    {
        ui = GetComponent<UI>();
        SetupWeapons();
    }
    private void SetupWeapons()
    {
        GlobalWeaponList = GameObject.FindGameObjectWithTag("WeaponManager").GetComponent<WeaponList>();
        //deactivate all equipment and perk hands
        weaponAudioSource = ADSHolder.GetComponent<AudioSource>();
        Perks.PerkHands.gameObject.SetActive(false);
        Equipment.KnifeHands.gameObject.SetActive(false);

        foreach (Weapon weap in GlobalWeaponList.AllWeapons)
        {
            weap.Clip = weap.ClipSize;
            --weap.Clips;
            //make transform invisible
            weap.WeaponTransform.gameObject.SetActive(false);
        }
        //pick out the starting weapon from the entire lost of playable weapons
        CurrentWeapon = GlobalWeaponList.AllWeapons[PrimaryWeaponIndex];
        CurrentWeapon.WeaponTransform.gameObject.SetActive(true);
        anim = CurrentWeapon.WeaponTransform.GetComponent<Animator>();

        //assign to playerWeapons
        PlayerWeapons.Add(CurrentWeapon);

        //assign default secondary to the next weapon in the list (-1 means we don't wan't to start with a secondary 
        if(SecondaryWeaponIndex != -1)
            PlayerWeapons.Add(GlobalWeaponList.AllWeapons[SecondaryWeaponIndex]);

        CanShoot = true;
    }
    private void WeaponController()
    {
        ///TOCO:
        ///Handle pickups??
        ///Current and secondary weapon
        ///
        //If not knifing or using equipment
        /*TEST set when switching weapon*/
        foreach (var weapon in PlayerWeapons)
        {
            if (weapon != CurrentWeapon)
                //hide the weapon which isn't equiped
                weapon.WeaponTransform.gameObject.SetActive(false);
        }

        //switching between PlayerWeapons
        if (Input.GetAxis("SwitchWeapon") < 0 || Input.GetAxis("SwitchWeapon") > 0 && PlayerMovementState != MovementState.Running)
        {
            if (switchTime < Time.time && PlayerWeapons.Count > 1)
            {
                curWeap++;
                //where 2 is the number of weapons - change when player has mulekick
                if (curWeap >= 2)
                    curWeap = 0;
                switchTime = Time.time + 1;

                StartCoroutine(Switch(true));

            }
        }

        if (Input.GetKeyDown(KeyCode.V) && !IsSwitching)
        {
            StartCoroutine(Switch(false, "knife"));
        }
    }
    private void AnimationController()
    {
        ///TODO:
        ///Check movement states and adjust animator accordingly
        //Get current weapons Animator
        anim = CurrentWeapon.WeaponTransform.GetComponent<Animator>();

        if (PlayerMovementState == MovementState.Idle)
            anim.SetBool("IsIdle", true);
        else
            anim.SetBool("IsIdle", false);
        if (PlayerMovementState == MovementState.Walking)
            anim.SetBool("IsWalking", true);
        else
            anim.SetBool("IsWalking", false);
        if (PlayerMovementState == MovementState.Running)
            anim.SetBool("IsRunning", true);
        else
            anim.SetBool("IsRunning", false);

        //Shooting
        if (IsShooting && CanShoot && !IsReloading)
            anim.Play(string.Format("{0}_shoot", CurrentWeapon.Name));


        //manager gun postition if the weapon has an empty position
        int emptyWeaponIndex = anim.GetLayerIndex("EmptyWeapon");
        if (emptyWeaponIndex != -1)
            if (CurrentWeapon.Clip == 0 && CurrentWeapon.Clips == 0)
                anim.SetLayerWeight(emptyWeaponIndex, 1);
            else
                anim.SetLayerWeight(emptyWeaponIndex, 0);
    }
    private void RecoilController()
    {
        CurrentRecoil1 = Vector3.Lerp(CurrentRecoil1, Vector3.zero, 0.3f);
        CurrentRecoil2 = Vector3.Lerp(CurrentRecoil2, CurrentRecoil1, 0.3f);
        CurrentRecoil3 = Vector3.Lerp(CurrentRecoil3, Vector3.zero, 0.3f);
        CurrentRecoil4 = Vector3.Lerp(CurrentRecoil4, CurrentRecoil3, 0.3f);

        RecoilHolder.localEulerAngles = CurrentRecoil2;
        RecoilHolder.localPosition = CurrentRecoil4;
        //BulletInstantiationPoint.localEulerAngles = CurrentRecoil2;
        //BulletInstantiationPoint.localPosition = CurrentRecoil4;
    }
    private void UIController()
    {
        ui.ScoreText.text = string.Format("Score: {0}", PlayerScore);
        ui.WeaponNameText.text = CurrentWeapon.Name;
        ui.AmmoText.text = string.Format("{0}/{1}", CurrentWeapon.Clip, CurrentWeapon.ClipSize * CurrentWeapon.Clips);
        if (GameManager.instance) {
            ui.RoundText.text = GameManager.instance.round.ToString();
        }
    
    }
    private void PerkController()
    {
        if (Perks.hasJugg && !Perks.hasDrankJugg)
        {
            //needs to drink perk
            Perks.hasDrankJugg = true;
            StartCoroutine(Switch(false, "jugg"));
        }
        /*
         * if (Perks.hasJugg && !Perks.hasDrankJugg)
        {
            //needs to drink perk
        }
        */

        //manager player variables based on perks
        if (Perks.hasJugg && !Perks.setJugg)
        {
            PlayerHealth = 200;
            Perks.setJugg = true;
        }
    }

    //Input Controllers
    private void WalkingStateController()
    {
        if (Input.GetAxis("Horizontal") != 0 ||Input.GetAxis("Vertical") != 0 && m_CharacterController.velocity.magnitude > 0.2f)
        {
            if (Input.GetButton("Run") && Input.GetAxis("Vertical") > 0)
            {
                PlayerMovementState = MovementState.Running;
                LegRigAnimator.SetBool("IsRunning", true);

                LegRigAnimator.SetBool("IsIdle", false);
                LegRigAnimator.SetBool("IsWalking", false);
            }
            else
            {
                PlayerMovementState = MovementState.Walking;
                LegRigAnimator.SetBool("IsWalking", true);

                LegRigAnimator.SetBool("IsIdle", false);
                LegRigAnimator.SetBool("IsRunning", false);
            }

            if (!Input.GetButton("Run") && IsAiming)
            {
                PlayerMovementState = MovementState.Idle;
                LegRigAnimator.SetBool("IsIdle", true);

                LegRigAnimator.SetBool("IsRunning", false);
                LegRigAnimator.SetBool("IsWalking", false);
            }
        }
        else
        {
            PlayerMovementState = MovementState.Idle;
            LegRigAnimator.SetBool("IsIdle", true);

            LegRigAnimator.SetBool("IsRunning", false);
            LegRigAnimator.SetBool("IsWalking", false);
        }
        
    }
    private void ShootController()
    {
        if (CanShoot && !IsReloading)
        {
            //Decide input type i.e trigger or trigger held down
            bool weaponInputType = false;
            if (CurrentWeapon.WeaponType == WeaponType.Auto)
                weaponInputType = Input.GetButton("Fire1");
            if (CurrentWeapon.WeaponType == WeaponType.SemiAuto)
                weaponInputType = Input.GetButtonDown("Fire1");


            if (shootTime <= Time.time)
            {

                if (weaponInputType && CurrentWeapon.Clip > 0)
                {
                    if (PlayerMovementState != MovementState.Running && !IsReloading)
                    {

                        //Add time until next shot
                        shootTime = Time.time + CurrentWeapon.Firerate;
                        //Subtract a bullet
                        CurrentWeapon.Clip--;

                        if (IsAiming == false)
                        {
                            CurrentRecoil1 += new Vector3(CurrentWeapon.RecoilRotation.x, Random.Range(-CurrentWeapon.RecoilRotation.y, CurrentWeapon.RecoilRotation.y));
                            CurrentRecoil3 += new Vector3(Random.Range(-CurrentWeapon.RecoilKickback.x, CurrentWeapon.RecoilKickback.x), Random.Range(-CurrentWeapon.RecoilKickback.y, CurrentWeapon.RecoilKickback.y), CurrentWeapon.RecoilKickback.z);
                        }
                        else
                        {
                            //Reduce recoil if ADS
                            CurrentRecoil1 += new Vector3(CurrentWeapon.RecoilRotation.x, Random.Range(-CurrentWeapon.RecoilRotation.y, CurrentWeapon.RecoilRotation.y) / 2);
                            CurrentRecoil3 += new Vector3(Random.Range(-CurrentWeapon.RecoilKickback.x, CurrentWeapon.RecoilKickback.x), Random.Range(-CurrentWeapon.RecoilKickback.y, CurrentWeapon.RecoilKickback.y), CurrentWeapon.RecoilKickback.z) / 2;
                        }

                        if (CurrentWeapon.ShootSounds.Length > 0)
                            weaponAudioSource.PlayOneShot(CurrentWeapon.ShootSounds[Random.Range(0, CurrentWeapon.ShootSounds.Length)], 0.5f);

                        IsShooting = true;

                        //Raycast shoot reducing damage with each hit and range??
                        //if the ray hits a collider which is untagged assume it is rock and ingore subsequent hits
                        //if tag is = wood, thin, plaster etc allow bullet penetration
                        Vector3 rayOrigin = m_Camera.ViewportToWorldPoint(new Vector3(.5f, .5f, 0));
                        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, m_Camera.transform.forward, CurrentWeapon.Range, IgnoredLayers);
                        int zombieHitCount = 0;
                        foreach (RaycastHit hit in hits)
                        {
                            //Debug.Log(hit.collider.gameObject.name);
                            //Instantiate(PointHitCube, hit.point, hit.transform.rotation);
                            if (hit.transform.gameObject.tag == "zombie" || hit.transform.gameObject.tag == "enemy")
                            {
                                zombieHitCount++;
                                if (zombieHitCount < 3)
                                {
                                    float damage = CurrentWeapon.Damage / zombieHitCount;
                                    hit.collider.SendMessageUpwards("HitApplyDamage", new object[] { damage, this });
                                }
                            }
                            //add some force to object hit
                            if (hit.rigidbody != null)
                                hit.rigidbody.AddForce((hit.point * CurrentWeapon.Damage) / 1000, ForceMode.Impulse);
                        }
                    }

                }
            }
            else if (Input.GetButton("Fire1") && CurrentWeapon.Clip < 1)
            {
                //clip is empty
                weaponAudioSource.PlayOneShot(GlobalWeaponList.emptyClip, 0.5f);
                IsShooting = false;
            }
            else
            {
                //This flag is 100% needed
                IsShooting = false;
            }
        }
    }
    private void ADSController()
    {
        if (Input.GetButton("Fire2") && PlayerMovementState != MovementState.Running && !IsReloading)
        {
            IsAiming = true;
            m_WalkSpeed = m_WalkSpeedStore / 2;
            ADSHolder.localPosition = Vector3.Lerp(ADSHolder.localPosition, CurrentWeapon.Scopes[CurrentWeapon.CurrentScope].AdsPosition, 0.25f);
            ADSHolder.localRotation = Quaternion.Euler(CurrentWeapon.Scopes[CurrentWeapon.CurrentScope].AdsRotation);
            m_Camera.fieldOfView = Mathf.Lerp(m_Camera.fieldOfView, CurrentWeapon.Scopes[CurrentWeapon.CurrentScope].FOV, 0.25f);
        }
        else
        {
            IsAiming = false;
            m_WalkSpeed = m_WalkSpeedStore;
            ADSHolder.localPosition = Vector3.Lerp(ADSHolder.localPosition, Vector3.zero, 0.25f);
            ADSHolder.localRotation = Quaternion.Euler(0,0,0);
            m_Camera.fieldOfView = Mathf.Lerp(m_Camera.fieldOfView, 60, 0.25f);
        }
    }
    private void ReloadController()
    {
        if (((Input.GetButtonUp("Reload") && CurrentWeapon.Clip < CurrentWeapon.ClipSize) || (CurrentWeapon.Clip <= 0)) && !IsReloading)
        {
            if (CurrentWeapon.Clips > 0 && PlayerMovementState != MovementState.Running)
            {
                //User reloads themselves of auto-reload if the clip is empty - check if clips remaining is > 0
                StartCoroutine(Reload());
                if (CurrentWeapon.ReloadSound != null)
                    weaponAudioSource.PlayOneShot(CurrentWeapon.ReloadSound);

            }
            else if (CurrentWeapon.Clips == 0)
            {
                //All gone
                return;
            }
        }
    }
    IEnumerator Reload()
    {
        CanShoot = false; IsShooting = false;
        IsReloading = true;
        Animator anim = CurrentWeapon.WeaponTransform.GetComponent<Animator>();
        anim.Play(string.Format("{0}_reload", CurrentWeapon.Name));
        yield return new WaitForSeconds(anim.GetCurrentAnimatorStateInfo(0).length);

        while (anim.GetCurrentAnimatorStateInfo(0).IsName(string.Format("{0}_reload", CurrentWeapon.Name)))
        {
            yield return new WaitForSeconds(0.01f);
        }
        CurrentWeapon.Clip = CurrentWeapon.ClipSize;
        CurrentWeapon.Clips--;
        IsReloading = false;
        CanShoot = true;
    }
    IEnumerator Switch(bool isWeapon = true, string equipName = "")
    {
        if (isWeapon)
        {
            IsSwitching = true;
            //switching weapons
            Animator anim = CurrentWeapon.WeaponTransform.GetComponent<Animator>();

            anim.Play(string.Format("{0}_putaway", CurrentWeapon.Name));
            yield return new WaitForSeconds(anim.GetCurrentAnimatorStateInfo(0).length);

            //set weapon and get the new animator
            CurrentWeapon = PlayerWeapons[curWeap]; CurrentWeapon.WeaponTransform.gameObject.SetActive(true);
            anim = CurrentWeapon.WeaponTransform.GetComponent<Animator>();
            anim.Play(string.Format("{0}_retrieve", CurrentWeapon.Name));
            yield return new WaitForSeconds(anim.GetCurrentAnimatorStateInfo(0).length);
            IsShooting = false;
            anim.Play(string.Format("{0}_idle", CurrentWeapon.Name), 0);

            IsSwitching = false;
        }
        else if(equipName == "knife")
        {
            IsSwitching = true;

            Animator anim = CurrentWeapon.WeaponTransform.GetComponent<Animator>();
            CurrentWeapon.WeaponTransform.gameObject.SetActive(false);
            Equipment.KnifeHands.gameObject.SetActive(true);

            Vector3 rayOrigin = m_Camera.ViewportToWorldPoint(new Vector3(.5f, .5f, 0));
            Debug.DrawRay(rayOrigin, Vector3.forward, Color.red);
            RaycastHit hit;
            if (Physics.Raycast(rayOrigin, m_Camera.transform.forward, out hit, 2, IgnoredLayers))
            {
                if(hit.collider.tag == "zombie" || hit.collider.tag == "enemy")
                {
                    float knifeDamage = 150;
                    hit.collider.SendMessageUpwards("ApplyDamage", new object[] { knifeDamage, this });
                }
            }

            yield return new WaitForSeconds(.44f);
            Equipment.KnifeHands.gameObject.SetActive(false);
            CurrentWeapon.WeaponTransform.gameObject.SetActive(true);
            anim.Play(string.Format("{0}_retrieve", CurrentWeapon.Name));
            yield return new WaitForSeconds(anim.GetCurrentAnimatorStateInfo(0).length);
            anim.Play(string.Format("{0}_idle", CurrentWeapon.Name));
            IsSwitching = false;
        }
        else
        {
            IsSwitching = true;
            //i.e equipment or perk
            GameObject perkBottleToInstantiate = null;
            if (equipName == "jugg")
            {
                perkBottleToInstantiate = Perks.juggBottle;
                perkBottleToInstantiate.layer = 8;
            }

            Animator anim = CurrentWeapon.WeaponTransform.GetComponent<Animator>();
            anim.Play(string.Format("{0}_putaway", CurrentWeapon.Name));
            yield return new WaitForSeconds(anim.GetCurrentAnimatorStateInfo(0).length);

            //instantiate bottle and  drink perk anim
            GameObject perkBottle = Instantiate(perkBottleToInstantiate, Perks.PerkBottleNode);

            Perks.PerkHands.gameObject.SetActive(true);
            Perks.PerkHands.GetComponent<Animator>().Play("perk_bottle_drink");

            //hide lid
            yield return new WaitForSeconds(.3f);
            perkBottleToInstantiate.transform.Find("lid").gameObject.SetActive(false);

            yield return new WaitForSeconds(Perks.PerkHands.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).length + 1.5f);

            //clean up gameobjects
            Destroy(perkBottle);

            Perks.PerkHands.gameObject.SetActive(false);
            //bring weapon back
            anim.Play(string.Format("{0}_retrieve", CurrentWeapon.Name));
            yield return new WaitForSeconds(anim.GetCurrentAnimatorStateInfo(0).length);
            IsSwitching = false;
            anim.Play(string.Format("{0}_idle", CurrentWeapon.Name), 0);    
        }
    }
    public int GetScore()
    {
        return PlayerScore;
    }
    public void AddScore(int score)
    {
        PlayerScore += score;
    }
    public void MinusScore(int score)
    {
        PlayerScore -= score;
    }
    public bool HasWeapon(string weaponName)
    {
        if (PlayerWeapons[0].Name == weaponName)
            return true;
        if (PlayerWeapons.Count > 1)
            if (PlayerWeapons[1].Name == weaponName)
                return true;

        return false;
    }
    public void GiveWeapon(string weaponName)
    {
        if(weaponName != "")
        {

            //replace current weapon with new weapon if the player has more than one weapon
            if (PlayerWeapons.Count == 2)
            {
                ///////PLAYER WEAPONS
                if (PlayerWeapons[0].Name == CurrentWeapon.Name)
                {
                    CurrentWeapon.WeaponTransform.gameObject.SetActive(false);
                    PlayerWeapons[0] = (Weapon)GlobalWeaponList.AllWeapons.Find(x => x.Name == weaponName).Clone();
                    CurrentWeapon = PlayerWeapons[0];
                }
                if (PlayerWeapons[1].Name == CurrentWeapon.Name)
                {
                    CurrentWeapon.WeaponTransform.gameObject.SetActive(false);
                    PlayerWeapons[1] = (Weapon)GlobalWeaponList.AllWeapons.Find(x => x.Name == weaponName).Clone();
                    CurrentWeapon = PlayerWeapons[1];
                }

                CurrentWeapon.WeaponTransform.gameObject.SetActive(true);
            }
            else if(PlayerWeapons.Count == 1)
            {
                //PLAYER WEAPONS
                PlayerWeapons.Add((Weapon)GlobalWeaponList.AllWeapons.Find(x => x.Name == weaponName).Clone());
                curWeap++;
                StartCoroutine(Switch(true));
            }
        }
    }
    #region StandardFPS
    private void PlayJumpSound()
    {
        m_AudioSource.clip = m_JumpSound;
        m_AudioSource.Play();
    }
    private void PlayLandingSound()
    {
        m_AudioSource.clip = m_LandSound;
        m_AudioSource.Play();
        m_NextStep = m_StepCycle + .5f;
    }
    private void ProgressStepCycle(float speed)
    {
        if (m_CharacterController.velocity.sqrMagnitude > 0 && (m_Input.x != 0 || m_Input.y != 0))
        {
            m_StepCycle += (m_CharacterController.velocity.magnitude + (speed * (m_IsWalking ? 1f : m_RunstepLenghten))) *
                         Time.fixedDeltaTime;
        }

        if (!(m_StepCycle > m_NextStep))
        {
            return;
        }

        m_NextStep = m_StepCycle + m_StepInterval;

        PlayFootStepAudio();
    }
    private void PlayFootStepAudio()
    {
        if (!m_CharacterController.isGrounded)
        {
            return;
        }
        // pick & play a random footstep sound from the array,
        // excluding sound at index 0
        int n = Random.Range(1, m_FootstepSounds.Length);
        m_AudioSource.clip = m_FootstepSounds[n];
        m_AudioSource.PlayOneShot(m_AudioSource.clip);
        // move picked sound to index 0 so it's not picked next time
        m_FootstepSounds[n] = m_FootstepSounds[0];
        m_FootstepSounds[0] = m_AudioSource.clip;
    }
    private void UpdateCameraPosition(float speed)
    {
        Vector3 newCameraPosition;
        if (!m_UseHeadBob)
        {
            return;
        }
        if (m_CharacterController.velocity.magnitude > 0 && m_CharacterController.isGrounded)
        {
            m_Camera.transform.localPosition =
                m_HeadBob.DoHeadBob(m_CharacterController.velocity.magnitude +
                                  (speed * (m_IsWalking ? 1f : m_RunstepLenghten)));
            newCameraPosition = m_Camera.transform.localPosition;
            newCameraPosition.y = m_Camera.transform.localPosition.y - m_JumpBob.Offset();
        }
        else
        {
            newCameraPosition = m_Camera.transform.localPosition;
            newCameraPosition.y = m_OriginalCameraPosition.y - m_JumpBob.Offset();
        }
        m_Camera.transform.localPosition = newCameraPosition;
    }
    private void GetInput(out float speed)
    {
        // Read input
        float horizontal = CrossPlatformInputManager.GetAxis("Horizontal");
        float vertical = CrossPlatformInputManager.GetAxis("Vertical");

        bool waswalking = m_IsWalking;
#if !MOBILE_INPUT
        // On standalone builds, walk/run speed is modified by a key press.
        // keep track of whether or not the character is walking or running
        m_IsWalking = !Input.GetButton("Run");
        if(vertical < 0)
            m_IsWalking = true;
#endif
        // set the desired speed to be walking or running
        speed = m_IsWalking ? m_WalkSpeed : m_RunSpeed;
        m_Input = new Vector2(horizontal, vertical);

        // normalize input if it exceeds 1 in combined length:
        if (m_Input.sqrMagnitude > 1)
        {
            m_Input.Normalize();
        }

        // handle speed change to give an fov kick
        // only if the player is going to a run, is running and the fovkick is to be used
        if (m_IsWalking != waswalking && m_UseFovKick && m_CharacterController.velocity.sqrMagnitude > 0)
        {
            StopAllCoroutines();
            StartCoroutine(!m_IsWalking ? m_FovKick.FOVKickUp() : m_FovKick.FOVKickDown());
        }
    }
    private void RotateView()
    {
        m_MouseLook.LookRotation(transform, m_Camera.transform);
    }
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody body = hit.collider.attachedRigidbody;
        //dont move the rigidbody if the character is on top of it
        if (m_CollisionFlags == CollisionFlags.Below)
        {
            return;
        }

        if (body == null || body.isKinematic)
        {
            return;
        }
        body.AddForceAtPosition(m_CharacterController.velocity * 0.1f, hit.point, ForceMode.Impulse);
    }
    #endregion
}
//Dependent Player Classes
public enum MovementState
{
    Idle,
    Walking,
    Running
}

[System.Serializable]
public class PerkManagement
{
    public Transform PerkHands;
    public Transform PerkBottleNode;

    public GameObject juggBottle;
    //players current perk
    public bool hasJugg;

    //perk bought status
    public bool hasDrankJugg;

    //set perk attributes
    public bool setJugg;
}
[System.Serializable]
public class EquipmentManager
{
    public Transform KnifeHands;
}