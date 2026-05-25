# Battle Board 3D Setup Guide

This is the active battle presentation path for the project.

`BattleFlowController` keeps all battle logic.
`BattleBoard3DController` is the 3D presentation and input layer on top of it.

## 1. Active 3D Scripts

Current 3D battle scripts in `Assets/Scripts/Presentation3D`:

- `BattleBoard3DController`
- `RuneSpot3D`
- `RitualPedestal3D`
- `RuneCard3D`
- `RitualPreviewBook3D`

Current overlay UI scripts still used by the 3D version:

- `CombatantPanelUI`
- `RitualPreviewPanelUI`
- `UIRuneVisuals`

## 2. Recommended Scene Hierarchy

```text
BattleRoot
  BattleFlowController
  BattleBoard3DController

Battle3D
  PlayerRuneAnchor_01
  PlayerRuneAnchor_02
  PlayerRuneAnchor_03
  PlayerRuneAnchor_04
  PlayerRuneAnchor_05
  PlayerUpcomingRuneAnchor_01
  PlayerUpcomingRuneAnchor_02
  PlayerUpcomingRuneAnchor_03
  PlayerModifierAnchor

  EnemyRuneAnchor_01
  EnemyRuneAnchor_02
  EnemyRuneAnchor_03
  EnemyRuneAnchor_04
  EnemyRuneAnchor_05
  EnemyModifierAnchor

  RitualPedestal
    RitualSlot_01
    RitualSlot_02
    RitualSlot_03
    RitualModifierSlot

  RuneRoot
```

The player and enemy rune anchors can be anywhere in world space.
They do not need a table under them.
For your current setup they can float behind and above the character.

## 3. Rune Spot Setup

Every hand anchor and pedestal slot should use `RuneSpot3D`.

Optional children:

- `Anchor`
- `EmptyVisual`
- `OccupiedVisual`

Assign in each `RuneSpot3D`:

- `anchor` -> exact transform where the rune should rest
- `emptyVisual` -> optional visual for empty state
- `occupiedVisual` -> optional visual for filled state

If `anchor` is empty, the spot object transform is used.

## 4. Ritual Pedestal Setup

On the pedestal root add `RitualPedestal3D`.

Assign:

- `elementalSpots` -> 3 elemental ritual spots in order
- `modifierSpot` -> 1 modifier ritual spot

## 5. Rune Prefab Setup

Create a 3D rune prefab, for example `RuneCard3D_Prefab`.

Recommended prefab contents:

- root object with `RuneCard3D`
- collider on root or any child
- child `VisualRoot`
- child `SmallBodyRoot`
- child `MediumBodyRoot`
- child `LargeBodyRoot`
- child `ModifierBodyRoot`
- one or more child `SpriteRenderer` objects for face symbols
- optional world-space TextMeshPro for mana cost
- optional selected highlight object

`VisualRoot` should contain every visible object that should:

- float in idle animation
- scale during flight
- rotate slightly during idle motion

Typical contents inside `VisualRoot`:

- active body mesh root
- face symbol objects
- selected highlight
- any glow or particle child objects attached to the rune itself

Assign in `RuneCard3D`:

- `smallBodyRoot`
- `mediumBodyRoot`
- `largeBodyRoot`
- `modifierBodyRoot`
- `visualRoot`
- `symbolRenderers`
- `tintRenderers`
- `costLabel` optional
- `selectedHighlight` optional
- `interactionCollider`

Card hand scale, ritual scale, modifier ritual scale, and idle float tuning are controlled on
`BattleBoard3DController` under `Rune Card Presentation`, not on individual rune prefab instances.

Shape rule currently supported:

- small elemental rune -> `SmallBodyRoot`
- medium elemental rune -> `MediumBodyRoot`
- large elemental rune -> `LargeBodyRoot`
- modifier rune -> `ModifierBodyRoot`

Current element tint setup:

- water -> blue
- air -> green
- earth -> brown
- fire -> red
- modifier -> purple

If your material supports `_BaseColor` or `_Color`, the script can tint it automatically.

## 6. Battle Board Controller Setup

Add `BattleBoard3DController` on `BattleRoot` or a dedicated controller object.

Assign:

