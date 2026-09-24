using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bộ điều khiển trung tâm của chế độ chơi Tower Def (Thủ Thành):
/// - Quản lý tài nguyên: Vàng (Gold khởi đầu = 50), Năng lượng (Energy khởi đầu = 0).
/// - Khởi tạo bản đồ chính xác theo Thiết kế (7 cột, khu vực quái tối phía trên, tường gạch chia đôi, phòng phòng thủ 7x4 bên dưới).
/// - Quản lý công trình mặc định ban đầu: Cổng sắt có thanh máu, Pháo ở Hàng 2 Cột 2, Trụ năng lượng ở Hàng 3 Cột 3, Giường ở Hàng 3 Cột 4.
/// - Điều phối đợt tấn công của quái vật, đường đạn của tháp pháo, thăng cấp công trình và lưu trữ tiến trình qua TowerDefProgress.
/// </summary>
[DefaultExecutionOrder(-50)]
public class TowerDefGameManager : MonoBehaviour
{
    public static TowerDefGameManager Instance { get; private set; }

    [Header("Economy (Khởi tạo theo Ảnh Thiết Kế)")]
    [SerializeField] private int gold = 50;
    [SerializeField] private int energy = 0;

    [Header("Level State")]
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int totalWaves = 3;
    [SerializeField] private int currentWave = 0;
    [SerializeField] private bool waveInProgress = false;

    [Header("Game Elements")]
    [SerializeField] private TowerDefGate gate;
    [SerializeField] private TowerDefUIController uiController;
    [SerializeField] private Transform enemySpawnParent;
    [SerializeField] private Transform projectileParent;

    [Header("Sprite Assets")]
    [SerializeField] private Sprite darkTileSprite;
    [SerializeField] private Sprite wallStripSprite;
    [SerializeField] private Sprite gateSprite;
    [SerializeField] private Sprite plusTileSprite;
    [SerializeField] private Sprite turretBaseSprite;
    [SerializeField] private Sprite turretGunSprite;
    [SerializeField] private Sprite pawnTowerSprite;
    [SerializeField] private Sprite corePodSprite;
    [SerializeField] private Sprite upgradeCircleSprite;
    [SerializeField] private Sprite greenBarSprite;
    [SerializeField] private Sprite backArrowSprite;
    [SerializeField] private Sprite coinIconSprite;
    [SerializeField] private Sprite energyIconSprite;
    [SerializeField] private Sprite creepSprite;
    [SerializeField] private Sprite bossSprite;

    private readonly List<TowerDefEnemy> activeEnemies = new List<TowerDefEnemy>();
    private readonly TowerDefGridCell[,] gridCells = new TowerDefGridCell[4, 7]; // 4 hàng x 7 cột
    private bool isGameOver = false;

    public int Gold => gold;
    public int Energy => energy;
    public int CurrentLevel => currentLevel;
    public bool WaveInProgress => waveInProgress;
    public IReadOnlyList<TowerDefEnemy> ActiveEnemies => activeEnemies;
    public TowerDefGate Gate => gate;

