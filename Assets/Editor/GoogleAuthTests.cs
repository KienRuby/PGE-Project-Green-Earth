using NUnit.Framework;
using PGE.Auth;
using UnityEngine;

public class GoogleAuthTests
{
    [SetUp]
    public void SetUp()
    {
        if (GoogleAuthManager.Instance != null && GoogleAuthManager.Instance.IsLoggedIn)
        {
            GoogleAuthManager.Instance.SignOut();
        }
    }

    [TearDown]
    public void TearDown()
    {
        if (GoogleAuthManager.Instance != null && GoogleAuthManager.Instance.IsLoggedIn)
        {
            GoogleAuthManager.Instance.SignOut();
        }
    }

    [Test]
    public void GoogleAuth_SignIn_SetsCurrentUser_AndPersistsSession()
    {
        bool callbackFired = false;
        UserProfile signedUser = null;

        GoogleAuthManager.Instance.SignInWithGoogle((success, user) =>
        {
            callbackFired = true;
            signedUser = user;
        });

        Assert.That(callbackFired, Is.True);
        Assert.That(GoogleAuthManager.Instance.IsLoggedIn, Is.True);
        Assert.That(signedUser, Is.Not.Null);
        Assert.That(signedUser.authProvider, Is.EqualTo("Google"));
        Assert.That(signedUser.email, Does.Contain("@gmail.com"));

        // Sign out
        GoogleAuthManager.Instance.SignOut();
        Assert.That(GoogleAuthManager.Instance.IsLoggedIn, Is.False);
    }

    [Test]
    public void CloudSave_SyncsPlayerData_WhenLoggedIn()
    {
        GoogleAuthManager.Instance.SignInWithGoogle();

        int originalChips = PlayerDataService.DataChips;
        PlayerDataService.DataChips = 5555;

        bool saveSuccess = false;
        CloudSaveSyncService.SaveToCloud((success, msg) => saveSuccess = success);
        Assert.That(saveSuccess, Is.True);

        // Simulate local data loss
        PlayerDataService.DataChips = 10;
        Assert.That(PlayerDataService.DataChips, Is.EqualTo(10));

        // Restore from cloud
        bool loadSuccess = false;
        CloudSaveSyncService.LoadFromCloud((success, msg) => loadSuccess = success);
        Assert.That(loadSuccess, Is.True);
        Assert.That(PlayerDataService.DataChips, Is.EqualTo(5555));

        // Cleanup
        PlayerDataService.DataChips = originalChips;
        GoogleAuthManager.Instance.SignOut();
    }

    [Test]
    public void AppleAuth_SignIn_AndCloudSaveSync()
    {
        bool callbackFired = false;
        UserProfile signedUser = null;

        AppleAuthManager.Instance.SignInWithApple((success, user) =>
        {
            callbackFired = true;
            signedUser = user;
        });

        Assert.That(callbackFired, Is.True);
        Assert.That(AppleAuthManager.Instance.IsLoggedIn, Is.True);
        Assert.That(signedUser, Is.Not.Null);
        Assert.That(signedUser.authProvider, Is.EqualTo("Apple"));
        Assert.That(signedUser.email, Does.Contain("@privaterelay.appleid.com"));

        // Test Cloud Save with Apple Account
        int originalGems = PlayerDataService.RedGems;
        PlayerDataService.RedGems = 7777;

        bool saveDone = false;
        CloudSaveSyncService.SaveToCloud((success, msg) => saveDone = success);
        Assert.That(saveDone, Is.True);

        // Sign out
        AppleAuthManager.Instance.SignOut();
        Assert.That(AppleAuthManager.Instance.IsLoggedIn, Is.False);
        PlayerDataService.RedGems = originalGems;
    }
}
