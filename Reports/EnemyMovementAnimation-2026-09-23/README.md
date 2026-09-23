# Enemy movement: confirmed animation root-position conflict

Unity 2022.3.62f2 ran in a separate temporary project containing copies of all nine original enemy prefabs, their Animator controllers, clips, sprites and Physics2D/TagManager settings. Gameplay scripts were absent to isolate Animator and Rigidbody2D from AI, spawning, obstacles and combat.

Each prefab started at (5, 3). For 100 steps, Rigidbody2D.MovePosition requested +0.04 on X, Physics2D.Simulate advanced 0.02 seconds, then Animator.Update evaluated animation. The experiment ran with Animator disabled and enabled.

- Animator disabled: all nine prefabs finished at (9, 3).
- Animator enabled: Creep2, Bigcreep3 and boss 3 reset to (0, 0) after every animation update. The other six finished at (9, 3).
- Their controllers use Write Defaults. Root-position bindings in inactive Attack/Death clips overwrite the gameplay root even while the default Walk state runs.

Raw baseline: before.txt.

Unity AnimationUtility.SetEditorCurve removed only the root Transform m_LocalPosition.x/y/z bindings from:

- Assets/Animaton/Enemy/Creep 2/Attackcreep2.anim
- Assets/Animaton/Enemy/Creep3/AttackBigcreep.anim
- Assets/Animaton/Enemy/Boss/Boss3/Death.anim

Other curves and original GUIDs were retained. The small root translation in these attack/death clips is removed; rotation and other visual animation remain. Earlier speculative changes to EnemyMovement, BossMovement and EnemySpawner were reverted to their prior state.

Unity batch execution passed 18/18 checks:

- Nine NUnit regression cases execute Animator.Rebind/Update at a nonzero spawn position, then 60 successive movement/animation updates, and inspect every referenced clip for root-position bindings.
- Nine Rigidbody2D simulations with Animator enabled each move from (5, 3) to (9, 3) after 100 steps.

Raw results: after.txt. Persistent regression suite: Assets/Editor/EnemyAnimationMovementTests.cs (Unity Test Runner, EditMode).

This validates the animation/physics failure in Unity through an isolated reproduction, not a full map playthrough. No gameplay scene or player save was changed.
