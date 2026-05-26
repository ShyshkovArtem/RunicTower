# Runic Tower

Runic Tower is a single-player tactical roguelite prototype built in Unity. The core idea is turn-based tower combat where the player builds rituals from a random hand of prebuild deck of elemental runes, balancing mana cost and combo value.

This project is in active development. I am using it as a portfolio piece because it best represents my current Unity/C# skills: gameplay systems, data-driven design, 3D interaction, UI feedback, and the process of turning a combat design into a playable vertical slice.

## Current Focus

The current development target is a playable vertical slice that answers one question:

> Is it fun to build rituals from a random rune hand inside a turn-based tower battle?

Everything in the project is being built around that combat loop first, before expanding into deeper progression, content, and polish.

## Gameplay Concept

- Draw a hand of elemental runes each turn.
- Place up to three elemental runes onto a ritual pedestal.
- Preview mana cost, and expected effect.
- Cast the ritual, resolve damage, shielding, healing, regeneration, or hybrid effects.
- Fight through tower encounters and improve the run over time.

The rune system currently uses four elements:

- Fire
- Air
- Earth
- Water

Runes also have sizes, which affect cost and power:

- Small
- Medium
- Large


## Implemented So Far

- Turn-based battle flow with player and enemy phases.
- Runtime battle state, combatant state, hand state, ritual builds, and ritual results.
- ScriptableObject definitions for elemental runes, modifiers, enemies, and encounters.
- Data-driven rune assets for elemental rune sizes.
- Ritual validation and calculation services.
- Hybrid ritual rule table for element combinations.
- Mana cost and combat effect resolution.
- Enemy decision service for enemy ritual selection.
- Tower progression service groundwork.
- 3D battle board presentation layer.
- Tappable 3D rune cards that move between hand anchors and ritual slots.
- Ritual pedestal with elemental slots.
- 3D ritual preview book flow.
- Combatant UI panels and ritual preview UI.
- Battle audio hooks and ritual VFX controller.

## Technical Highlights

- Unity 6000.3.10f1 project using URP.
- C# gameplay architecture split into data, services, combat flow, UI, and 3D presentation.
- ScriptableObject-authored content for runes, modifiers, enemies, and encounters.
- Plain C# runtime state objects for testable combat logic.
- Service classes for deck handling, hand drawing, ritual validation, ritual calculation, battle resolution, enemy decisions, and tower progression.
- Partial classes used to keep the 3D battle board controller manageable across cards, input, and preview behavior.
- Event-driven battle updates between the combat controller and presentation/UI layers.

## Project Structure

```text
Assets/
  Docs/                  Design and setup notes
  Resources/
    ScriptableObjects/   Rune, modifier, enemy, and encounter data
    Data/                Ritual combination data
  Scripts/
    Audio/               Battle sound hooks
    Combat/              Battle flow controller
    Core/                Shared gameplay enums
    Data/                Definitions and runtime state models
    Debug/               Battle logging helpers
    Editor/              Custom editor tooling
    Presentation3D/      3D rune board, cards, pedestal, preview book, VFX
    Services/            Gameplay logic services
    UI/                  Combat HUD and preview UI
```

## How To Open

1. Clone the repository.
2. Open the project in Unity `6000.3.10f1` or a compatible Unity 6 version.
3. Let Unity restore packages from `Packages/manifest.json`.
4. Open `Assets/Scenes/Test 1.unity`, which is the current scene included in build settings.
5. Press Play to test the current battle prototype.

The project is not a finished release build yet. Some scene wiring, balance values, assets, and progression features are still changing frequently.

## Development Roadmap

- Finish the first playable battle loop.
- Improve ritual readability and combat feedback.
- Expand enemy archetypes.
- Add stronger tower floor progression.
- Add reward choices after battles.
- Tune mana, overheat, and failure rules through playtesting.
- Add more visual polish, VFX, audio, and animation timing.
- Prepare a clean downloadable portfolio build.



## Status

Runic Tower is a work in progress, but it is already the project I would point to first when showing how I think about game systems. The most important part of the project is not just the fantasy theme, but the structure underneath it: a readable combat model, authored data, modular services, and a presentation layer that can evolve without rewriting the core rules.