    public event Action<int, int> OnCurrencyChanged;
    public event Action<int> OnWaveCompleted;
    public event Action OnLevelVictory;
    public event Action OnLevelDefeat;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitRuntimeHooks()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoadedCallback;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoadedCallback;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CheckActiveSceneOnStart()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.name == "TowerDef")
        {
            EnsureManagerInScene(scene);
        }
    }

    private static void OnSceneLoadedCallback(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (scene.name == "TowerDef")
        {
            EnsureManagerInScene(scene);
        }
    }

    private static void EnsureManagerInScene(UnityEngine.SceneManagement.Scene scene)
    {
        if (Instance == null && UnityEngine.Object.FindObjectOfType<TowerDefGameManager>() == null)
        {
            Debug.Log("[TowerDefGameManager] Auto-bootstrapping TowerDef scene dynamically at runtime...");
            GameObject gmObj = new GameObject("TowerDefGameManager", typeof(TowerDefGameManager));
            if (scene.IsValid() && scene.isLoaded)
            {
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(gmObj, scene);
            }
        }
    }

    private void Awake()
    {
        Instance = this;
        currentLevel = TowerDefProgress.SelectedLevel;
        EnsureSceneMap();
    }

    private void Start()
    {
        UpdateUI();
        if (gate != null)
        {
            gate.OnGateDestroyed += HandleGateBreached;
        }

        StartCoroutine(StartGameLoopRoutine());
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (gate != null) gate.OnGateDestroyed -= HandleGateBreached;
    }

    #region Economy & Upgrades
    public void AddGold(int amount)
    {
        gold += amount;
        UpdateUI();
    }

    public void AddEnergy(int amount)
    {
        energy += amount;
        UpdateUI();
    }

    public bool TryBuildStructure(TowerDefGridCell cell, TowerDefStructureType type)
    {
        if (cell == null || cell.IsOccupied) return false;

        int cost = type == TowerDefStructureType.Turret ? 50 : 60;
        if (gold < cost)
        {
            ShowFloatingText(cell.transform.position, "NOT ENOUGH GOLD!", Color.red);
            return false;
        }

        gold -= cost;
        Sprite baseSpr = type == TowerDefStructureType.Turret ? turretBaseSprite :
                         type == TowerDefStructureType.CoreBed ? corePodSprite : pawnTowerSprite;
        Sprite gunSpr = type == TowerDefStructureType.Turret ? turretGunSprite : null;

        cell.PlaceStructure(type, baseSpr, gunSpr, 1);
        ShowFloatingText(cell.transform.position, "BUILT!", Color.green);
        UpdateUI();
        return true;
    }

    public bool TryUpgradeStructure(TowerDefGridCell cell)
    {
        if (cell == null || !cell.IsOccupied) return false;

        bool upgraded = false;
        if (cell.Turret != null)
        {
            upgraded = cell.Turret.TryUpgrade(ref gold);
        }
        else if (cell.Generator != null)
        {
            upgraded = cell.Generator.TryUpgrade(ref gold);
        }

        if (upgraded)
        {
            cell.UpgradeCurrentStructure();
            ShowFloatingText(cell.transform.position, "UPGRADED!", Color.cyan);
            UpdateUI();
        }
        else
        {
            ShowFloatingText(cell.transform.position, "NOT ENOUGH GOLD!", Color.red);
        }

        return upgraded;
    }

    public bool TryRepairGate(TowerDefGate targetGate)
    {
        if (targetGate == null || gold < 20)
        {
            if (targetGate != null) ShowFloatingText(targetGate.transform.position, "NOT ENOUGH GOLD!", Color.red);
            return false;
        }

        gold -= 20;
        targetGate.Repair(100f);
        ShowFloatingText(targetGate.transform.position, "REPAIRED +100", Color.green);
        UpdateUI();
        return true;
    }

    public bool TryUpgradeGate(TowerDefGate targetGate)
    {
        if (targetGate == null) return false;

        bool ok = targetGate.TryUpgrade(ref gold);
        if (ok)
        {
            ShowFloatingText(targetGate.transform.position, $"GATE LV.{targetGate.GateLevel}!", Color.cyan);
            UpdateUI();
        }
        else
        {
            ShowFloatingText(targetGate.transform.position, "NOT ENOUGH GOLD!", Color.red);
        }
        return ok;
    }

    public void UpdateUI()
    {
        OnCurrencyChanged?.Invoke(gold, energy);
        if (uiController != null)
        {
            uiController.UpdateCurrencyDisplay(gold, energy);
        }
    }

    public void ShowFloatingText(Vector3 pos, string text, Color col)
    {
        if (uiController != null)
        {
            uiController.SpawnFloatingText(pos, text, col);
        }
    }

    public void OpenBuildPopup(TowerDefGridCell cell)
    {
        if (uiController != null) uiController.OpenBuildModal(cell);
    }

    public void OpenStructureUpgradePopup(TowerDefGridCell cell)
    {
        if (uiController != null) uiController.OpenUpgradeModal(cell);
    }

    public void OpenGateUpgradePopup(TowerDefGate targetGate)
    {
        if (uiController != null) uiController.OpenGateModal(targetGate);
    }
    #endregion

    #region Combat & Spawning
    public void SpawnBullet(Vector3 startPos, TowerDefEnemy target, float dmg)
    {
        GameObject bulletObj = new GameObject("Bullet", typeof(RectTransform), typeof(Image), typeof(TowerDefProjectile));
        bulletObj.transform.SetParent(projectileParent != null ? projectileParent : transform, false);
        bulletObj.transform.position = startPos;

        RectTransform rt = bulletObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(16f, 32f);

        Image img = bulletObj.GetComponent<Image>();
        img.color = new Color(0.2f, 0.9f, 1f, 1f);
        img.raycastTarget = false;

        TowerDefProjectile proj = bulletObj.GetComponent<TowerDefProjectile>();
        proj.Setup(target, dmg, 1400f);
    }

    private IEnumerator StartGameLoopRoutine()
    {
        // Chờ 3 giây đầu để người chơi quan sát căn cứ và làm quen
        yield return new WaitForSeconds(3.0f);

        for (int w = 1; w <= totalWaves; w++)
        {
            if (isGameOver) yield break;

            currentWave = w;
            waveInProgress = true;
            ShowFloatingText(new Vector3(0f, 400f, 0f), $"WAVE {w} INCOMING!", Color.red);

            yield return StartCoroutine(SpawnWaveRoutine(w));

            // Đợi tất cả quái vật của đợt này chết
            while (activeEnemies.Count > 0)
            {
                if (isGameOver) yield break;
                yield return new WaitForSeconds(0.5f);
            }

            waveInProgress = false;
            OnWaveCompleted?.Invoke(w);

            if (w < totalWaves)
            {
                ShowFloatingText(new Vector3(0f, 400f, 0f), "WAVE CLEARED!", Color.green);
                yield return new WaitForSeconds(4.0f);
            }
        }

        // Hoàn thành tất cả các wave -> CHIẾN THẮNG
        if (!isGameOver)
        {
            HandleLevelVictory();
        }
    }

    private IEnumerator SpawnWaveRoutine(int wave)
    {
        int enemyCount = 3 + wave * 2;
        float spawnY = 850f; // Vị trí xuất hiện ở phần trên cùng của khu vực tối
        float gateStopY = gate != null ? gate.transform.localPosition.y + 40f : 100f;

        for (int i = 0; i < enemyCount; i++)
        {
            if (isGameOver) yield break;

            // Chọn ngẫu nhiên một trong 7 cột để quái vật xuất hiện
            int col = UnityEngine.Random.Range(0, 7);
            float colX = GetColumnWorldX(col);

            SpawnEnemy(new Vector3(colX, spawnY, 0f), wave, gateStopY, i == enemyCount - 1 && wave == totalWaves);
            yield return new WaitForSeconds(1.8f);
        }
    }

    private void SpawnEnemy(Vector3 localPos, int wave, float gateStopY, bool isBoss = false)
    {
        GameObject enemyObj = new GameObject(isBoss ? "BossEnemy" : "CreepEnemy", typeof(RectTransform), typeof(Image), typeof(TowerDefEnemy));
        enemyObj.transform.SetParent(enemySpawnParent != null ? enemySpawnParent : transform, false);
        enemyObj.transform.localPosition = localPos;

        RectTransform rt = enemyObj.GetComponent<RectTransform>();
        rt.sizeDelta = isBoss ? new Vector2(160f, 160f) : new Vector2(110f, 110f);

        Image img = enemyObj.GetComponent<Image>();
        img.sprite = isBoss ? (bossSprite ?? creepSprite) : (creepSprite ?? bossSprite);
        img.color = Color.white;
        img.raycastTarget = false;

        // Health bar nhỏ trên đầu quái
        GameObject hpRoot = new GameObject("HpBar", typeof(RectTransform), typeof(Image));
        hpRoot.transform.SetParent(enemyObj.transform, false);
        RectTransform hpRt = hpRoot.GetComponent<RectTransform>();
        hpRt.anchorMin = new Vector2(0.5f, 1f);
        hpRt.anchorMax = new Vector2(0.5f, 1f);
        hpRt.pivot = new Vector2(0.5f, 0f);
        hpRt.anchoredPosition = new Vector2(0f, 6f);
        hpRt.sizeDelta = new Vector2(isBoss ? 120f : 80f, 12f);
        Image bgImg = hpRoot.GetComponent<Image>();
        bgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        bgImg.raycastTarget = false;

        GameObject hpFill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        hpFill.transform.SetParent(hpRoot.transform, false);
        RectTransform fillRt = hpFill.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.pivot = new Vector2(0f, 0.5f);
        fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;
        Image fillImg = hpFill.GetComponent<Image>();
        fillImg.color = isBoss ? new Color(1f, 0.2f, 0.2f) : new Color(0.9f, 0.8f, 0.1f);
        fillImg.raycastTarget = false;

        TowerDefEnemy enemy = enemyObj.GetComponent<TowerDefEnemy>();
        float hp = (isBoss ? 350f : 70f) * (1f + (wave - 1) * 0.35f);
        float spd = isBoss ? 75f : 110f;
        float dmg = isBoss ? 25f : 10f;
        int rwd = isBoss ? 50 : 15;

        enemy.Setup(hp, spd, dmg, rwd, gate, gateStopY, img.sprite);
        enemy.OnEnemyDied += HandleEnemyDied;
        activeEnemies.Add(enemy);
    }

    private void HandleEnemyDied(TowerDefEnemy enemy)
    {
        activeEnemies.Remove(enemy);
    }

    private void HandleGateBreached()
    {
        if (isGameOver) return;
        isGameOver = true;
        OnLevelDefeat?.Invoke();
        if (uiController != null) uiController.ShowDefeat();
    }

    private void HandleLevelVictory()
    {
        if (isGameOver) return;
        isGameOver = true;
        TowerDefProgress.CompleteLevel(currentLevel);
        OnLevelVictory?.Invoke();
        if (uiController != null) uiController.ShowVictory();
    }
    #endregion

    #region Self-Healing Layout & Bootstrap
    /// <summary>
    /// Đảm bảo toàn bộ Bản đồ Tower Def được khởi tạo hoàn chỉnh khớp 100% Ảnh Thiết Kế (Image 2).
    /// </summary>
    public void EnsureSceneMap()
    {
        LoadAssetReferences();

        Camera cam = Camera.main ?? FindObjectOfType<Camera>();
        if (cam != null)
        {
            cam.backgroundColor = new Color(0.08f, 0.08f, 0.1f, 1f);
            cam.orthographic = true;
        }

        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("TowerDefCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler cs = canvasObj.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080f, 1920f);
            cs.matchWidthOrHeight = 0f; // Khớp chuẩn bề ngang màn hình
        }

        // 1. Background Grid (Nền tối 7 cột)
        Transform bgTr = canvas.transform.Find("BackgroundGrid");
        if (bgTr == null)
        {
            GameObject bgObj = new GameObject("BackgroundGrid", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(canvas.transform, false);
            bgObj.transform.SetAsFirstSibling();
            RectTransform bgRt = bgObj.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;

            Image bgImg = bgObj.GetComponent<Image>();
            Sprite nenSprite = darkTileSprite ?? Resources.Load<Sprite>("UI/MiniGame/nen_map_thu_thanh");
            if (nenSprite != null) bgImg.sprite = nenSprite;
            bgImg.color = Color.white;
            bgImg.raycastTarget = false;
        }

        // 2. PlayArea Root
        Transform playArea = canvas.transform.Find("PlayArea") ?? new GameObject("PlayArea", typeof(RectTransform)).transform;
        playArea.SetParent(canvas.transform, false);
        RectTransform playRt = playArea.GetComponent<RectTransform>();
        playRt.anchorMin = Vector2.zero;
        playRt.anchorMax = Vector2.one;
        playRt.offsetMin = playRt.offsetMax = Vector2.zero;

        // Container cho Quái và Đạn
        if (enemySpawnParent == null)
        {
            enemySpawnParent = playArea.Find("EnemiesRoot") ?? new GameObject("EnemiesRoot", typeof(RectTransform)).transform;
            enemySpawnParent.SetParent(playArea, false);
        }
        if (projectileParent == null)
        {
            projectileParent = playArea.Find("ProjectilesRoot") ?? new GameObject("ProjectilesRoot", typeof(RectTransform)).transform;
            projectileParent.SetParent(playArea, false);
        }

        float cellWidth = 1080f / 7f; // ~154.2857f
        float cellHeight = 154.2857f;
        float wallY = 617.14f; // 4 hàng phòng thủ phía dưới (4 * 154.2857f)

        // 3. Tường gạch ngăn cách (Wall_Brick_Strip) & Cổng sắt (Gate_Metal)
        Transform wallTr = playArea.Find("WallAndGate");
        if (wallTr == null)
        {
            GameObject wallObj = new GameObject("WallAndGate", typeof(RectTransform));
            wallObj.transform.SetParent(playArea, false);
            wallTr = wallObj.transform;

            RectTransform wallRootRt = wallObj.GetComponent<RectTransform>();
            wallRootRt.anchorMin = new Vector2(0f, 0f);
            wallRootRt.anchorMax = new Vector2(1f, 0f);
            wallRootRt.pivot = new Vector2(0.5f, 0f);
            wallRootRt.anchoredPosition = new Vector2(0f, wallY);
            wallRootRt.sizeDelta = new Vector2(0f, cellHeight);

            // Dải tường gạch ngang
            GameObject stripObj = new GameObject("BrickStrip", typeof(RectTransform), typeof(Image));
            stripObj.transform.SetParent(wallTr, false);
            RectTransform stripRt = stripObj.GetComponent<RectTransform>();
            stripRt.anchorMin = Vector2.zero;
            stripRt.anchorMax = Vector2.one;
            stripRt.offsetMin = stripRt.offsetMax = Vector2.zero;
            Image stripImg = stripObj.GetComponent<Image>();
            if (wallStripSprite != null) stripImg.sprite = wallStripSprite;
            stripImg.color = Color.white;
            stripImg.raycastTarget = false;

            // Cổng sắt ở cột chính giữa (Cột 4, index 3)
            GameObject gateObj = new GameObject("Gate", typeof(RectTransform), typeof(Image), typeof(Button), typeof(TowerDefGate));
            gateObj.transform.SetParent(wallTr, false);
            RectTransform gateRt = gateObj.GetComponent<RectTransform>();
            gateRt.anchorMin = new Vector2(0.5f, 0.5f);
            gateRt.anchorMax = new Vector2(0.5f, 0.5f);
            gateRt.pivot = new Vector2(0.5f, 0.5f);
            gateRt.anchoredPosition = Vector2.zero;
            gateRt.sizeDelta = new Vector2(cellWidth, cellHeight);

            Image gateImg = gateObj.GetComponent<Image>();
            if (gateSprite != null) gateImg.sprite = gateSprite;
            gateImg.color = Color.white;
            gateImg.raycastTarget = true;
            Button gateBtn = gateObj.GetComponent<Button>();

            // Thanh máu xanh lá trên đỉnh cửa (Bar_Green)
            GameObject hpObj = new GameObject("GateHpRoot", typeof(RectTransform), typeof(Image));
            hpObj.transform.SetParent(gateObj.transform, false);
            RectTransform hpRt = hpObj.GetComponent<RectTransform>();
            hpRt.anchorMin = new Vector2(0.5f, 1f);
            hpRt.anchorMax = new Vector2(0.5f, 1f);
            hpRt.pivot = new Vector2(0.5f, 1f);
            hpRt.anchoredPosition = new Vector2(0f, 0f);
            hpRt.sizeDelta = new Vector2(cellWidth, 26f);
            Image hpBg = hpObj.GetComponent<Image>();
            hpBg.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);
            hpBg.raycastTarget = false;

            GameObject hpFillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            hpFillObj.transform.SetParent(hpObj.transform, false);
            RectTransform hpFillRt = hpFillObj.GetComponent<RectTransform>();
            hpFillRt.anchorMin = Vector2.zero;
            hpFillRt.anchorMax = Vector2.one;
            hpFillRt.pivot = new Vector2(0f, 0.5f);
            hpFillRt.offsetMin = hpFillRt.offsetMax = Vector2.zero;
            Image hpFill = hpFillObj.GetComponent<Image>();
            if (greenBarSprite != null) hpFill.sprite = greenBarSprite;
            hpFill.color = Color.white;
            hpFill.raycastTarget = false;

            // Icon Nâng cấp tròn vàng (Icon_Upgrade_Circle)
            GameObject upObj = new GameObject("UpgradeBadge", typeof(RectTransform), typeof(Image));
            upObj.transform.SetParent(gateObj.transform, false);
            RectTransform upRt = upObj.GetComponent<RectTransform>();
            upRt.anchorMin = new Vector2(0.5f, 0.5f);
            upRt.anchorMax = new Vector2(0.5f, 0.5f);
            upRt.pivot = new Vector2(0.5f, 0.5f);
            upRt.anchoredPosition = new Vector2(0f, -10f);
            upRt.sizeDelta = new Vector2(64f, 64f);
            Image upImg = upObj.GetComponent<Image>();
            if (upgradeCircleSprite != null) upImg.sprite = upgradeCircleSprite;
            upImg.color = Color.white;
            upImg.raycastTarget = false;

            gate = gateObj.GetComponent<TowerDefGate>();
            gate.Setup(300f, hpFill, upObj, gateBtn);
        }
        else
        {
            gate = wallTr.GetComponentInChildren<TowerDefGate>(true);
        }

        // 4. Lưới phòng thủ 7x4 bên dưới tường (Defense Grid)
        Transform gridTr = playArea.Find("DefenseGrid");
        if (gridTr == null)
        {
            GameObject gridObj = new GameObject("DefenseGrid", typeof(RectTransform));
            gridObj.transform.SetParent(playArea, false);
            gridTr = gridObj.transform;

            RectTransform gridRt = gridObj.GetComponent<RectTransform>();
            gridRt.anchorMin = Vector2.zero;
            gridRt.anchorMax = new Vector2(1f, 0f);
            gridRt.pivot = new Vector2(0.5f, 0f);
            gridRt.anchoredPosition = Vector2.zero;
            gridRt.sizeDelta = new Vector2(0f, wallY);

            // Sinh 4 hàng x 7 cột ô
            for (int r = 0; r < 4; r++)
            {
                // r = 3 là hàng trên cùng ngay dưới cổng; r = 0 là hàng dưới cùng
                float posY = (r + 0.5f) * cellHeight;

                for (int c = 0; c < 7; c++)
                {
                    float posX = GetColumnWorldX(c);
                    gridCells[r, c] = CreateGridCell(gridTr, r, c, new Vector2(posX, posY), cellWidth, cellHeight);
                }
            }

            // Gắn các công trình mặc định ban đầu theo đúng Ảnh 2:
            // - Hàng 2 từ trên xuống (r = 2), Cột 2 (c = 1): Pháo (Turret)
            if (gridCells[2, 1] != null)
            {
                gridCells[2, 1].PlaceStructure(TowerDefStructureType.Turret, turretBaseSprite, turretGunSprite, 1);
            }

            // - Hàng 3 từ trên xuống (r = 1), Cột 3 (c = 2): Trụ Năng Lượng (Pawn Tower)
            if (gridCells[1, 2] != null)
            {
                gridCells[1, 2].PlaceStructure(TowerDefStructureType.EnergyGenerator, pawnTowerSprite, null, 1);
            }

            // - Hàng 3 từ trên xuống (r = 1), Cột 4 (c = 3, thẳng cổng): Giường (Core Bed)
            if (gridCells[1, 3] != null)
            {
                gridCells[1, 3].PlaceStructure(TowerDefStructureType.CoreBed, corePodSprite, null, 1);
            }
        }

        // 5. Thanh công cụ Đỉnh màn hình (Top Bar) & UI Controller
        if (uiController == null)
        {
            uiController = canvas.GetComponentInChildren<TowerDefUIController>(true);
            if (uiController == null)
            {
                uiController = BuildTopBarAndUI(canvas.transform);
            }
        }
    }

    private TowerDefGridCell CreateGridCell(Transform parent, int r, int c, Vector2 anchoredPos, float width, float height)
    {
        GameObject cellObj = new GameObject($"Cell_R{r}_C{c}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(TowerDefGridCell));
        cellObj.transform.SetParent(parent, false);

        RectTransform cellRt = cellObj.GetComponent<RectTransform>();
        cellRt.anchorMin = new Vector2(0.5f, 0f);
        cellRt.anchorMax = new Vector2(0.5f, 0f);
        cellRt.pivot = new Vector2(0.5f, 0.5f);
        cellRt.anchoredPosition = anchoredPos;
        cellRt.sizeDelta = new Vector2(width, height);

        Image slotImg = cellObj.GetComponent<Image>();
        if (plusTileSprite != null) slotImg.sprite = plusTileSprite;
        slotImg.color = Color.white;
        slotImg.raycastTarget = true;
        Button btn = cellObj.GetComponent<Button>();

        // Root chứa hình ảnh công trình
        GameObject sRoot = new GameObject("StructureRoot", typeof(RectTransform));
        sRoot.transform.SetParent(cellObj.transform, false);
        RectTransform sRt = sRoot.GetComponent<RectTransform>();
        sRt.anchorMin = sRt.anchorMax = new Vector2(0.5f, 0.5f);
        sRt.pivot = new Vector2(0.5f, 0.5f);
        sRt.anchoredPosition = Vector2.zero;
        sRt.sizeDelta = new Vector2(width * 0.9f, height * 0.95f);

        GameObject sImgObj = new GameObject("StructureImage", typeof(RectTransform), typeof(Image));
        sImgObj.transform.SetParent(sRoot.transform, false);
        RectTransform sImgRt = sImgObj.GetComponent<RectTransform>();
        sImgRt.anchorMin = Vector2.zero;
        sImgRt.anchorMax = Vector2.one;
        sImgRt.offsetMin = sImgRt.offsetMax = Vector2.zero;
        Image sImg = sImgObj.GetComponent<Image>();
        sImg.preserveAspect = true;
        sImg.raycastTarget = false;

        // Gun transform (cho nòng súng pháo)
        GameObject gunObj = new GameObject("Gun", typeof(RectTransform), typeof(Image));
        gunObj.transform.SetParent(sRoot.transform, false);
        RectTransform gunRt = gunObj.GetComponent<RectTransform>();
        gunRt.anchorMin = gunRt.anchorMax = new Vector2(0.5f, 0.5f);
        gunRt.pivot = new Vector2(0.5f, 0.5f);
        gunRt.anchoredPosition = Vector2.zero;
        gunRt.sizeDelta = new Vector2(width * 0.55f, height * 0.35f);
        Image gunImg = gunObj.GetComponent<Image>();
        gunImg.preserveAspect = true;
        gunImg.raycastTarget = false;
        gunObj.SetActive(false);

        // Upgrade icon
        GameObject upObj = new GameObject("UpgradeBadge", typeof(RectTransform), typeof(Image));
        upObj.transform.SetParent(cellObj.transform, false);
        RectTransform upRt = upObj.GetComponent<RectTransform>();
        upRt.anchorMin = upRt.anchorMax = new Vector2(0.5f, 0.5f);
        upRt.pivot = new Vector2(0.5f, 0.5f);
        upRt.anchoredPosition = Vector2.zero;
        upRt.sizeDelta = new Vector2(58f, 58f);
        Image upImg = upObj.GetComponent<Image>();
        if (upgradeCircleSprite != null) upImg.sprite = upgradeCircleSprite;
        upImg.color = Color.white;
        upImg.raycastTarget = false;
        upObj.SetActive(false);

        sRoot.SetActive(false);

        TowerDefGridCell cellComp = cellObj.GetComponent<TowerDefGridCell>();
        cellComp.SetupCell(r, c, slotImg, btn, sRoot, sImg, gunRt, upObj);
        return cellComp;
    }

    private TowerDefUIController BuildTopBarAndUI(Transform canvasTr)
    {
        GameObject uiObj = new GameObject("TowerDefUI", typeof(RectTransform), typeof(TowerDefUIController));
        uiObj.transform.SetParent(canvasTr, false);
        RectTransform uiRt = uiObj.GetComponent<RectTransform>();
        uiRt.anchorMin = Vector2.zero;
        uiRt.anchorMax = Vector2.one;
        uiRt.offsetMin = uiRt.offsetMax = Vector2.zero;

        // Top Bar
        GameObject topBar = new GameObject("TopBar", typeof(RectTransform));
        topBar.transform.SetParent(uiObj.transform, false);
        RectTransform topRt = topBar.GetComponent<RectTransform>();
        topRt.anchorMin = new Vector2(0f, 1f);
        topRt.anchorMax = new Vector2(1f, 1f);
        topRt.pivot = new Vector2(0.5f, 1f);
        topRt.anchoredPosition = Vector2.zero;
        topRt.sizeDelta = new Vector2(0f, 120f);

        // Nút Back đỏ (<)
        GameObject backBtnObj = new GameObject("BackButton", typeof(RectTransform), typeof(Image), typeof(Button));
        backBtnObj.transform.SetParent(topBar.transform, false);
        RectTransform backRt = backBtnObj.GetComponent<RectTransform>();
        backRt.anchorMin = new Vector2(0f, 0.5f);
        backRt.anchorMax = new Vector2(0f, 0.5f);
        backRt.pivot = new Vector2(0f, 0.5f);
        backRt.anchoredPosition = new Vector2(30f, -15f);
        backRt.sizeDelta = new Vector2(85f, 85f);
        Image backImg = backBtnObj.GetComponent<Image>();
        if (backArrowSprite != null) backImg.sprite = backArrowSprite;
        backImg.preserveAspect = true;
        Button backBtn = backBtnObj.GetComponent<Button>();

        // Nhóm Coin (🪙 50)
        GameObject coinGrp = new GameObject("CoinGroup", typeof(RectTransform));
        coinGrp.transform.SetParent(topBar.transform, false);
        RectTransform coinGrpRt = coinGrp.GetComponent<RectTransform>();
        coinGrpRt.anchorMin = new Vector2(0.28f, 0.5f);
        coinGrpRt.anchorMax = new Vector2(0.28f, 0.5f);
        coinGrpRt.pivot = new Vector2(0f, 0.5f);
        coinGrpRt.anchoredPosition = new Vector2(0f, -15f);
        coinGrpRt.sizeDelta = new Vector2(180f, 60f);

        GameObject coinIconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        coinIconObj.transform.SetParent(coinGrp.transform, false);
        RectTransform cIconRt = coinIconObj.GetComponent<RectTransform>();
        cIconRt.anchorMin = new Vector2(0f, 0.5f);
        cIconRt.anchorMax = new Vector2(0f, 0.5f);
        cIconRt.pivot = new Vector2(0f, 0.5f);
        cIconRt.anchoredPosition = Vector2.zero;
        cIconRt.sizeDelta = new Vector2(50f, 50f);
        Image cIcon = coinIconObj.GetComponent<Image>();
        if (coinIconSprite != null) cIcon.sprite = coinIconSprite;
        cIcon.preserveAspect = true;

        GameObject coinTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        coinTxtObj.transform.SetParent(coinGrp.transform, false);
        RectTransform cTxtRt = coinTxtObj.GetComponent<RectTransform>();
        cTxtRt.anchorMin = new Vector2(0f, 0.5f);
        cTxtRt.anchorMax = new Vector2(1f, 0.5f);
        cTxtRt.pivot = new Vector2(0f, 0.5f);
        cTxtRt.anchoredPosition = new Vector2(62f, 0f);
        cTxtRt.sizeDelta = new Vector2(120f, 55f);
        TextMeshProUGUI cTxt = coinTxtObj.GetComponent<TextMeshProUGUI>();
        cTxt.text = "50";
        cTxt.fontSize = 42f;
        cTxt.fontStyle = FontStyles.Bold;
        cTxt.color = Color.white;
        cTxt.alignment = TextAlignmentOptions.MidlineLeft;

        // Nhóm Energy (⚡ 0)
        GameObject energyGrp = new GameObject("EnergyGroup", typeof(RectTransform));
        energyGrp.transform.SetParent(topBar.transform, false);
        RectTransform energyGrpRt = energyGrp.GetComponent<RectTransform>();
        energyGrpRt.anchorMin = new Vector2(0.50f, 0.5f);
        energyGrpRt.anchorMax = new Vector2(0.50f, 0.5f);
        energyGrpRt.pivot = new Vector2(0f, 0.5f);
        energyGrpRt.anchoredPosition = new Vector2(0f, -15f);
        energyGrpRt.sizeDelta = new Vector2(180f, 60f);

        GameObject energyIconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        energyIconObj.transform.SetParent(energyGrp.transform, false);
        RectTransform eIconRt = energyIconObj.GetComponent<RectTransform>();
        eIconRt.anchorMin = new Vector2(0f, 0.5f);
        eIconRt.anchorMax = new Vector2(0f, 0.5f);
        eIconRt.pivot = new Vector2(0f, 0.5f);
        eIconRt.anchoredPosition = Vector2.zero;
        eIconRt.sizeDelta = new Vector2(36f, 56f);
        Image eIcon = energyIconObj.GetComponent<Image>();
        if (energyIconSprite != null) eIcon.sprite = energyIconSprite;
        eIcon.preserveAspect = true;

        GameObject energyTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        energyTxtObj.transform.SetParent(energyGrp.transform, false);
        RectTransform eTxtRt = energyTxtObj.GetComponent<RectTransform>();
        eTxtRt.anchorMin = new Vector2(0f, 0.5f);
        eTxtRt.anchorMax = new Vector2(1f, 0.5f);
        eTxtRt.pivot = new Vector2(0f, 0.5f);
        eTxtRt.anchoredPosition = new Vector2(50f, 0f);
        eTxtRt.sizeDelta = new Vector2(120f, 55f);
        TextMeshProUGUI eTxt = energyTxtObj.GetComponent<TextMeshProUGUI>();
        eTxt.text = "0";
        eTxt.fontSize = 42f;
        eTxt.fontStyle = FontStyles.Bold;
        eTxt.color = Color.white;
        eTxt.alignment = TextAlignmentOptions.MidlineLeft;

        TowerDefUIController ctrl = uiObj.GetComponent<TowerDefUIController>();
        ctrl.SetupTopBar(backBtn, cTxt, eTxt);
        return ctrl;
    }

    private float GetColumnWorldX(int colIndex)
    {
        float cellWidth = 1080f / 7f;
        // colIndex = 0 là ngoài cùng bên trái (-3 * cellWidth), colIndex = 3 là giữa (0), colIndex = 6 là phải (+3 * cellWidth)
        return (colIndex - 3) * cellWidth;
    }

    private void LoadAssetReferences()
    {
        if (darkTileSprite == null) darkTileSprite = Resources.Load<Sprite>("TowerDef/Tile_Floor_Dark");
        if (wallStripSprite == null) wallStripSprite = Resources.Load<Sprite>("TowerDef/Wall_Brick_Strip");
        if (gateSprite == null) gateSprite = Resources.Load<Sprite>("TowerDef/Gate_Metal");
        if (plusTileSprite == null) plusTileSprite = Resources.Load<Sprite>("TowerDef/Tile_Placement_Plus");

        if (turretBaseSprite == null) turretBaseSprite = Resources.Load<Sprite>("TowerDef/Turret_Base_01_Cyan");
        if (turretGunSprite == null) turretGunSprite = Resources.Load<Sprite>("TowerDef/Turret_Gun_01_Cyan");
        if (pawnTowerSprite == null) pawnTowerSprite = Resources.Load<Sprite>("TowerDef/Pawn_Tower_03_Purple");
        if (corePodSprite == null) corePodSprite = Resources.Load<Sprite>("TowerDef/Core_Pod_Green");

        if (upgradeCircleSprite == null) upgradeCircleSprite = Resources.Load<Sprite>("TowerDef/Icon_Upgrade_Circle");
        if (greenBarSprite == null) greenBarSprite = Resources.Load<Sprite>("TowerDef/Bar_Green");
        if (backArrowSprite == null) backArrowSprite = Resources.Load<Sprite>("TowerDef/Btn_Arrow_Back");
        if (coinIconSprite == null) coinIconSprite = Resources.Load<Sprite>("TowerDef/Icon_Coin");
        if (energyIconSprite == null) energyIconSprite = Resources.Load<Sprite>("TowerDef/Icon_Energy");

        if (creepSprite == null) creepSprite = Resources.Load<Sprite>("TowerDef/Creep_Mine");
        if (bossSprite == null) bossSprite = Resources.Load<Sprite>("TowerDef/Boss_Mine");

#if UNITY_EDITOR
        string tilesDir = "Assets/Sprites/Mini game/Sliced/Tiles/";
        string uiDir = "Assets/Sprites/Mini game/Sliced/UI/";
        string towersDir = "Assets/Sprites/Mini game/Sliced/Towers/";
        string monstersDir = "Assets/Sprites/Mini game/Sliced/Monsters/";

        if (darkTileSprite == null)
        {
            darkTileSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Mini game/nền map thủ thành.png");
            if (darkTileSprite == null) darkTileSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(tilesDir + "Tile_Floor_Dark.png");
        }
        if (wallStripSprite == null) wallStripSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(tilesDir + "Wall_Brick_Strip.png");
        if (gateSprite == null) gateSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(tilesDir + "Gate_Metal.png");
        if (plusTileSprite == null) plusTileSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(tilesDir + "Tile_Placement_Plus.png");

        if (turretBaseSprite == null) turretBaseSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(towersDir + "Turret_Base_01_Cyan.png");
        if (turretGunSprite == null) turretGunSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(towersDir + "Turret_Gun_01_Cyan.png");
        if (pawnTowerSprite == null) pawnTowerSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(towersDir + "Pawn_Tower_03_Purple.png");
        if (corePodSprite == null) corePodSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(towersDir + "Core_Pod_Green.png");

        if (upgradeCircleSprite == null) upgradeCircleSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(uiDir + "Icon_Upgrade_Circle.png");
        if (greenBarSprite == null) greenBarSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(uiDir + "Bar_Green.png");
        if (backArrowSprite == null) backArrowSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(uiDir + "Btn_Arrow_Back.png");
        if (coinIconSprite == null) coinIconSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(uiDir + "Icon_Coin.png");
        if (energyIconSprite == null) energyIconSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(uiDir + "Icon_Energy.png");

        if (creepSprite == null) creepSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(monstersDir + "Creep_Mine.png");
        if (bossSprite == null) bossSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(monstersDir + "Boss_Mine.png");
#endif
    }
    #endregion
}
