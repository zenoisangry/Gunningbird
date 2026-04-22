using UnityEngine;
using System.Collections.Generic;

public class ImpactManager : MonoBehaviour
{
    public static ImpactManager Instance { get; private set; }

    [System.Serializable]
    public struct SurfaceEntry
    {
        public string tag;
        public ImpactData data;
    }

    [SerializeField] private SurfaceEntry[] surfaceEntries;
    [SerializeField] private ImpactData defaultImpact;

    private Dictionary<string, ImpactData> _map;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        _map = new Dictionary<string, ImpactData>();
        foreach (var entry in surfaceEntries)
            if (!string.IsNullOrEmpty(entry.tag) && entry.data != null)
                _map[entry.tag] = entry.data;
    }

    public ImpactData Get(string tag)
        => _map.TryGetValue(tag, out var data) ? data : defaultImpact;
}
