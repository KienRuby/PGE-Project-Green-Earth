using System;
using System.Reflection;
using PGE.Auth;
using UnityEngine;
public static class ProbeProgram {
    static int reproduced;
    static void Check(string name,bool observed,string detail) {
        Console.WriteLine($"{(observed?"REPRODUCED":"NOT REPRODUCED")} | {name} | {detail}");
        if(observed) reproduced++;
    }
    static void Set(object obj,string name,object value) => obj.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(obj,value);
    public static int Main() {
        Console.WriteLine("Actual project logic with in-memory Unity/PlayerPrefs doubles; not Unity PlayMode tests.");
        PlayerPrefs.ResetProbeStore();
        PlayerDataService.DataChips=1000000;
        var chip=new ChipItemData {level=1,tier=ChipTier.Magic,count=100,enhanceCost=1};
        chip.ConfigureTierUnlockRules(10,5,10,15,100,20);
        int upgrades=0;
        while(chip.Enhance() && upgrades<20) upgrades++;
        Check("Chipset tier deadlock",upgrades==5 && !chip.CanEnhance && !chip.CanAdvanceTier,
            $"level={chip.level}; enhances={chip.tierEnhanceCount}/10; count={chip.count}; canEnhance={chip.CanEnhance}; canAdvance={chip.CanAdvanceTier}");

        PlayerDataService.DataChips=1000;
        CloudSaveSyncService.SaveToCloud();
        PlayerDataService.TrySpendDataChips(300);
        int afterSpend=PlayerDataService.DataChips;
        CloudSaveSyncService.LoadFromCloud();
        Check("Cloud refunds spent currency",afterSpend==700 && PlayerDataService.DataChips==1000,
            $"saved=1000; afterSpend={afterSpend}; afterLoad={PlayerDataService.DataChips}");

        PlayerPrefs.ResetProbeStore();
        PlayerDataService.SetItemLevel("HP",10);
        PlayerDataService.SetItemLevel("DEF",10);
        var health=new PlayerHealth();
        var stats=new PlayerStatsManager();
        Set(stats,"playerHealth",health);
        stats.LoadAndApplyStats();
        int hpBefore=health.MaxHealth,defBefore=health.Reduction;
        var inventory=new PlayerArtifactInventory();
        Set(inventory,"playerHealth",health);
        inventory.EquipArtifact(new ArtifactData {artifactName="Strong Cooler",statType=ArtifactStatType.TurretAttackSpeedPercent,statValue=20});
        Check("Unrelated artifact wipes Lab HP/DEF",hpBefore==250 && defBefore==10 && health.MaxHealth==100 && health.Reduction==0,
            $"HP {hpBefore}->{health.MaxHealth}; DEF {defBefore}->{health.Reduction}");

        PlayerPrefs.ResetProbeStore();
        PlayerDataService.SetItemLevel("MOVE SPEED",10);
        var movement=new PlayerMovement();
        var speedStats=new PlayerStatsManager(); Set(speedStats,"playerMovement",movement); speedStats.LoadAndApplyStats();
        float speedBefore=movement.Bonus;
        var speedInventory=new PlayerArtifactInventory(); Set(speedInventory,"playerMovement",movement);
        speedInventory.EquipArtifact(new ArtifactData {statType=ArtifactStatType.MoveSpeedPercent,statValue=10});
        Check("Movement artifact replaces Lab speed",speedBefore==2.5f && movement.Bonus==0.5f,$"bonus {speedBefore}->{movement.Bonus}");

        var buddy=new BuddyItemData {tier=BuddyTier.Holographic,count=20,requiredCount=10};
        bool advanced=buddy.AdvanceTier();
        Check("Max-tier Buddy consumes fragments",advanced && buddy.tier==BuddyTier.Holographic && buddy.count==10,
            $"advance={advanced}; tier={buddy.tier}; fragments 20->{buddy.count}");
        Console.WriteLine($"Confirmed {reproduced}/5 observed logic defects.");
        return reproduced==5?0:1;
    }
}
