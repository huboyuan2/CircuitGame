using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages wire placement limits for each level
/// Tracks available wire counts and validates placement attempts
/// </summary>
public class WireLimitManager : MonoBehaviour
{
    [System.Serializable]
    public class WireLimit
    {
        public WireType wireType;
        public int maxCount;
        [HideInInspector] public int currentCount; // Current placed count
    }

    [Header("Level Configuration")]
    [Tooltip("Define wire limits for this level")]
    public List<WireLimit> wireLimits = new List<WireLimit>();

    [Header("Settings")]
    [Tooltip("Allow unlimited placement if no limit is defined for a wire type")]
    public bool allowUnlimitedIfNotDefined = true;

    // Runtime tracking
    private Dictionary<WireType, WireLimit> limitDict;

    // Events for UI updates
    public static event Action<WireType, int, int> OnWireCountChanged; // (wireType, current, max)

    // Singleton
    private static WireLimitManager _instance;
    public static WireLimitManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<WireLimitManager>();
            }
            return _instance;
        }
    }

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        InitializeLimits();
    }

    /// <summary>
    /// Initialize limit dictionary and reset counts
    /// </summary>
    private void InitializeLimits()
    {
        limitDict = new Dictionary<WireType, WireLimit>();

        foreach (var limit in wireLimits)
        {
            if (limit.wireType == WireType.None)
                continue;

            limitDict[limit.wireType] = limit;
            limit.currentCount = 0;
        }

        Debug.Log($"WireLimitManager: Initialized {limitDict.Count} wire limits");
    }

    /// <summary>
    /// Reset all wire counts (call when restarting level)
    /// </summary>
    public void ResetAllCounts()
    {
        foreach (var limit in wireLimits)
        {
            limit.currentCount = 0;
            NotifyCountChanged(limit.wireType);
        }
    }

    /// <summary>
    /// Check if a wire can be placed
    /// </summary>
    public bool CanPlaceWire(WireType wireType)
    {
        if (wireType == WireType.None)
            return true;

        if (!limitDict.TryGetValue(wireType, out WireLimit limit))
        {
            // No limit defined for this type
            return allowUnlimitedIfNotDefined;
        }

        return limit.currentCount < limit.maxCount;
    }

    /// <summary>
    /// Try to consume one wire from available count
    /// Returns true if successful, false if limit reached
    /// </summary>
    public bool TryConsumeWire(WireType wireType)
    {
        if (wireType == WireType.None)
            return true;

        if (!CanPlaceWire(wireType))
        {
            Debug.LogWarning($"Cannot place {wireType}: limit reached ({GetCurrentCount(wireType)}/{GetMaxCount(wireType)})");
            return false;
        }

        if (limitDict.TryGetValue(wireType, out WireLimit limit))
        {
            limit.currentCount++;
            NotifyCountChanged(wireType);
        }

        return true;
    }

    /// <summary>
    /// Return one wire to available count (called when removing/deleting wire)
    /// </summary>
    public void ReturnWire(WireType wireType)
    {
        if (wireType == WireType.None)
            return;

        if (limitDict.TryGetValue(wireType, out WireLimit limit))
        {
            if (limit.currentCount > 0)
            {
                limit.currentCount--;
                NotifyCountChanged(wireType);
                Debug.Log($"Returned {wireType}: now {limit.currentCount}/{limit.maxCount}");
            }
        }
    }

    /// <summary>
    /// Get current placed count for a wire type
    /// </summary>
    public int GetCurrentCount(WireType wireType)
    {
        if (limitDict.TryGetValue(wireType, out WireLimit limit))
        {
            return limit.currentCount;
        }
        return 0;
    }

    /// <summary>
    /// Get maximum allowed count for a wire type
    /// </summary>
    public int GetMaxCount(WireType wireType)
    {
        if (limitDict.TryGetValue(wireType, out WireLimit limit))
        {
            return limit.maxCount;
        }
        return allowUnlimitedIfNotDefined ? int.MaxValue : 0;
    }

    /// <summary>
    /// Get remaining available count for a wire type
    /// </summary>
    public int GetRemainingCount(WireType wireType)
    {
        return GetMaxCount(wireType) - GetCurrentCount(wireType);
    }

    /// <summary>
    /// Check if a wire type has a defined limit
    /// </summary>
    public bool HasLimit(WireType wireType)
    {
        return limitDict.ContainsKey(wireType);
    }

    /// <summary>
    /// Notify UI of count change
    /// </summary>
    private void NotifyCountChanged(WireType wireType)
    {
        OnWireCountChanged?.Invoke(wireType, GetCurrentCount(wireType), GetMaxCount(wireType));
    }

    /// <summary>
    /// Editor helper: Initialize wire limits for all wire types
    /// </summary>
    [ContextMenu("Initialize All Wire Types")]
    private void InitializeAllWireTypes()
    {
        wireLimits.Clear();

        foreach (WireType type in System.Enum.GetValues(typeof(WireType)))
        {
            if (type == WireType.None)
                continue;

            wireLimits.Add(new WireLimit
            {
                wireType = type,
                maxCount = 10 // Default value
            });
        }

        Debug.Log($"Initialized {wireLimits.Count} wire type limits");
    }

    #region Debug Methods

    [ContextMenu("Debug Print All Limits")]
    private void DebugPrintLimits()
    {
        Debug.Log("=== Wire Limits ===");
        foreach (var limit in wireLimits)
        {
            Debug.Log($"{limit.wireType}: {limit.currentCount}/{limit.maxCount} (Remaining: {limit.maxCount - limit.currentCount})");
        }
    }

    #endregion
}