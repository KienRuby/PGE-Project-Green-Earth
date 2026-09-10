using System.Linq;
using NUnit.Framework;
using PGE.Auth;
using UnityEngine;

public sealed class PGEAuthSaveTests
{
    private GameSaveData baseline;

    [SetUp]
    public void SetUp()
    {
        baseline = GameSaveData.Capture(string.Empty, 0);
    }

    [TearDown]
    public void TearDown()
    {
        baseline.ApplyToPlayerPrefs();
        PlayerPrefs.DeleteKey(GameSettings.GoogleAccountKey);
        PlayerPrefs.DeleteKey(GameSettings.AppleAccountKey);
        PlayerPrefs.Save();
    }

    [Test]
    public void Snapshot_RoundTrip_RestoresGameplayValues()
    {
        PlayerPrefs.SetInt(PlayerDataService.DataChipsKey, 1234);
        PlayerPrefs.SetString(PlayerDataService.SelectedWeaponIdKey, "test_weapon");
        GameSaveData snapshot = GameSaveData.Capture("server-player-a", 17);
        string json = JsonUtility.ToJson(snapshot);

        PlayerPrefs.SetInt(PlayerDataService.DataChipsKey, 1);
        PlayerPrefs.SetString(PlayerDataService.SelectedWeaponIdKey, "blaster");
        GameSaveData restored = JsonUtility.FromJson<GameSaveData>(json);
        restored.ApplyToPlayerPrefs();

        Assert.That(PlayerPrefs.GetInt(PlayerDataService.DataChipsKey), Is.EqualTo(1234));
        Assert.That(PlayerPrefs.GetString(PlayerDataService.SelectedWeaponIdKey), Is.EqualTo("test_weapon"));
        Assert.That(restored.progressRevision, Is.EqualTo(17));
        Assert.That(restored.ownerPlayerId, Is.EqualTo("server-player-a"));
    }

    [Test]
    public void Validation_RejectsDuplicateAndFutureSchema()
    {
        GameSaveData data = GameSaveData.CreateDefaults("player-a");
        data.values.Add(new SaveValue { key = data.values[0].key, intValue = 99 });
        Assert.That(data.Validate(out _), Is.False);
        data = GameSaveData.CreateDefaults("player-a");
        data.saveVersion = GameSaveData.CurrentVersion + 1;
        Assert.That(data.Validate(out _), Is.False);
    }

    [Test]
    public void AccountSwitch_DefaultSnapshotDoesNotCopyPriorDynamicInventory()
    {
        string priorKey = PlayerDataService.GetChipItemPrefix(1) + "Level";
        PlayerPrefs.SetInt(priorKey, 24);
        GameSaveData cleanAccount = GameSaveData.CreateDefaults("server-player-b");
        Assert.That(cleanAccount.ownerPlayerId, Is.EqualTo("server-player-b"));
        Assert.That(cleanAccount.values.Any(v => v.key == priorKey), Is.False);
        PlayerPrefs.DeleteKey(priorKey);
    }

    [Test]
    public void LocalAccountStrings_CannotClaimPlatformAuthentication()
    {
        GameSettings.GoogleAccount = "forged-google-id";
        GameSettings.AppleAccount = "forged-apple-id";
        Assert.That(GameSettings.IsLoggedInGoogle, Is.False);
        Assert.That(GameSettings.IsLoggedInApple, Is.False);
        Assert.That(CloudSaveSyncService.IsAnyCloudLoggedIn, Is.False);
    }
}
