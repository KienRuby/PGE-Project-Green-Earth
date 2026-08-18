using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class PlayerAutoShooterAimTests
{
    [TestCase(0f, 1f)]
    [TestCase(90f, 1f)]
    [TestCase(180f, -1f)]
    [TestCase(-90f, 1f)]
    public void CalculateAimScale_FlipsOnlyYAxis(float angle, float expectedYSign)
    {
        MethodInfo method = typeof(PlayerAutoShooter).GetMethod(
            "CalculateAimScale",
            BindingFlags.NonPublic | BindingFlags.Static
        );

        Assert.That(method, Is.Not.Null);

        Vector3 baseScale = new Vector3(2f, 3f, 4f);
        Vector3 result = (Vector3)method.Invoke(null, new object[] { angle, baseScale });

        Assert.That(result.x, Is.EqualTo(baseScale.x));
        Assert.That(result.y, Is.EqualTo(Mathf.Abs(baseScale.y) * expectedYSign));
        Assert.That(result.z, Is.EqualTo(baseScale.z));
    }

    [TestCase(false, 2f)]
    [TestCase(true, -2f)]
    public void CalculateBodyScale_MirrorsOnlyXAxis(
        bool isAimingLeft,
        float expectedX
    )
    {
        MethodInfo method = typeof(PlayerAutoShooter).GetMethod(
            "CalculateBodyScale",
            BindingFlags.NonPublic | BindingFlags.Static
        );

        Assert.That(method, Is.Not.Null);

        Vector3 baseScale = new Vector3(2f, 3f, 4f);
        Vector3 result = (Vector3)method.Invoke(
            null,
            new object[] { isAimingLeft, baseScale }
        );

        Assert.That(result.x, Is.EqualTo(expectedX));
        Assert.That(result.y, Is.EqualTo(baseScale.y));
        Assert.That(result.z, Is.EqualTo(baseScale.z));
    }

    [TestCase(0f, false, 0f)]
    [TestCase(90f, false, 90f)]
    [TestCase(180f, true, 0f)]
    [TestCase(135f, true, 45f)]
    [TestCase(-135f, true, -45f)]
    public void CalculateLocalAimAngle_CompensatesBodyMirror(
        float worldAngle,
        bool isAimingLeft,
        float expectedLocalAngle
    )
    {
        MethodInfo method = typeof(PlayerAutoShooter).GetMethod(
            "CalculateLocalAimAngle",
            BindingFlags.NonPublic | BindingFlags.Static
        );

        Assert.That(method, Is.Not.Null);

        float result = (float)method.Invoke(
            null,
            new object[] { worldAngle, isAimingLeft }
        );

        Assert.That(Mathf.DeltaAngle(expectedLocalAngle, result), Is.EqualTo(0f));
    }
}
