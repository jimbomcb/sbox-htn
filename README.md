# s&box HTN (Hierarchial Task Network) Planner

> ***This is still in development, and probably not stable enough for usage in your project yet.***
> ***I am "dogfooding" this library to drive a few thousand AI agents within a reasonable time budget, and once I'm happy enough with the API design I will look at locking it down and publishing.***

HTN AI planner - Readme WIP

## What is planning? 

Game AI often takes the form of Finite State Machines and Behaviour Trees, where behaviours and the transitions between behaviours are specifically crafted. The agent will evaluate on-the-fly the most appropriate action for the current world state, potentially at the detriment of a longer-term goal.

Planning approaches it differently, building the ideal plan (series of sequential tasks) that best "solves" the world state. This was popularized in games by FEAR's clever of GOAP (Goal-Oriented Action Planning), but GOAP is not the only planning algorithm.

## What is HTN (Hierarchial Task Network) planning?

The building blocks of the Hierarchial Task Network are:
- **Primitive Tasks**: A single performable operation, such as: Pick up object, Walk to position
- **Compound Tasks**: A series of sequential **Branches**, each branch has a set of preconditions and actions to perform if preconditions are met

HTN planning involves taking a root compound task, evaluating the branches against the world state to find the ideal compound + primitive tasks to perform, and decomposing(*) any compound tasks recursively until we are left with only actionable primitive tasks.

- **Branch**:
  - **Preconditions**: All preconditions must pass for the actions to be chosen
  - **Tasks**: A list of compound or primitive tasks, recusively decomposed(*) until only primitive tasks remain.

Branch preconditions have a powerful JSHOP2-like query system allowing for smart backtracking, and attempting of alternative matching query results if the initial results do not decompose into a valid set of tasks.

# Example
### World State
```
(enemy [Enemy:human] vec3(150,200,350) [Weapon.Gun])
(enemy [Enemy:troll] vec3(10,60,110) [Weapon.Club])
(enemy [Enemy:goblin] vec3(-200,-200,0) [Weapon.Claw])
(weak_to [Weapon:Gun])
(player_health 80)
(player_weapon [Weapon.Club])
```

### Branches
```
Compound Tasks:

RootTask:
- Branch: AttackIfPossible
  - Precondition:
    AND (
      QUERY( "enemy", "?enemy", "?position", "?weapon" ), // Find each world state entry that starts with "enemy" (and has 4 elements)
      NOT( QUERY( "weak_to", "?weapon" ) ) // And only take enemies if we're not weak to their weapon
    )
  - Tasks:
      PerformAttack( vars.Get<Enemy>("?enemy") )

PerformAttack( ?enemy ):
- Branch: UseSniper
  - Precondition:
    QUERY( "player_weapon", Weapon.Sniper )
  - Tasks:
    MoveToSniperNest()
    AimAtTarget( vars.Get<Enemy>("?enemy") )
    FireWeapon( vars.Get<Enemy>("?enemy") )
- Branch: UseRPG
  - Precondition:
    QUERY( "player_weapon", Weapon.RPG )
  - Tasks:
    MoveToSafeDistanceFrom( vars.Get<Enemy>("?enemy") )
    AimAtTarget( vars.Get<Enemy>("?enemy") )
    FireWeapon( vars.Get<Enemy>("?enemy") )
```

... TODO

# ---
Inspired by HTN Planning in the DECIMA engine as described by Tim Verweij and the Guerrilla Games team, itself inspired by the JSHOP2 planner from the University of Maryland.

See also:
- [HTN Planning in Decima - Tim Verweij/Guerrilla Games](https://www.guerrilla-games.com/read/htn-planning-in-decima)
- [Hierarchical AI for Multiplayer Bots in Killzone 3 - Remco Straatman, Tim Verweij, Alex Champandard, Robert Morcus, and Hylke Kleve](https://www.gameaipro.com/GameAIPro/GameAIPro_Chapter29_Hierarchical_AI_for_Multiplayer_Bots_in_Killzone_3.pdf)
- [Exploring HTN Planners through Example - Troy Humphreys](https://www.gameaipro.com/GameAIPro/GameAIPro_Chapter12_Exploring_HTN_Planners_through_Example.pdf)
- [JSHOP2 planner - University of Maryland](https://www.cs.umd.edu/projects/shop/)
