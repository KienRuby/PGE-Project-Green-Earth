using System;

public enum TowerDefStructureType
{
    None = 0,
    Turret = 1,
    EnergyGenerator = 2,
    CoreBed = 3,
    Gate = 4
}

public enum TowerDefEnemyState
{
    Spawning,
    MovingDown,
    AttackingGate,
    Dead
}
