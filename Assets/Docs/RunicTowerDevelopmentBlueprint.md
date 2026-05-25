# Runic Tower Development Blueprint

## 1. Goal

This document turns the design concept from `RunicTowerPlan.pdf` into a practical Unity development plan.

The first playable target is a vertical slice that proves one thing clearly:

`Is it fun to build rituals from a random rune hand inside a turn-based tower battle?`

Everything in the first implementation should support that question. Systems that do not directly help answer it should be delayed.

## 2. MVP Pillars

The prototype should be built around these pillars:

1. Single-player only.
2. Turn-based combat.
3. Random rune hand from a prebuilt deck.
4. Ritual building from up to 3 runes plus 1 modifier.
5. Mana pressure and overheat/failure risk.
6. Tower progression through floors with regular enemies and a boss.
7. Simple rewards that let the player continue climbing.

## 3. First Vertical Slice Scope

The first vertical slice should contain:

- 4 elements: Fire, Air, Earth, Water
- 3 rune sizes: Small, Medium, Large
- 4 modifiers: Stabilization, Direction, Focus, Echo
- Player deck of 8 elemental runes before a run
- Draw 5 elemental runes each turn
- Draw 1 modifier each turn
- Save 1 unused elemental rune for the next turn
- Build a ritual from up to 3 elemental runes
- Apply 1 modifier in a dedicated slot
- Player HP and mana
- Enemy HP and mana
- Overheat/failure resolution
- 3 regular enemy archetypes
- 1 boss
- 5 to 10 tower floors
- Basic post-battle reward choice

## 4. Core Gameplay Loop

### 4.1 Meta Loop

1. Start tower run.
2. Enter floor encounter.
3. Fight one or more enemies.
4. Receive reward.
5. Continue climbing.
6. Reach boss floor every 5 floors.
7. Lose run or continue indefinitely.

### 4.2 Battle Loop

1. Start round.
2. Restore mana partially.
3. Draw 5 random elemental runes from valid deck distribution.
4. Draw 1 modifier rune.
5. Show enemy archetype and visible enemy rune hand.
6. Player chooses up to 3 elemental runes.
7. Player optionally inserts 1 modifier into modifier slot.
8. Player optionally marks 1 unused elemental rune to preserve.
9. Ritual cost is calculated.
10. Success and overheat rules are checked.
11. Ritual resolves.
12. Enemy takes its turn.
13. End round state updates.
14. Next round begins with 4 new elemental runes plus preserved rune.

## 5. Design-to-System Translation

### 5.1 Combat Domain

The combat layer should be deterministic after selection, except for explicit success/failure rules.

Core combat responsibilities:

- Track turn state
- Track current hand
- Validate ritual construction
- Calculate mana cost
- Calculate effect payload
- Apply overheat/failure logic
- Resolve player and enemy actions
- Decide victory or defeat

### 5.2 Rune Domain

A rune is not just an icon in hand. It needs to carry enough data to be used in deckbuilding, hand generation, validation, and combat resolution.

Each elemental rune should define:

- stable id
- display name
- element
- size
- mana cost
- base primary stat value
- optional secondary stat value
- tags for special combination rules

Each modifier rune should define:

- stable id
- display name
- modifier type
- cost adjustment or rule override
- failure/success adjustment
- duplication or special resolution behavior

### 5.3 Ritual Domain

A ritual is the result of the player combining:

- up to 3 elemental runes
- 0 or 1 modifier rune

The ritual system should own:

- rune order
- combination validation
- final cost
- success chance
- overheat contribution
- final effects generated

### 5.4 Progression Domain

For the first slice, progression should stay thin.

Required progression responsibilities:

- floor number
- encounter generation
- boss floor detection
- simple reward selection
- player deck persistence during current run

Delayed progression responsibilities:

- account progression
- rune leveling
- artifact inventory
- deep economy

## 6. Proposed Project Structure

This is a clean starting structure for the current Unity project:

```text
Assets/
  Art/
  Audio/
  Docs/
  Prefabs/
  Resources/
    ScriptableObjects/
      Runes/
      Modifiers/
      Enemies/
      Encounters/
  Scenes/
    Boot.unity
    Battle.unity
    Meta.unity
  Scripts/
    Core/
    Combat/
    Runes/
    Enemies/
    Progression/
    UI/
    Data/
  UI/
```

