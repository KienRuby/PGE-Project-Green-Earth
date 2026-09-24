using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enables a deterministic share of the authored obstacles under this object.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class GenMineObstacleDensity : MonoBehaviour
{
    [SerializeField, Range(0, 100)]
    [InspectorName("Mật độ (%)")]
    [Tooltip("0 = không có chướng ngại; 100 = hiện toàn bộ chướng ngại.")]
    private int densityPercent = 100;

    [SerializeField]
    [InspectorName("Seed bố cục")]
    [Tooltip("Đổi seed để chọn bố cục chướng ngại khác ở cùng mật độ.")]
    private int seed = 0;

#if UNITY_EDITOR
    private bool applyScheduled;
#endif

    private struct RankedObstacle
    {
        public GameObject GameObject;
        public uint Rank;
        public int SiblingIndex;
    }

    private void OnEnable()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            ScheduleApply();
            return;
        }
#endif
        ApplyDensity();
    }

    private void OnValidate()
    {
        densityPercent = Mathf.Clamp(densityPercent, 0, 100);
#if UNITY_EDITOR
        ScheduleApply();
#endif
    }

#if UNITY_EDITOR
    private void OnDisable()
    {
        if (!applyScheduled) return;
        UnityEditor.EditorApplication.delayCall -= ApplyScheduledDensity;
        applyScheduled = false;
    }

    private void ScheduleApply()
    {
        if (applyScheduled) return;
        applyScheduled = true;
        UnityEditor.EditorApplication.delayCall += ApplyScheduledDensity;
    }

    private void ApplyScheduledDensity()
    {
        applyScheduled = false;
        if (this != null && isActiveAndEnabled)
            ApplyDensity();
    }
#endif

    public void SetDensityPercent(int value)
    {
        densityPercent = Mathf.Clamp(value, 0, 100);
        ApplyDensity();
    }

    [ContextMenu("Apply Obstacle Density")]
    public void ApplyDensity()
    {
        if (!isActiveAndEnabled) return;

        int count = transform.childCount;
        var obstacles = new List<RankedObstacle>(count);
        for (int i = 0; i < count; i++)
        {
            Transform child = transform.GetChild(i);
            obstacles.Add(new RankedObstacle
            {
                GameObject = child.gameObject,
                Rank = StableRank(child.name, seed),
                SiblingIndex = i
            });
        }

        obstacles.Sort((a, b) =>
        {
            int rankComparison = a.Rank.CompareTo(b.Rank);
            return rankComparison != 0 ? rankComparison : a.SiblingIndex.CompareTo(b.SiblingIndex);
        });

        int activeCount = Mathf.RoundToInt(densityPercent * count / 100f);
        for (int i = 0; i < count; i++)
        {
            GameObject obstacle = obstacles[i].GameObject;
            bool shouldBeActive = i < activeCount;
            if (obstacle.activeSelf == shouldBeActive) continue;

#if UNITY_EDITOR
            if (!Application.isPlaying)
                UnityEditor.Undo.RecordObject(obstacle, "Change Obstacle Density");
#endif
            obstacle.SetActive(shouldBeActive);
        }
    }

    private static uint StableRank(string name, int layoutSeed)
    {
        unchecked
        {
            uint hash = 2166136261u ^ (uint)layoutSeed;
            for (int i = 0; i < name.Length; i++)
            {
                hash ^= name[i];
                hash *= 16777619u;
            }
            return hash;
        }
    }
}
