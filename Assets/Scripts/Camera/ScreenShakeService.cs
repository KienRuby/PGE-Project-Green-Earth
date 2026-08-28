using UnityEngine;

/// <summary>
/// Rung camera 2D bằng trauma giảm dần. CameraFollow lấy offset từ đây ở cuối LateUpdate.
/// </summary>
public static class ScreenShakeService
{
    private const float TraumaDecayPerSecond = 1.8f;
    private const float MaxHorizontalOffset = 0.18f;
    private const float MaxVerticalOffset = 0.12f;
    private static float trauma;
    private static float phase;

    public static void AddTrauma(float amount)
    {
        if (!GameSettings.ScreenShake) return;
        trauma = Mathf.Clamp01(trauma + Mathf.Max(0f, amount));
    }

    public static Vector3 UpdateAndGetOffset(float deltaTime)
    {
        if (!GameSettings.ScreenShake)
        {
            trauma = 0f;
            return Vector3.zero;
        }

        trauma = Mathf.Max(0f, trauma - TraumaDecayPerSecond * Mathf.Max(0f, deltaTime));
        if (trauma <= 0f) return Vector3.zero;

        phase += Mathf.Max(0f, deltaTime) * 32f;
        float strength = trauma * trauma;
        return new Vector3(
            Mathf.Sin(phase * 1.73f) * MaxHorizontalOffset * strength,
            Mathf.Sin(phase * 2.31f) * MaxVerticalOffset * strength,
            0f);
    }

    public static void Reset()
    {
        trauma = 0f;
        phase = 0f;
    }
}
