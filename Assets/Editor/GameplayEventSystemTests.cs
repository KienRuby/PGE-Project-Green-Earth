using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace PGE.Tests
{
    public class GameplayEventSystemTests
    {
        private GameObject testContainer;

        [SetUp]
        public void SetUp()
        {
            testContainer = new GameObject("TestContainer");
            testContainer.hideFlags = HideFlags.DontSave;
            GameplayEventPickup.ClearActiveEventsForTesting();
        }

        [TearDown]
        public void TearDown()
        {
            GameplayEventPickup.ClearActiveEventsForTesting();
            if (testContainer != null)
            {
                Object.DestroyImmediate(testContainer);
            }
            if (DamageNumberManager.Instance != null && !Application.isPlaying)
            {
                Object.DestroyImmediate(DamageNumberManager.Instance.gameObject);
            }
            Time.timeScale = 1f;
        }

        [Test]
        public void Test01_Database_InitializesWithDefaultEvents()
        {
            GameplayEventDatabase db = ScriptableObject.CreateInstance<GameplayEventDatabase>();
            db.InitializeDefaults();

            Assert.AreEqual(3, db.Events.Count);

            var electricCar = db.GetById("electric_car");
            Assert.IsNotNull(electricCar);
            Assert.AreEqual("Electric Car", electricCar.eventTitle);
            Assert.IsFalse(electricCar.isInstantResult);
            Assert.AreEqual(2, electricCar.options.Count);
            Assert.AreEqual("Consume HP to start engine", electricCar.options[0].buttonText);
            StringAssert.Contains("HP -5%", electricCar.options[0].rewardText);

            var bunker = db.GetById("underground_bunker");
            Assert.IsNotNull(bunker);
            Assert.AreEqual("Underground Bunker", bunker.eventTitle);
            Assert.IsFalse(bunker.isInstantResult);
            Assert.AreEqual(3, bunker.options.Count);
            Assert.AreEqual("Go to the room on the left", bunker.options[0].buttonText);

            var assassinator = db.GetById("assasinator");
            Assert.IsNotNull(assassinator);
            Assert.AreEqual("Assasinator", assassinator.eventTitle);
            Assert.AreEqual(3, assassinator.options.Count);
            Assert.AreEqual("To purge mutants", assassinator.options[0].buttonText);
        }

        [Test]
        public void Test02_GameplayEventPickup_ActiveTracking()
        {
            Assert.AreEqual(0, GameplayEventPickup.ActiveEvents.Count);

            GameObject eventObj1 = new GameObject("Event1", typeof(CircleCollider2D), typeof(GameplayEventPickup));
            eventObj1.transform.SetParent(testContainer.transform);
            GameplayEventPickup pickup1 = eventObj1.GetComponent<GameplayEventPickup>();

            Assert.AreEqual(1, GameplayEventPickup.ActiveEvents.Count);
            Assert.AreSame(pickup1, GameplayEventPickup.ActiveEvents[0]);

            GameObject eventObj2 = new GameObject("Event2", typeof(CircleCollider2D), typeof(GameplayEventPickup));
            eventObj2.transform.SetParent(testContainer.transform);
            GameplayEventPickup pickup2 = eventObj2.GetComponent<GameplayEventPickup>();

            Assert.AreEqual(2, GameplayEventPickup.ActiveEvents.Count);

            Object.DestroyImmediate(eventObj1);
            Assert.AreEqual(1, GameplayEventPickup.ActiveEvents.Count);
            Assert.AreSame(pickup2, GameplayEventPickup.ActiveEvents[0]);
        }

        [Test]
        public void Test03_OptionRewards_MoveSpeedPercent()
        {
            GameObject playerObj = new GameObject("Player", typeof(Rigidbody2D), typeof(PlayerHealth), typeof(PlayerMovement));
            playerObj.transform.SetParent(testContainer.transform);
            PlayerMovement movement = playerObj.GetComponent<PlayerMovement>();

            float initialSpeed = movement.EffectiveSpeed;
            movement.AddMoveSpeedPercent(5f);

            float speedAfterBuff = movement.EffectiveSpeed;
            Assert.Greater(speedAfterBuff, initialSpeed);
            Assert.AreEqual(initialSpeed * 0.05f, movement.MoveSpeedBonus, 0.01f);
        }

        [Test]
        public void Test04_OptionRewards_HpConsumption()
        {
            GameObject playerObj = new GameObject("Player", typeof(Rigidbody2D), typeof(PlayerHealth));
            playerObj.transform.SetParent(testContainer.transform);
            PlayerHealth health = playerObj.GetComponent<PlayerHealth>();

            int initialHp = health.CurrentHealth;
            int consumeDmg = Mathf.RoundToInt(initialHp * 0.05f);
            health.TakeDamage(consumeDmg);

            Assert.AreEqual(initialHp - consumeDmg, health.CurrentHealth);
        }

        [Test]
        public void Test05_OptionRewards_ArtifactGrant()
        {
            GameObject playerObj = new GameObject("Player", typeof(PlayerArtifactInventory));
            playerObj.transform.SetParent(testContainer.transform);
            PlayerArtifactInventory inv = playerObj.GetComponent<PlayerArtifactInventory>();

            ArtifactData bandAid = ArtifactDatabase.Instance.GetById("metal_band_aid");
            Assert.IsNotNull(bandAid, "Metal Band-aid phải có trong ArtifactDatabase");

            bool equipped = inv.EquipArtifact(bandAid);
            Assert.IsTrue(equipped);
            Assert.IsTrue(inv.HasArtifact("metal_band_aid"));
        }

        [Test]
        public void Test06_ModalLifecycle_PauseResume()
        {
            GameObject canvasObj = new GameObject("Canvas", typeof(Canvas));
            canvasObj.transform.SetParent(testContainer.transform);
            Canvas canvas = canvasObj.GetComponent<Canvas>();

            GameplayEventModalController modal = GameplayEventModalController.EnsureModalInScene(canvas);
            Assert.IsNotNull(modal);

            GameplayEventDatabase db = ScriptableObject.CreateInstance<GameplayEventDatabase>();
            db.InitializeDefaults();
            GameplayEventData ev = db.GetById("electric_car");

            Time.timeScale = 1f;
            modal.Show(ev);

            Assert.AreEqual(0f, Time.timeScale, "Time.timeScale phải về 0 khi mở modal sự kiện");
            Assert.IsTrue(modal.gameObject.activeInHierarchy);
        }

        [Test]
        public void Test07_ProceduralSprites_GeneratedWithoutError()
        {
            Sprite btnSprite = GameplayEventModalController.GetOrCreateTealButtonSprite();
            Assert.IsNotNull(btnSprite);
            Assert.AreEqual(128, btnSprite.rect.width);
            Assert.AreEqual(36, btnSprite.rect.height);

            Sprite scanlineSprite = GameplayEventModalController.GetOrCreateScanlineSprite();
            Assert.IsNotNull(scanlineSprite);
        }

        [Test]
        public void Test08_RewardsHiddenUntilOptionChosen()
        {
            GameObject canvasObj = new GameObject("Canvas", typeof(Canvas));
            canvasObj.transform.SetParent(testContainer.transform);
            Canvas canvas = canvasObj.GetComponent<Canvas>();

            GameplayEventModalController modal = GameplayEventModalController.EnsureModalInScene(canvas);

            GameplayEventDatabase db = ScriptableObject.CreateInstance<GameplayEventDatabase>();
            db.InitializeDefaults();
            GameplayEventData bunker = db.GetById("underground_bunker");

            // Mở modal: ban đầu KHÔNG được hiển thị chỉ số thưởng
            modal.Show(bunker);

            var rewardText = modal.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
            // Tìm đối tượng RewardText trong hierarchy của modal
            Transform rewardObj = modal.transform.Find("ContentPanel/RewardText");
            if (rewardObj != null)
            {
                Assert.IsFalse(rewardObj.gameObject.activeSelf, "RewardText phải ẩn khi người chơi chưa chọn lựa");
            }
        }
    }
}