### 6.1 Scene Responsibilities

`Boot.unity`

- Initializes save data and shared services
- Loads first playable scene

`Meta.unity`

- Run start screen
- Floor map or simple floor progression UI
- Reward choice screen

`Battle.unity`

- Main combat scene
- Hand UI
- Ritual slots
- Enemy presentation
- Battle log and combat state UI

## 7. Recommended Script Architecture

Keep the first architecture simple and testable. Use plain C# data objects for calculation and MonoBehaviours mainly for scene wiring and UI.

### 7.1 Data Layer

Use `ScriptableObject` assets for authored content:

- `ElementalRuneDefinition`
- `ModifierRuneDefinition`
- `EnemyDefinition`
- `EncounterDefinition`

Use plain runtime classes for changing state:

- `PlayerRunState`
- `BattleState`
- `CombatantState`
- `HandState`
- `RitualBuild`
- `RitualResult`

### 7.2 Service Layer

Recommended service classes:

- `DeckService`
- `HandDrawService`
- `RitualValidationService`
- `RitualCalculationService`
- `OverheatService`
- `EnemyDecisionService`
- `BattleResolutionService`
- `TowerProgressionService`
- `RewardService`

### 7.3 Presentation Layer

Main UI controllers:

- `BattleScreenController`
- `HandPanelController`
- `RitualBuilderController`
- `CombatantHudController`
- `RewardScreenController`
- `FloorProgressController`

## 8. Data Model Proposal

### 8.1 Enums

Use a few stable enums early:

```csharp
public enum ElementType
{
    Fire,
    Air,
    Earth,
    Water
}

public enum RuneSize
{
    Small,
    Medium,
    Large
}

public enum ModifierType
{
    Stabilization,
    Direction,
    Focus,
    Echo
}

public enum EffectType
{
    Damage,
    Shield,
    Heal,
    Regeneration,
    DefenseBreak,
    Burn
}
```

### 8.2 Scriptable Object Definitions

Suggested authored fields:

`ElementalRuneDefinition`

- `string Id`
- `string DisplayName`
- `ElementType Element`
- `RuneSize Size`
- `int ManaCost`
- `int PrimaryValue`
- `int SecondaryValue`

`ModifierRuneDefinition`

- `string Id`
- `string DisplayName`
- `ModifierType ModifierType`
- `int ManaCostDelta`
- `float SuccessChanceDelta`

`EnemyDefinition`

- `string Id`
- `string DisplayName`
- `int MaxHp`
- `int MaxMana`
- `EnemyArchetype Archetype`
- `List<string> PreferredRuneIds`
- `BossRuleData BossRule`

## 9. First-Pass Gameplay Numbers

These are not final balance values. They are starter numbers to get the game moving.

### 9.1 Player Starting Stats

- HP: `30`
- Max mana: `6`
- Mana regen per round: `3`

### 9.2 Enemy Starting Stats

- Regular enemy HP: `18` to `28`
- Boss HP: `45` to `60`
- Enemy max mana: `5` to `7`
- Enemy mana regen per round: `2` to `3`

### 9.3 Rune Cost by Size

- Small: `1`
- Medium: `2`
- Large: `3`

### 9.4 Base Rune Effects

These values should be attached to size first, then flavored by element:

- Fire
  - Small: `3 damage`
  - Medium: `5 damage`
  - Large: `8 damage + 2 burn`
- Air
  - Small: `2 damage`
  - Medium: `4 damage + 1 defense break`
  - Large: `6 damage + 2 defense break`
- Earth
  - Small: `3 shield`
  - Medium: `5 shield`
  - Large: `6 shield + 3 damage`
- Water
  - Small: `2 regeneration`
  - Medium: `4 regeneration`
  - Large: `5 regeneration + 3 damage`

### 9.5 Modifier Effects

- Stabilization
  - Raises success chance of unstable same-element chains
- Direction
  - Allows otherwise blocked combinations
- Focus
  - Reduces ritual mana cost by `1`
- Echo
  - Repeats ritual at `50%` repeated effect value

