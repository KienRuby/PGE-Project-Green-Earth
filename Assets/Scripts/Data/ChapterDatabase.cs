using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cơ sở dữ liệu tập trung quản lý toàn bộ các Chapter trong game.
/// Cho phép thêm bao nhiêu Chapter tùy thích mà không cần đụng đến code UI.
/// </summary>
[CreateAssetMenu(fileName = "ChapterDatabase", menuName = "PGE/Chapter Database", order = 11)]
public class ChapterDatabase : ScriptableObject
{
    [Tooltip("Danh sách toàn bộ Chapter theo thứ tự mở khóa / xuất hiện.")]
    [SerializeField] private List<ChapterData> chapters = new List<ChapterData>();

    public IReadOnlyList<ChapterData> Chapters => chapters;
    public int Count => chapters != null ? chapters.Count : 0;

    public ChapterData GetChapter(int index)
    {
        if (chapters == null || chapters.Count == 0) return null;
        int clamped = Mathf.Clamp(index, 0, chapters.Count - 1);
        return chapters[clamped];
    }

    /// <summary>
    /// Cho phép thiết lập danh sách Chapter trong Unit Test và Editor Tool.
    /// </summary>
    public void SetChaptersForTesting(List<ChapterData> chapterList)
    {
        chapters = chapterList ?? new List<ChapterData>();
    }
}
