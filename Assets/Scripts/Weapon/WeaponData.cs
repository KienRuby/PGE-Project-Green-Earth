using UnityEngine;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "PGE/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Weapon Info")]
    [Tooltip("Mã định danh duy nhất của khẩu súng (ví dụ: blaster, shotgun, sniper, smg).")]
    public string weaponId = "blaster";

    [Tooltip("Tên hiển thị của khẩu súng.")]
    public string weaponName = "Blaster Gun";

    [Tooltip("Icon hiển thị trên giao diện Main Menu / Shop.")]
    public Sprite weaponIcon;

    [Header("Visuals & Barrel Position")]
    [Tooltip("Sprite hình ảnh của khẩu súng hiển thị trên tay người chơi.")]
    public Sprite gunSprite;

    [Tooltip("Vị trí local của FirePoint. Giá trị này sẽ ghi đè vị trí FirePoint trong Scene khi vào Play.")]
    public Vector2 firePointOffset = new Vector2(1.5f, 0.1f);

    [Header("Core 4 Weapon Stats (4 Chỉ số cốt lõi)")]
    [Tooltip("1. TỐC ĐỘ BẮN: Số phát bắn mỗi giây (ví dụ: Shotgun = 1.5, Súng máy SMG = 8).")]
    [Range(0.1f, 20f)] public float fireRate = 2f;

    [Tooltip("2. KHOẢNG CÁCH BẮN: Tầm bắn tối đa của đạn và phạm vi phát hiện quái (mét) (ví dụ: Shotgun = 6m, Sniper = 18m).")]
    [Range(2f, 30f)] public float attackRange = 12f;

    [Tooltip("3. SÁT THƯƠNG: Lượng sát thương gây ra cho quái vật khi bị trúng đạn (ví dụ: SMG = 15, Sniper = 150).")]
    [Range(1, 1000)] public int damage = 20;

    [Tooltip("4. TỐC ĐỘ RA ĐẠN: Vận tốc bay của viên đạn sau khi rời nòng súng (mét/giây) (ví dụ: Đạn Plasma = 8, Đạn Sniper = 25).")]
    [Range(3f, 50f)] public float bulletSpeed = 12f;

    [Header("Multi-shot Settings (Tùy chọn bắn nhiều viên như Shotgun)")]
    [Tooltip("Số lượng viên đạn bắn ra trong một lần bóp cò (mặc định = 1, Shotgun = 3 đến 5).")]
    [Range(1, 10)] public int bulletsPerShot = 1;

    [Tooltip("Độ tỏa góc bắn nếu bắn nhiều viên (độ).")]
    [Range(0f, 60f)] public float spreadAngle = 15f;

    [Header("Projectile Prefab")]
    [Tooltip("Prefab viên đạn riêng biệt của khẩu súng này.")]
    public GameObject projectilePrefab;
}
