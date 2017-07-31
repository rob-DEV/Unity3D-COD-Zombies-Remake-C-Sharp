using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponList : MonoBehaviour
{
    public List<Weapon> AllWeapons;
    public GameObject[] WeaponModels;
    public AudioClip emptyClip;
}

public enum WeaponType
{
    Auto,
    SemiAuto,
    Semi3Burst
}

[System.Serializable]
public class Weapon : ICloneable
{
    public WeaponType WeaponType = WeaponType.Auto;
    public string Name = "Weapon";
    public float Firerate = 0.1f;
    public float Damage = 25;
    public float Range = 1000;
    public int ClipSize = 1;

    public int Clips = 1;
    [NonSerialized]
    public int Clip = 1;

    public int Totalammo;
    public AudioClip[] ShootSounds;
    public AudioClip ReloadSound;
    public Transform WeaponTransform;

    public Vector3 RecoilRotation;
    public Vector3 RecoilKickback;

    public int CurrentScope;
    public List<WeaponScope> Scopes = new List<WeaponScope>();

    public object Clone()
    {
        return this.MemberwiseClone();
    }
}
[System.Serializable]
public class WeaponScope
{
    public string Name;
    public float FOV;
    public Vector3 AdsPosition;
    public Vector3 AdsRotation;
    public Transform ScopeTransform;
}