- `battleController` -> object with `BattleFlowController`
- `interactionCamera` -> main battle camera
- `cardRoot` -> parent object where spawned rune objects live, usually `RuneRoot`
- `runeCardPrefab` -> your rune prefab
- `playerHandElementSpots` -> 5 player rune anchors
- `playerUpcomingRuneSpots` -> optional 3 anchors showing the next runes in the player deck cycle
- `playerModifierSpot` -> player modifier anchor
- `enemyHandElementSpots` -> 5 enemy rune anchors
- `enemyModifierSpot` -> enemy modifier anchor
- `ritualPedestal` -> pedestal object with `RitualPedestal3D`

Tune spawned rune presentation on the controller:

- `cardHandScale`
- `cardUpcomingScale`
- `cardRitualScale`
- `modifierRitualScale`
- `playCardIdleInHand`
- `playCardIdleInUpcoming`
- `cardIdlePositionAmplitude`
- `cardIdleRotationAmplitude`
- `cardIdlePositionFrequency`
- `cardIdleRotationFrequency`
- `cardUpcomingIdlePositionAmplitude`
- `cardUpcomingIdleRotationAmplitude`
- `cardUpcomingIdlePositionFrequency`
- `cardUpcomingIdleRotationFrequency`

Optional overlay UI:

- `playerPanel`
- `enemyPanel`
- `previewPanel`
- `roundLabel`
- `previewButton`
- `castButton`
- `skipTurnButton`
- `ritualPreviewBook`

If `ritualPreviewBook` is assigned, the old flat `previewPanel` is hidden automatically.

## 7. Overlay UI Setup

`CombatantPanelUI` only needs:

- `healthFillImage`
- `healthLabel`
- `manaFillImage`
- `manaLabel`
- status badge objects

Recommended HP/MP bar structure:

- frame image
- child fill image using `Image Type = Filled`
- text on top like `12/20`

`RitualPreviewPanelUI` only needs:

- `manaCostLabel`
- `successChanceLabel`
- `finalEffectLabel`

## 8. Ritual Book Setup

Add `RitualPreviewBook3D` to your small book object in the scene.

Assign:

- `bookRoot` -> the transform that should physically fly and scale
- `focusPoint` -> a transform in the center of the scene where the book opens
- `interactionCollider` -> collider used for tap detection
- `pageContentRoot` -> object holding the page text labels
- `runeLineLabel`
- `manaCostLabel`
- `successRateLabel`
- `effectLabel`
- `animator`

Animator setup expected by script:

- state `Open`
- state `Flip`
- state `Close`

If your state names are different, just change the strings in the inspector.

Recommended flow:

- book starts small in its original scene position
- `pageContentRoot` starts disabled
- tap small book or press assigned `previewButton`
- book flies to `focusPoint`, scales up, plays `Open`, then `Flip`
- page text becomes visible and stays there
- tap again or press button again
- book plays `Close`, flies back, scales down, and hides page text again
- animation duration fields control real clip playback speed, not only script wait times
- while the book is open or animating, battle interaction is locked until it closes

## 9. Interaction Behavior

Current supported behavior:

- tap player elemental rune in air -> flies to first free ritual slot
- tap player modifier rune in air -> flies to ritual modifier slot
- tap rune on pedestal -> flies back to its original air anchor
- player runes cycle through the deck: the opening hand is shuffled once, consumed runes refill after casting, and the next runes can be shown in `playerUpcomingRuneSpots`
- enemy runes are visible but not interactable
- runes idle-float while waiting in hand/air anchors
- runes scale down while flying onto the pedestal
- tap ritual book -> opens ritual preview in 3D
- press `Cast` -> casts the selected valid ritual only
- press `Skip Turn` -> passes the player turn when no runes or modifier are selected

## 10. First Play Test

1. Press Play.
2. Confirm battle starts automatically.
3. Confirm player runes appear on player anchors.
4. Confirm enemy runes appear on enemy anchors.
5. Tap a player rune.
6. Confirm it flies to the pedestal instead of teleporting.
7. Tap that rune on the pedestal.
8. Confirm it flies back and resumes idle motion.
9. Tap more runes quickly.
10. Confirm multiple flights can overlap.
11. Tap the ritual book or press the preview button.
12. Confirm it flies to the center, opens, flips pages, and shows preview text.
13. Tap it again.
14. Confirm it closes and returns to its original place.
15. Press `Cast`.
16. Confirm player turn resolves, then enemy turn resolves, then next round starts.

## 11. Notes

- the old 2D battle UI path is no longer part of this setup
- this version uses tap interaction only
- if a rune does not appear, first check its assigned spot and collider
- if a rune does not tint, check whether its material exposes `_BaseColor` or `_Color`