## 10. Ritual Rules Specification

These rules should be implemented explicitly, not buried inside UI logic.

### 10.1 Ritual Construction Rules

- Ritual can contain `1` to `3` elemental runes.
- Modifier slot accepts `0` or `1` modifier.
- Ritual cannot be cast if player lacks mana after modifier adjustments.
- Preserved rune cannot also be consumed this round.

### 10.2 Same-Element Rules

- Same-element chains amplify base effect.
- Three identical element runes without Stabilization are powerful but unstable.
- Three identical element runes without Stabilization have `50%` failure chance.
- Three identical element runes with Stabilization have `90%` success chance.

### 10.3 Same-Element Different-Size Rule

- If two runes of the same element but different sizes are used together, only the larger rune contributes its base size power.
- The smaller rune may still count for combo structure if needed, but should not double-dip raw elemental value.

This rule needs targeted playtesting because it can feel unintuitive if presented poorly.

### 10.4 Hybrid Combination Rules

First locked hybrid table:

- Earth + Water = shield + regeneration
- Earth + Fire = magma, damage-heavy, no shield
- Water + Air = ice, damage-heavy, no healing
- Fire + Air = amplified fire damage
- Air + Earth = blocked by default, enabled by Direction, becomes stone bullet with damage + weaken
- Water + Fire = blocked by default, enabled by Direction, becomes steam with damage + small self-heal

### 10.5 Chain Resolution Priority

Recommended calculation order:

1. Validate combination legality.
2. Determine effective runes after same-element size suppression.
3. Build base effect package from rune elements and sizes.
4. Apply same-element amplification.
5. Apply hybrid transformation rules.
6. Apply modifier rule changes.
7. Calculate final mana cost.
8. Calculate success chance and overheat.
9. Resolve success or failure.
10. Apply final effect payload.

## 11. Overheat Proposal

The design doc says overheat must exist, but its exact formula is still open. For the first implementation, use a readable formula instead of a highly simulated one.

### 11.1 Overheat Score

Each ritual generates overheat points:

- Small rune: `1`
- Medium rune: `2`
- Large rune: `3`
- Same-element triple: `+2`
- Forbidden combo opened by Direction: `+1`
- Echo: `+1`

### 11.2 Failure Check

Suggested first-pass formula:

`FailureChance = max(0, OverheatScore * 5 - StabilityBonus)`

Where:

- `StabilityBonus = 5` when ritual has 2 different elements
- `StabilityBonus = 10` with Stabilization
- Same-element triple without Stabilization is clamped to at least `50%` failure chance
- Same-element triple with Stabilization is clamped to `10%` failure chance

### 11.3 Failure Result

Keep failure readable in the first prototype:

- ritual fizzles completely, or
- ritual resolves at `50%` value and player takes `1` self-damage

The second option is usually better for testing because full whiff outcomes can feel too punishing too early.

## 12. Starter Deck Proposal

The design doc defines deck structure as:

- `3` small runes
- `3` medium runes
- `2` large runes

Recommended starter deck:

- Small Fire
- Small Earth
- Small Water
- Medium Fire
- Medium Air
- Medium Earth
- Large Water
- Large Fire

This mix gives:

- offensive access
- defense access
- recovery access
- obvious hybrid testing paths

## 13. Enemy Archetypes for Prototype

### 13.1 Ember Acolyte

- Aggressive fire-focused enemy
- Prioritizes direct damage chains
- Low defense
- Teaches player threat pressure

### 13.2 Stone Warden

- Earth-focused defensive enemy
- Builds shields and waits for value turns
- Teaches shield breaking and tempo control

### 13.3 Mist Seer

- Water and air enemy
- Uses regeneration and ice-style hybrids
- Teaches reading enemy setup instead of pure damage racing

### 13.4 First Boss: Furnace Archivist

- Boss floor: `5`
- Theme: unstable fire rituals and punishing greed
- Unique rule: when player casts a 3-rune ritual, boss gains `1` charge
- At 3 charges, boss performs a strong ignition attack next turn

This boss tests whether the player can adapt, not just always build the biggest chain.

## 14. Reward Design for Prototype

Keep rewards small and strategically meaningful.

