using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class EnemyAnimationMovementTests
{
    [TestCase("Assets/Prefabs/Enemy/Creep/Creep.prefab")]
    [TestCase("Assets/Prefabs/Enemy/Creep/BigCreep.prefab")]
    [TestCase("Assets/Prefabs/Enemy/Creep/Creep2.prefab")]
    [TestCase("Assets/Prefabs/Enemy/Creep/Big creep 2.prefab")]
    [TestCase("Assets/Prefabs/Enemy/Creep/Creep3.prefab")]
    [TestCase("Assets/Prefabs/Enemy/Creep/Bigcreep3.prefab")]
    [TestCase("Assets/Prefabs/Enemy/Boss/Boss.prefab")]
    [TestCase("Assets/Prefabs/Enemy/Boss/Boss2.prefab")]
    [TestCase("Assets/Prefabs/Enemy/Boss/boss 3.prefab")]
    public void Animator_PreservesGameplayPosition_AcrossUpdatesAndPoolRebind(string prefabPath)
    {
        GameObject instance = null;
        try
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.That(prefab, Is.Not.Null, prefabPath);
            instance = Object.Instantiate(prefab);
            instance.hideFlags = HideFlags.HideAndDontSave;
            var animator = instance.GetComponent<Animator>();
            Assert.That(animator, Is.Not.Null, prefabPath);
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.applyRootMotion = false;

            // Pool reset must not teleport the newly spawned body to the clip's origin.
            Vector3 expected = new Vector3(5f, -3f, 0f);
            instance.transform.position = expected;
            animator.Rebind();
            animator.Update(0f);
            Assert.That(Vector3.Distance(instance.transform.position, expected), Is.LessThan(0.0001f),
                prefabPath + ": Animator rebind overwrote the spawn position.");

            // Mimic successive gameplay movement before the Animator evaluates each frame.
            for (int frame = 0; frame < 60; frame++)
            {
                expected += new Vector3(0.04f, 0.01f, 0f);
                instance.transform.position = expected;
                animator.Update(1f / 60f);
                Assert.That(Vector3.Distance(instance.transform.position, expected), Is.LessThan(0.0001f),
                    prefabPath + ": Animator overwrote movement at frame " + frame);
            }

            // With Write Defaults enabled, even an inactive Attack/Death clip can lock Walk.
            foreach (var clip in animator.runtimeAnimatorController.animationClips)
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                Assert.That(binding.type == typeof(Transform) && binding.path == "" &&
                            binding.propertyName.StartsWith("m_LocalPosition."), Is.False,
                    prefabPath + ": " + clip.name + " animates the gameplay root position.");
            }
        }
        finally
        {
            if (instance != null) Object.DestroyImmediate(instance);
        }
    }
}
