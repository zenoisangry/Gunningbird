using UnityEngine;
// ฅ^•ﻌ•^ฅ
[CreateAssetMenu(fileName = "ImpactData", menuName = "Weapon System/Impact Data")]
public class ImpactData : ScriptableObject
{
    [Header("VFX - Particle istanziato al punto di impatto")]
    public GameObject impactParticlePrefab;

    [Header("Decal")]
    public Sprite impactDecalSprite;
    
    public Vector2 decalSizeRange = new Vector2(0.08f, 0.18f);
    
    public float decalDuration = 8f;

    [Header("Audio")]
    public AudioClip impactSound;
    [Range(0f, 1f)] public float volume = 0.8f;

    [Header("Materiale")]
    
    public bool changeMaterial = false;
    public Material impactMaterial;
    
    public float materialDuration = 0.5f;

    [Header("Emitter figlio")]
   
    public bool activateChildEmitter = false;
    
    public string childEmitterName = "";
    
    public float childEmitterDuration = 3f;
}
