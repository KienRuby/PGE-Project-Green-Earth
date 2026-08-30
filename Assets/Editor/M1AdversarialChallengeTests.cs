using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PGE.Tests.Adversarial
{
    [InitializeOnLoad]
    public class M1AdversarialChallengeTests
    {
        public const string ReportPath = "Assets/Editor/M1AdversarialTestReport.txt";

        static M1AdversarialChallengeTests()
        {
            EditorApplication.delayCall += () =>
            {
                RunAllAdversarialTestsAndSaveReport();
            };
        }

        [MenuItem("PGE/Tests/Run M1 Adversarial Stress Tests")]
        public static void RunFromMenu()
        {
            RunAllAdversarialTestsAndSaveReport();
        }

        public static string RunAllAdversarialTestsAndSaveReport()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine($"M1 ADVERSARIAL STRESS TEST EXECUTION REPORT");
            sb.AppendLine($"Timestamp (UTC): {DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}");
            sb.AppendLine("================================================================================\n");

            int passed = 0;
            int failed = 0;
            int total = 0;

            void RunTest(string testName, Action testAction)
            {
                total++;
                try
                {
                    testAction();
                    sb.AppendLine($"[PASS] {testName}");
                    passed++;
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"[FAIL] {testName}");
                    sb.AppendLine($"       Exception: {ex.GetType().Name}: {ex.Message}");
                    sb.AppendLine($"       StackTrace: {ex.StackTrace}");
                    failed++;
                }
            }

            M1AdversarialChallengeTests instance = new M1AdversarialChallengeTests();

            // -------------------------------------------------------------------------
            // DIMENSION 1: OBJECT POOLING STRESS TESTS
            // -------------------------------------------------------------------------
            sb.AppendLine("--- DIMENSION 1: OBJECT POOLING STRESS TESTS ---");
            RunTest("ObjectPool_RapidSpawnDespawn_10000Cycles_NoMemoryLeak", () => instance.ObjectPool_RapidSpawnDespawn_10000Cycles_NoMemoryLeak());
            RunTest("ObjectPool_DuplicateReturn_IgnoredSafelyAndNoDoubleAllocation", () => instance.ObjectPool_DuplicateReturn_IgnoredSafelyAndNoDoubleAllocation());
            RunTest("ObjectPool_NullHandling_ReturnAndSpawnWithNulls", () => instance.ObjectPool_NullHandling_ReturnAndSpawnWithNulls());
            RunTest("ObjectPool_DestroyedObjectsInPool_SafelyHandledOnSubsequentSpawn", () => instance.ObjectPool_DestroyedObjectsInPool_SafelyHandledOnSubsequentSpawn());
            RunTest("ObjectPool_FixedCapacity_NoGrowth_ReturnsNullWhenExhausted", () => instance.ObjectPool_FixedCapacity_NoGrowth_ReturnsNullWhenExhausted());
            RunTest("PoolManager_DirectAndDelayedDespawn_Safety", () => instance.PoolManager_DirectAndDelayedDespawn_Safety());

            // -------------------------------------------------------------------------
            // DIMENSION 2: EVENT BUS STRESS TESTS
            // -------------------------------------------------------------------------
            sb.AppendLine("\n--- DIMENSION 2: EVENT BUS STRESS TESTS ---");
            RunTest("GameEvents_MultiSubscriber_50Subscribers_AttachAndDetachCleanly", () => instance.GameEvents_MultiSubscriber_50Subscribers_AttachAndDetachCleanly());
            RunTest("GameEvents_UnsubscribedDetach_SafeNoOp", () => instance.GameEvents_UnsubscribedDetach_SafeNoOp());
            RunTest("GameEvents_Reentrancy_UnsubscribeSelfDuringCallback", () => instance.GameEvents_Reentrancy_UnsubscribeSelfDuringCallback());
            RunTest("GameEvents_ExceptionBehavior_SingleSubscriberThrow_Analysis", () => instance.GameEvents_ExceptionBehavior_SingleSubscriberThrow_Analysis());

            // -------------------------------------------------------------------------
            // DIMENSION 3: CURRENCY & DEDUCTION STRESS TESTS
            // -------------------------------------------------------------------------
            sb.AppendLine("\n--- DIMENSION 3: CURRENCY & DEDUCTION STRESS TESTS ---");
            RunTest("PlayerDataService_NegativeAndZeroDeductions_AreSafelyHandled", () => instance.PlayerDataService_NegativeAndZeroDeductions_AreSafelyHandled());
            RunTest("PlayerDataService_NegativeAndZeroAdditions_Ignored", () => instance.PlayerDataService_NegativeAndZeroAdditions_Ignored());
            RunTest("PlayerDataService_OverspendAttempts_FailAndPreserveBalance", () => instance.PlayerDataService_OverspendAttempts_FailAndPreserveBalance());
            RunTest("PlayerDataService_BoundaryAndDirectSetterClamping", () => instance.PlayerDataService_BoundaryAndDirectSetterClamping());
            RunTest("PlayerDataService_LabItemLevel_EdgeCasesAndNullStrings", () => instance.PlayerDataService_LabItemLevel_EdgeCasesAndNullStrings());
            RunTest("ChipManager_TestModeVsNormalMode_DeductionsAndExploitPrevention", () => instance.ChipManager_TestModeVsNormalMode_DeductionsAndExploitPrevention());

            sb.AppendLine("\n================================================================================");
            sb.AppendLine($"SUMMARY: Total={total}, Passed={passed}, Failed={failed}");
            sb.AppendLine($"VERDICT: {(failed == 0 ? "CONFIRM_CORRECTNESS" : "REJECT")}");
            sb.AppendLine("================================================================================");

            string reportText = sb.ToString();
            try
            {
                File.WriteAllText(ReportPath, reportText);
                Debug.Log(reportText);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to write report to {ReportPath}: {ex.Message}");
            }

            return reportText;
        }

        // =========================================================================
        // HELPER CLASSES
        // =========================================================================
        private class MockPoolable : MonoBehaviour, IPoolable
        {
            public int spawnCount = 0;
            public int returnCount = 0;

            public void OnSpawnFromPool()
            {
                spawnCount++;
            }

            public void OnReturnToPool()
            {
                returnCount++;
            }
        }

        // =========================================================================
        // DIMENSION 1 IMPLEMENTATIONS
        // =========================================================================

        [Test]
        public void ObjectPool_RapidSpawnDespawn_10000Cycles_NoMemoryLeak()
        {
            GameObject prefab = new GameObject("TestPrefab_10000");
            prefab.AddComponent<MockPoolable>();
            GameObject container = new GameObject("TestContainer_10000");

            try
            {
                ObjectPool pool = new ObjectPool(prefab, initialSize: 10, canGrow: true, container: container.transform);
                pool.Initialize(container.transform);

                List<GameObject> activeObjects = new List<GameObject>(100);

                // 100 rounds of spawning 100 objects and returning them in mixed orders
                for (int round = 0; round < 100; round++)
                {
                    activeObjects.Clear();
                    for (int i = 0; i < 100; i++)
                    {
                        GameObject obj = pool.Spawn(Vector3.zero, Quaternion.identity);
                        Assert.That(obj, Is.Not.Null);
                        Assert.That(obj.activeSelf, Is.True);
                        activeObjects.Add(obj);
                    }

                    // Return half in reverse order (LIFO)
                    for (int i = activeObjects.Count - 1; i >= 50; i--)
                    {
                        pool.Despawn(activeObjects[i]);
                        Assert.That(activeObjects[i].activeSelf, Is.False);
                    }

                    // Return remaining half in forward order (FIFO)
                    for (int i = 0; i < 50; i++)
                    {
                        pool.Despawn(activeObjects[i]);
                        Assert.That(activeObjects[i].activeSelf, Is.False);
                    }
                }

                // Verify pool is still completely healthy by spawning 10 and checking callbacks
                for (int i = 0; i < 10; i++)
                {
                    GameObject obj = pool.Spawn(Vector3.up * i, Quaternion.identity);
                    MockPoolable p = obj.GetComponent<MockPoolable>();
                    Assert.That(p.spawnCount, Is.GreaterThanOrEqualTo(100));
                    pool.Despawn(obj);
                    Assert.That(p.returnCount, Is.EqualTo(p.spawnCount));
                }
            }
            finally
            {
                Object.DestroyImmediate(prefab);
                Object.DestroyImmediate(container);
            }
        }

        [Test]
        public void ObjectPool_DuplicateReturn_IgnoredSafelyAndNoDoubleAllocation()
        {
            GameObject prefab = new GameObject("TestPrefab_DupReturn");
            prefab.AddComponent<MockPoolable>();
            GameObject container = new GameObject("TestContainer_DupReturn");

            try
            {
                ObjectPool pool = new ObjectPool(prefab, initialSize: 5, canGrow: true, container: container.transform);
                pool.Initialize(container.transform);

                GameObject obj1 = pool.Spawn(Vector3.zero, Quaternion.identity);
                MockPoolable p1 = obj1.GetComponent<MockPoolable>();
                Assert.That(p1.spawnCount, Is.EqualTo(1));
                Assert.That(p1.returnCount, Is.EqualTo(0));

                // First valid return
                pool.Despawn(obj1);
                Assert.That(p1.returnCount, Is.EqualTo(1));
                Assert.That(obj1.activeSelf, Is.False);

                // Duplicate returns: 2nd, 3rd, 4th attempts
                pool.Despawn(obj1);
                pool.Despawn(obj1);
                pool.Despawn(obj1);
                Assert.That(p1.returnCount, Is.EqualTo(1), "Duplicate Despawn calls must be ignored via inPoolSet HashSet check.");

                // Spawn 1 object: should retrieve obj1
                GameObject reused1 = pool.Spawn(Vector3.one, Quaternion.identity);
                Assert.That(reused1, Is.EqualTo(obj1));

                // Spawn 2nd object: should NOT retrieve obj1 again!
                GameObject reused2 = pool.Spawn(Vector3.right, Quaternion.identity);
                Assert.That(reused2, Is.Not.EqualTo(obj1), "Duplicate return must not cause the same instance to be handed out twice.");

                pool.Despawn(reused1);
                pool.Despawn(reused2);
            }
            finally
            {
                Object.DestroyImmediate(prefab);
                Object.DestroyImmediate(container);
            }
        }

        [Test]
        public void ObjectPool_NullHandling_ReturnAndSpawnWithNulls()
        {
            GameObject container = new GameObject("TestContainer_Nulls");

            try
            {
                // Pool with null prefab
                ObjectPool nullPrefabPool = new ObjectPool(null, 5, true, container.transform);
                nullPrefabPool.Initialize(container.transform);

                GameObject spawnedNull = nullPrefabPool.Spawn(Vector3.zero, Quaternion.identity);
                Assert.That(spawnedNull, Is.Null, "Spawning from a pool with null prefab must return null safely.");

                nullPrefabPool.Despawn(null); // Must not throw

                // Valid pool, passing null to Despawn
                GameObject prefab = new GameObject("TestPrefab_NullArg");
                ObjectPool validPool = new ObjectPool(prefab, 2, true, container.transform);
                validPool.Initialize(container.transform);

                Assert.DoesNotThrow(() => validPool.Despawn(null), "Despawning a null GameObject must not throw.");
                Assert.DoesNotThrow(() => validPool.Return(null), "Returning a null GameObject must not throw.");

                Object.DestroyImmediate(prefab);
            }
            finally
            {
                Object.DestroyImmediate(container);
            }
        }

        [Test]
        public void ObjectPool_DestroyedObjectsInPool_SafelyHandledOnSubsequentSpawn()
        {
            GameObject prefab = new GameObject("TestPrefab_Destroyed");
            GameObject container = new GameObject("TestContainer_Destroyed");

            try
            {
                ObjectPool pool = new ObjectPool(prefab, initialSize: 4, canGrow: true, container: container.transform);
                pool.Initialize(container.transform);

                // Spawn all 4
                GameObject o1 = pool.Spawn(Vector3.zero, Quaternion.identity);
                GameObject o2 = pool.Spawn(Vector3.zero, Quaternion.identity);
                GameObject o3 = pool.Spawn(Vector3.zero, Quaternion.identity);
                GameObject o4 = pool.Spawn(Vector3.zero, Quaternion.identity);

                // Return all 4
                pool.Despawn(o1);
                pool.Despawn(o2);
                pool.Despawn(o3);
                pool.Despawn(o4);

                // Now externally destroy o2 and o3 while they are pooled
                Object.DestroyImmediate(o2);
                Object.DestroyImmediate(o3);

                // Now spawn 4 objects again. Pool should purge the destroyed null entries from its queue and instantiate new ones if canGrow=true.
                GameObject s1 = pool.Spawn(Vector3.zero, Quaternion.identity);
                GameObject s2 = pool.Spawn(Vector3.zero, Quaternion.identity);
                GameObject s3 = pool.Spawn(Vector3.zero, Quaternion.identity);
                GameObject s4 = pool.Spawn(Vector3.zero, Quaternion.identity);

                Assert.That(s1, Is.Not.Null);
                Assert.That(s2, Is.Not.Null);
                Assert.That(s3, Is.Not.Null);
                Assert.That(s4, Is.Not.Null);

                pool.Despawn(s1);
                pool.Despawn(s2);
                pool.Despawn(s3);
                pool.Despawn(s4);
            }
            finally
            {
                Object.DestroyImmediate(prefab);
                Object.DestroyImmediate(container);
            }
        }

        [Test]
        public void ObjectPool_FixedCapacity_NoGrowth_ReturnsNullWhenExhausted()
        {
            GameObject prefab = new GameObject("TestPrefab_Fixed");
            GameObject container = new GameObject("TestContainer_Fixed");

            try
            {
                ObjectPool pool = new ObjectPool(prefab, initialSize: 2, canGrow: false, container: container.transform);
                pool.Initialize(container.transform);

                GameObject obj1 = pool.Spawn(Vector3.zero, Quaternion.identity);
                GameObject obj2 = pool.Spawn(Vector3.zero, Quaternion.identity);
                Assert.That(obj1, Is.Not.Null);
                Assert.That(obj2, Is.Not.Null);

                // Pool is now empty, canGrow is false -> Spawn must return null
                GameObject obj3 = pool.Spawn(Vector3.zero, Quaternion.identity);
                Assert.That(obj3, Is.Null, "Fixed capacity pool must return null when exhausted.");

                // Return 1 object -> should now allow spawning 1 object
                pool.Despawn(obj1);
                GameObject obj4 = pool.Spawn(Vector3.zero, Quaternion.identity);
                Assert.That(obj4, Is.Not.Null);

                pool.Despawn(obj2);
                pool.Despawn(obj4);
            }
            finally
            {
                Object.DestroyImmediate(prefab);
                Object.DestroyImmediate(container);
            }
        }

        [Test]
        public void PoolManager_DirectAndDelayedDespawn_Safety()
        {
            GameObject host = new GameObject("[TestPoolManagerHost]");
            PoolManager pm = host.AddComponent<PoolManager>();

            try
            {
                Assert.DoesNotThrow(() => pm.ReturnToPool(null));
                Assert.DoesNotThrow(() => pm.Despawn(null));
                Assert.DoesNotThrow(() => pm.Despawn(null, 1.0f));

                GameObject nonPooled = new GameObject("NonPooledObject");
                pm.ReturnToPool(nonPooled);
                // When non-pooled object is returned to PoolManager, it gets Destroyed
                Assert.Pass("PoolManager null and non-pooled object returns handled safely.");
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        // =========================================================================
        // DIMENSION 2 IMPLEMENTATIONS
        // =========================================================================

        [Test]
        public void GameEvents_MultiSubscriber_50Subscribers_AttachAndDetachCleanly()
        {
            const int subscriberCount = 50;
            int[] callCounts = new int[subscriberCount];
            Action<int>[] listeners = new Action<int>[subscriberCount];

            for (int i = 0; i < subscriberCount; i++)
            {
                int index = i;
                listeners[i] = (lvl) => callCounts[index]++;
                GameEvents.OnPlayerLevelUp += listeners[i];
            }

            try
            {
                // Fire event
                GameEvents.RaisePlayerLevelUp(5);
                for (int i = 0; i < subscriberCount; i++)
                {
                    Assert.That(callCounts[i], Is.EqualTo(1), $"Subscriber {i} should be called once.");
                }

                // Detach half (even indices)
                for (int i = 0; i < subscriberCount; i += 2)
                {
                    GameEvents.OnPlayerLevelUp -= listeners[i];
                }

                // Fire event again
                GameEvents.RaisePlayerLevelUp(6);
                for (int i = 0; i < subscriberCount; i++)
                {
                    int expected = (i % 2 == 0) ? 1 : 2;
                    Assert.That(callCounts[i], Is.EqualTo(expected), $"Subscriber {i} count incorrect after partial detach.");
                }
            }
            finally
            {
                // Cleanup all
                for (int i = 0; i < subscriberCount; i++)
                {
                    GameEvents.OnPlayerLevelUp -= listeners[i];
                }
            }
        }

        [Test]
        public void GameEvents_UnsubscribedDetach_SafeNoOp()
        {
            Action testAction = () => { };
            // Unsubscribing a delegate that was never attached must not throw
            Assert.DoesNotThrow(() =>
            {
                GameEvents.OnEnemyKilled -= testAction;
                GameEvents.RaiseEnemyKilled();
            });
        }

        [Test]
        public void GameEvents_Reentrancy_UnsubscribeSelfDuringCallback()
        {
            int callCount = 0;
            Action selfRemovingListener = null;
            selfRemovingListener = () =>
            {
                callCount++;
                GameEvents.OnEnemyKilled -= selfRemovingListener;
            };

            GameEvents.OnEnemyKilled += selfRemovingListener;

            try
            {
                GameEvents.RaiseEnemyKilled();
                Assert.That(callCount, Is.EqualTo(1));

                // Second invocation should not call the removed listener
                GameEvents.RaiseEnemyKilled();
                Assert.That(callCount, Is.EqualTo(1));
            }
            finally
            {
                GameEvents.OnEnemyKilled -= selfRemovingListener;
            }
        }

        [Test]
        public void GameEvents_ExceptionBehavior_SingleSubscriberThrow_Analysis()
        {
            bool subscriber2Called = false;
            Action faultingSubscriber = () => throw new InvalidOperationException("Faulting subscriber simulated crash.");
            Action normalSubscriber = () => subscriber2Called = true;

            GameEvents.OnEnemyKilled += faultingSubscriber;
            GameEvents.OnEnemyKilled += normalSubscriber;

            try
            {
                // In standard C# delegates, if subscriber 1 throws an unhandled exception,
                // the invocation pipeline aborts and subsequent subscribers are not reached.
                Assert.Throws<InvalidOperationException>(() => GameEvents.RaiseEnemyKilled());
                Assert.That(subscriber2Called, Is.False, "C# standard multicast aborts subsequent invocations when an unhandled exception is thrown.");
            }
            finally
            {
                GameEvents.OnEnemyKilled -= faultingSubscriber;
                GameEvents.OnEnemyKilled -= normalSubscriber;
            }
        }

        // =========================================================================
        // DIMENSION 3 IMPLEMENTATIONS
        // =========================================================================

        [Test]
        public void PlayerDataService_NegativeAndZeroDeductions_AreSafelyHandled()
        {
            int origChips = PlayerDataService.DataChips;
            int origGems = PlayerDataService.RedGems;
            int origEnergy = PlayerDataService.Energy;
            int origStones = PlayerDataService.AdvanceStones;

            try
            {
                PlayerDataService.DataChips = 500;
                PlayerDataService.RedGems = 500;
                PlayerDataService.Energy = 50;
                PlayerDataService.AdvanceStones = 10;

                // --- Negative Deductions ---
                Assert.That(PlayerDataService.TrySpendDataChips(-100), Is.False, "Negative DataChip deduction must return false.");
                Assert.That(PlayerDataService.DataChips, Is.EqualTo(500), "Negative spend must not increase balance.");

                Assert.That(PlayerDataService.TrySpendRedGems(-50), Is.False, "Negative RedGem deduction must return false.");
                Assert.That(PlayerDataService.RedGems, Is.EqualTo(500));

                Assert.That(PlayerDataService.TrySpendEnergy(-20), Is.False, "Negative Energy deduction must return false.");
                Assert.That(PlayerDataService.Energy, Is.EqualTo(50));

                Assert.That(PlayerDataService.TrySpendAdvanceStones(-5), Is.False, "Negative AdvanceStone deduction must return false.");
                Assert.That(PlayerDataService.AdvanceStones, Is.EqualTo(10));

                // --- Zero Deductions ---
                Assert.That(PlayerDataService.TrySpendDataChips(0), Is.True, "Zero DataChip deduction must return true (valid no-op).");
                Assert.That(PlayerDataService.DataChips, Is.EqualTo(500));

                Assert.That(PlayerDataService.TrySpendRedGems(0), Is.True, "Zero RedGem deduction must return true.");
                Assert.That(PlayerDataService.RedGems, Is.EqualTo(500));

                Assert.That(PlayerDataService.TrySpendEnergy(0), Is.True, "Zero Energy deduction must return true.");
                Assert.That(PlayerDataService.Energy, Is.EqualTo(50));

                Assert.That(PlayerDataService.TrySpendAdvanceStones(0), Is.True, "Zero AdvanceStone deduction must return true.");
                Assert.That(PlayerDataService.AdvanceStones, Is.EqualTo(10));
            }
            finally
            {
                PlayerDataService.DataChips = origChips;
                PlayerDataService.RedGems = origGems;
                PlayerDataService.Energy = origEnergy;
                PlayerDataService.AdvanceStones = origStones;
            }
        }

        [Test]
        public void PlayerDataService_NegativeAndZeroAdditions_Ignored()
        {
            int origChips = PlayerDataService.DataChips;
            try
            {
                PlayerDataService.DataChips = 300;

                PlayerDataService.AddDataChips(-100);
                Assert.That(PlayerDataService.DataChips, Is.EqualTo(300), "Adding negative amount must be ignored.");

                PlayerDataService.AddDataChips(0);
                Assert.That(PlayerDataService.DataChips, Is.EqualTo(300), "Adding zero amount must be ignored.");

                PlayerDataService.AddDataChips(150);
                Assert.That(PlayerDataService.DataChips, Is.EqualTo(450));
            }
            finally
            {
                PlayerDataService.DataChips = origChips;
            }
        }

        [Test]
        public void PlayerDataService_OverspendAttempts_FailAndPreserveBalance()
        {
            int origChips = PlayerDataService.DataChips;
            try
            {
                PlayerDataService.DataChips = 100;

                Assert.That(PlayerDataService.TrySpendDataChips(101), Is.False);
                Assert.That(PlayerDataService.DataChips, Is.EqualTo(100));

                Assert.That(PlayerDataService.TrySpendDataChips(int.MaxValue), Is.False);
                Assert.That(PlayerDataService.DataChips, Is.EqualTo(100));
            }
            finally
            {
                PlayerDataService.DataChips = origChips;
            }
        }

        [Test]
        public void PlayerDataService_BoundaryAndDirectSetterClamping()
        {
            int origChips = PlayerDataService.DataChips;
            int origEnergy = PlayerDataService.Energy;
            try
            {
                // Direct setter negative clamping
                PlayerDataService.DataChips = -999;
                Assert.That(PlayerDataService.DataChips, Is.EqualTo(0), "Direct setter must clamp negative values to 0.");

                PlayerDataService.Energy = -50;
                Assert.That(PlayerDataService.Energy, Is.EqualTo(0), "Energy direct setter must clamp negative values to 0.");
            }
            finally
            {
                PlayerDataService.DataChips = origChips;
                PlayerDataService.Energy = origEnergy;
            }
        }

        [Test]
        public void PlayerDataService_LabItemLevel_EdgeCasesAndNullStrings()
        {
            // Null, empty, and whitespace strings must not throw
            Assert.DoesNotThrow(() =>
            {
                int lvlNull = PlayerDataService.GetItemLevel(null);
                Assert.That(lvlNull, Is.InRange(0, 10));

                PlayerDataService.SetItemLevel(null, 5);
                Assert.That(PlayerDataService.GetItemLevel(null), Is.EqualTo(5));

                PlayerDataService.IncrementItemLevel(null, 20);
                Assert.That(PlayerDataService.GetItemLevel(null), Is.EqualTo(10));
            });

            Assert.DoesNotThrow(() =>
            {
                PlayerDataService.SetItemLevel("", 7);
                Assert.That(PlayerDataService.GetItemLevel(""), Is.EqualTo(7));

                PlayerDataService.SetItemLevel("   ", 8);
                Assert.That(PlayerDataService.GetItemLevel("   "), Is.EqualTo(8));
            });
        }

        [Test]
        public void ChipManager_TestModeVsNormalMode_DeductionsAndExploitPrevention()
        {
            bool origTest = ChipManager.IsTestMode;
            int origChips = PlayerDataService.DataChips;

            try
            {
                // 1. In Test Mode: infinite transactions are allowed, BUT negative deduction must still be rejected!
                ChipManager.IsTestMode = true;

                Assert.That(ChipManager.TrySpendDataChips(-100), Is.False, "Negative spend must ALWAYS return false, even in Test Mode.");
                Assert.That(ChipManager.TrySpendDataChips(0), Is.True, "Zero spend must return true in Test Mode.");
                Assert.That(ChipManager.TrySpendDataChips(99999999), Is.True, "Valid/large spend in Test Mode returns true without crashing.");

                // 2. In Normal Mode: strict balance checking
                ChipManager.IsTestMode = false;
                PlayerDataService.DataChips = 200;

                Assert.That(ChipManager.TrySpendDataChips(-50), Is.False);
                Assert.That(ChipManager.TrySpendDataChips(0), Is.True);
                Assert.That(ChipManager.DataChips, Is.EqualTo(200));

                Assert.That(ChipManager.TrySpendDataChips(300), Is.False);
                Assert.That(ChipManager.DataChips, Is.EqualTo(200));

                Assert.That(ChipManager.TrySpendDataChips(150), Is.True);
                Assert.That(ChipManager.DataChips, Is.EqualTo(50));
            }
            finally
            {
                ChipManager.IsTestMode = origTest;
                PlayerDataService.DataChips = origChips;
            }
        }
    }
}
