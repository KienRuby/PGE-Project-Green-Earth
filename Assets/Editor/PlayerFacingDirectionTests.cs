using System.Reflection;
using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class PlayerFacingDirectionTests
{
    private GameObject playerObj;
    private GameObject bodyBone;
    private GameObject bodyVisualObj;
    private SpriteRenderer bodyVisualRenderer;
    private GameObject legBone;
    private SpriteRenderer legRenderer;
    private PlayerAutoShooter shooter;

    [SetUp]
    public void SetUp()
    {
        playerObj = new GameObject("TestPlayer");
        shooter = playerObj.AddComponent<PlayerAutoShooter>();

        bodyBone = new GameObject("thân");
        bodyBone.transform.SetParent(playerObj.transform, false);

        bodyVisualObj = new GameObject("BodyVisual");
        bodyVisualObj.transform.SetParent(bodyBone.transform, false);
        bodyVisualRenderer = bodyVisualObj.AddComponent<SpriteRenderer>();

        legBone = new GameObject("Chan 1");
        legBone.transform.SetParent(playerObj.transform, false);
        legRenderer = legBone.AddComponent<SpriteRenderer>();

        typeof(PlayerAutoShooter).GetField("bodyTransform", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(shooter, bodyBone.transform);

        typeof(PlayerAutoShooter).GetField("bodyTransformBaseScale", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(shooter, Vector3.one);

        shooter.SetRenderers(null, new SpriteRenderer[] { bodyVisualRenderer, legRenderer });
    }

    [TearDown]
    public void TearDown()
    {
        if (playerObj != null)
        {
            Object.DestroyImmediate(playerObj);
        }
    }

    [Test]
    public void SetBodyFacing_WhenAimingLeft_FlipsBodyScaleX_AndDoesNotDoubleFlipVisualSlot()
    {
        MethodInfo setBodyFacing = typeof(PlayerAutoShooter).GetMethod("SetBodyFacing", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(setBodyFacing, Is.Not.Null, "Phải tìm thấy method SetBodyFacing.");

        setBodyFacing.Invoke(shooter, new object[] { true });

        Assert.That(bodyBone.transform.localScale.x, Is.LessThan(0f), "bodyTransform.localScale.x phải < 0 khi ngắm sang trái.");
        Assert.That(bodyVisualRenderer.flipX, Is.False, "bodyVisualRenderer.flipX phải là false để thân lật tự nhiên theo scale cha mà không bị khử.");
        Assert.That(legRenderer.flipX, Is.True, "legRenderer.flipX phải là true khi ngắm sang trái vì chân nằm ngoài thân.");
    }

    [Test]
    public void SetBodyFacing_WhenAimingRight_RestoresPositiveScaleX_AndLeavesFlipXFalse()
    {
        MethodInfo setBodyFacing = typeof(PlayerAutoShooter).GetMethod("SetBodyFacing", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(setBodyFacing, Is.Not.Null);

        setBodyFacing.Invoke(shooter, new object[] { true });
        setBodyFacing.Invoke(shooter, new object[] { false });

        Assert.That(bodyBone.transform.localScale.x, Is.GreaterThan(0f), "bodyTransform.localScale.x phải > 0 khi ngắm sang phải.");
        Assert.That(bodyVisualRenderer.flipX, Is.False, "bodyVisualRenderer.flipX phải là false khi ngắm sang phải.");
        Assert.That(legRenderer.flipX, Is.False, "legRenderer.flipX phải là false khi ngắm sang phải.");
    }

    [Test]
    public void UpdateGunAndAttackPointRotation_WithLeftTarget_TurnsBodyLeft()
    {
        GameObject targetObj = new GameObject("LeftTarget");
        try
        {
            targetObj.transform.position = new Vector3(-10f, 0f, 0f);

            typeof(PlayerAutoShooter).GetField("currentTarget", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(shooter, targetObj.transform);

            MethodInfo updateRotation = typeof(PlayerAutoShooter).GetMethod("UpdateGunAndAttackPointRotation", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(updateRotation, Is.Not.Null);

            updateRotation.Invoke(shooter, null);

            Assert.That(bodyBone.transform.localScale.x, Is.LessThan(0f), "Thân phải quay sang trái khi quái ở bên trái.");
            Assert.That(bodyVisualRenderer.flipX, Is.False, "Sprite thân không được có flipX = true.");
        }
        finally
        {
            Object.DestroyImmediate(targetObj);
        }
    }
}
