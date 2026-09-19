using UnityEngine;

/// <summary>
/// Lưu trữ danh sách Sprite của 7 loại Pet để tải an toàn và ổn định ở mọi nền tảng (Runtime/Build).
/// ID 0: Pink Bat
/// ID 1: Green Slime
/// ID 2: Cyber Spider
/// ID 3: Viper Bot
/// ID 4: Cydog
/// ID 5: Iron Shell
/// ID 6: Turbo Snail
/// </summary>
[CreateAssetMenu(fileName = "PetVisualLibrary", menuName = "PGE/Pet/Pet Visual Library")]
public sealed class PetVisualLibrary : ScriptableObject
{
    [Tooltip("Danh sách Sprite tương ứng 7 Pet theo thứ tự ID 0 đến 6.")]
    public Sprite[] petSprites = new Sprite[7];
}
