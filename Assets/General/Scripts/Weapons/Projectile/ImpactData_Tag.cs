using UnityEngine;

[CreateAssetMenu(fileName = "ImpactData", menuName = "Weapon System/Impact Data")]
public class ImpactData : ScriptableObject
{
    // <(= O . O =)> fat cat!
    [Header("VFX")]
    public GameObject impactParticlePrefab;

    [Header("Decal")]
    public Sprite impactDecalSprite;
    public Vector2 decalSizeRange = new Vector2(0.08f, 0.18f);
    public float decalDuration = 8f;

    [Header("Audio")]
    public AudioClip impactSound;
    [Range(0f, 1f)] public float volume = 0.8f;
}
