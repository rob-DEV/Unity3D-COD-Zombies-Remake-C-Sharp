using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageMultiplier : MonoBehaviour {

    public float damageMultiplier;
    public Transform zombie;

    private Zombie zombieComp;

    void Start()
    {
        zombieComp = zombie.GetComponent<Zombie>();
    }

    public void HitApplyDamage(object[] args)
    {
        zombieComp.ApplyDamage(args);
    }
}