After each battle, offer one of:

- Rune shard reward of known size
- Random ritual chest with 2 to 3 runes
- Mana blessing for next battle
- Heal a small amount

For the first slice, it is enough if rewards do one of:

- add a rune choice
- heal player
- improve next battle resource state

## 15. UX Requirements

The game lives or dies on readability. The player must understand why a ritual succeeded, failed, or transformed.

Critical UI requirements:

- hand clearly shows element and size
- ritual builder clearly shows slot order
- modifier slot is visually separate
- mana cost updates live while assembling ritual
- success/failure risk is visible before confirm
- final ritual preview explains expected outcome in plain language
- battle log records exact resolution

If the player cannot explain what happened, the combat system will feel unfair even when the math is correct.

## 16. Suggested Development Order

### Phase 1: Playable Combat Skeleton

- Create basic battle scene
- Create player and enemy stats
- Implement turn flow
- Implement hand draw
- Implement ritual slot selection
- Resolve simple single-rune effects

Exit condition:

- A player can enter battle, draw runes, play one rune, and defeat a simple enemy.

### Phase 2: Ritual System

- Add 2-rune and 3-rune chains
- Add modifier slot
- Add mana cost calculation
- Add base hybrid rules
- Add same-element instability
- Add overheat/failure

Exit condition:

- The full ritual loop works and can be tested repeatedly.

### Phase 3: Tower Slice

- Add floor progression
- Add encounter generation
- Add 3 archetypes
- Add boss floor
- Add simple reward flow

Exit condition:

- A full run from floor 1 to boss can be completed.

### Phase 4: Readability and Balance

- Improve combat preview
- Add battle log clarity
- Tune mana numbers
- Tune overheat toxicity
- Tune deck composition and enemy pacing

Exit condition:

- External playtesters can understand and finish the prototype.

## 17. Immediate Implementation Backlog

This is the best next concrete task breakdown for the current repo.

### Backlog A: Foundation

- Create folder structure under `Assets`
- Create `Boot`, `Battle`, and `Meta` scenes
- Create base enums and runtime data classes
- Create scriptable definitions for runes, modifiers, enemies

### Backlog B: Combat Core

- Build `BattleState`
- Build turn state machine
- Build hand draw logic
- Build ritual validation
- Build ritual resolution

### Backlog C: Presentation

- Hand UI
- Ritual slot UI
- Mana and HP HUD
- End turn and cast button flow
- Combat log

### Backlog D: Content

- Author 12 to 16 rune assets
- Author 4 modifier assets
- Author 3 enemy definitions
- Author first boss definition

### Backlog E: Tower Layer

- Floor progression state
- Encounter sequencing
- Reward selection
- Run failure and restart flow

## 18. Biggest Risks

These are the main risks we should watch early:

### 18.1 Randomness Feels Unfair

Mitigation:

- preserve 1 rune between turns
- show predicted ritual outcome
- keep deck small and legible

### 18.2 Overheat Feels Punitive

Mitigation:

- prefer reduced-power failure over full whiff
- show risk before cast
- keep Stabilization available early

### 18.3 Combo Rules Feel Opaque

Mitigation:

- battle log with exact explanations
- tooltip previews
- color-coded hybrid rule feedback

### 18.4 Enemy Intent Is Too Hidden

Mitigation:

- make archetypes visually distinct
- show enemy visible runes
- use consistent enemy behavior patterns

## 19. Success Criteria for Prototype

The prototype is successful if playtests confirm most of the following:

- Players understand the meaning of element and size quickly.
- Players feel they are solving a tactical hand each round.
- Saving 1 rune creates meaningful control.
- Modifier runes feel like rule-breaking tools, not passive stat boosts.
- Overheat adds tension without making the game frustrating.
- Enemy archetypes are readable even without explicit intent icons.
- Reaching the first boss feels like a meaningful test.

## 20. Best Next Step

The next implementation step should be:

`Build the combat skeleton first, not the tower layer.`

That means our practical order is:

1. create battle scene
2. create rune and modifier data definitions
3. implement draw and ritual assembly
4. implement ritual resolution
5. add 1 enemy and make combat playable

Only after that should we add multi-floor tower progression.
