# Synaptrace Unity Prototype Foundation

This document describes the first playable Unity foundation for Synaptrace, the dissertation artefact for reinforcement-learning-driven adaptive gameplay in a 2D platformer.

## What was created

- A playable `Main` scene at `game/Synaptrace/Assets/Scenes/Main.unity`.
- A runtime prototype bootstrapper that builds the current Layer 1 level when Play mode starts.
- A physics-based 2D player controller with horizontal movement, ground jumping, wall sliding, wall jumping, ground checks, and restart input.
- A surface modifier foundation that can later tune movement, friction, wall-slide speed, and wall-jump behaviour per platform or wall.
- A larger first level with a start point, styled platforms, spike hazards, a fall reset zone, wall-jump shafts, optional routing, and an elevated finish portal.
- Game flow scripts for level start, death, retry, respawn, and completion.
- Telemetry tracking for deaths, retries, jumps, hazard hits, elapsed time, completion time, and completion state.
- A lightweight debug HUD that displays the current run status and telemetry counters.
- Adaptation placeholder classes for static, rule-based, and future reinforcement-learning-driven difficulty.
- A committed `GameSystems` prefab containing the runtime bootstrap entry point.
- A Unity editor utility at `Synaptrace/Rebuild Prototype Scene` that rebuilds the bootstrap scene and keeps build settings aligned.

## Layer 1 visual pass

The prototype now has a cleaner, slightly futuristic visual identity:

- The player is a composite glowing avatar with a suit body, core, visor, feet, and a `Visual Root - Animation Ready` child object for later idle/run/jump animation work.
- Platforms use dark panel bases, cyan top highlights, side edge glows, lower shadows, and segmented panel seams.
- Hazards use red energy-field styling with spike silhouettes and warning strips.
- The finish object is a cyan portal/data gate with pylons, a glow aura, and a central data core.
- The background is a dark simulation space with subtle grid lines, distant panels, and low-alpha data nodes.
- The HUD uses a compact dark panel with lighter cyan text while preserving all telemetry fields.

All Layer 1 visuals are original procedural sprites generated at runtime by `RuntimePrototypeBootstrapper`. No external art assets were downloaded or imported.

## Expanded first-level layout

The first level is now significantly wider and more vertical. It is organised into named runtime sections in `RuntimePrototypeBootstrapper`:

- `01 Intro Section`: safe starting runway and start beacon.
- `02 Basic Jump Section`: three readable onboarding jumps with safe recovery space.
- `03 First Hazard Introduction`: a small spike gap with clear approach and exit platforms.
- `04 Wall Jump Tutorial Shaft`: a forgiving raised-entry shaft with a continuous safe floor, two clear walls, and a recovery ledge.
- `05 Mixed Platform Hazard Section`: a lower main route with a larger spike gap and safe landing decks.
- `06 Optional Upper Route`: a visible higher route that requires slightly better movement and rejoins before the final climb.
- `07 Final Vertical Climb`: a narrower raised-entry wall-jump climb with a recovery ledge leading directly to the finish platform.
- `08 Elevated Finish Area`: the final platform and finish portal above the starting height.

The layout is intended to be completed by a normal player in roughly 45-90 seconds once familiar with the controls. The main path runs left to right and then climbs upward, while the optional upper route can skip some of the lower hazard pressure. Both shaft entrances are now open at player height rather than blocked by their left walls. The section naming is deliberate: future adaptive difficulty work can vary gaps, route availability, hazard intensity, wall-interaction requirements, surface modifiers, or goal-section pacing by section.

## Spacing assumptions

The current controller uses a `7` unit horizontal move speed, `12` unit ground jump impulse, `3.2` gravity scale, and an `8.5 x / 11.5 y` wall-jump impulse. Based on those values, the expanded level keeps required ground gaps mostly around `1.5-2.8` units with broad landing platforms. The tutorial shaft is `2.3` units wide between wall faces with `1.55` units of entrance clearance; the final shaft is `2.0` units wide with `1.825` units of entrance clearance. Both clear the `1.18`-unit player collider and include a recovery ledge without creating a full staircase that bypasses wall interaction.

## Level-builder maintainability

The runtime-generated approach remains in place. Section methods now declare platforms through a small internal `PlatformSpec` structure, keeping names, positions, and sizes together while preserving the existing platform creation, colliders, procedural styling, and neutral `SurfaceModifier` setup.

## Current movement mechanics

- Ground movement uses `A/D` or the left/right arrow keys.
- Ground jump uses `Space`, `W`, or the up arrow.
- Wall slide occurs when the player is airborne, touching a platform side, and pressing toward that wall.
- Wall jump uses the normal jump input while wall sliding or touching a wall in the air. The jump pushes the player upward and away from the wall.
- Wall sliding is intentionally slow and forgiving for now so it can be tested easily.

The wall interaction is a controlled mechanic, not accidental side-sticking. Player/platform contacts use no-friction physics materials at runtime, while `PlayerController` applies explicit wall slide and wall jump rules. This keeps the behaviour readable and gives later adaptive difficulty work a clear tuning point.

`SurfaceModifier` is now attached to generated platforms with neutral default values. The player controller reads the modifier from the current ground or wall contact and applies its movement, jump, wall-slide, wall-jump, and contact-friction settings. Future levels can introduce water, oil, wet walls, sticky walls, or custom surfaces by changing those modifier values without rewriting the movement controller.

## How to open and run

1. Open Unity Hub.
2. Add/open the project folder `game/Synaptrace`.
3. Use Unity `6000.3.10f1`, matching `ProjectSettings/ProjectVersion.txt`.
4. Open `Assets/Scenes/Main.unity`.
5. Press Play.

Controls:

- Move: `A/D` or left/right arrows.
- Jump: `Space`, `W`, or up arrow.
- Wall jump: press jump while sliding/touching a wall in the air.
- Restart: `R` or the `Restart Level` HUD button.

## Main scripts

- `RuntimePrototypeBootstrapper.cs`: Builds the playable prototype objects at runtime so the scene can run even before authored art and prefab workflows are finalised.
- `PlayerController.cs`: Handles movement, ground jumping, wall sliding, wall jumping, ground/wall detection, respawning, and jump telemetry.
- `LevelManager.cs`: Owns level start, death, retry, respawn, and completion flow.
- `GameManager.cs`: Coordinates high-level run events, debug logs, HUD status, and telemetry calls.
- `TelemetryTracker.cs`: Stores the current run telemetry snapshot and JSON debug output.
- `Hazard.cs`: Reports player failure when a trigger hazard is touched.
- `GoalZone.cs`: Reports level completion when the player reaches the finish trigger.
- `CameraFollow2D.cs`: Keeps the camera following the player with simple clamps.
- `SurfaceModifier.cs`: Defines per-surface movement and wall-interaction multipliers for future environmental modifiers.
- `PrototypeHUD.cs`: Displays basic telemetry and restart controls.
- `DifficultyManager.cs` and `DifficultyProfile.cs`: Provide the first adaptation extension point. The current mode is static; rule-based and reinforcement-learning modes are placeholders.
- `PrototypeSceneGenerator.cs`: Optional editor tooling for rebuilding the bootstrap scene from Unity.

## Adaptation readiness

No reinforcement learning is implemented yet. The current design keeps adaptation separate from moment-to-moment player movement and level flow:

- `DifficultyManager` is the intended connection point for static, rule-based, and RL-driven adapters.
- `IDifficultyAdapter` defines the expected shape of future adapter implementations.
- `DifficultyProfile` shows how difficulty parameters can be externalised into ScriptableObjects.
- `TelemetryTracker` provides the run data that future adaptation or evaluation systems can consume or export.
- The runtime level hierarchy uses named sections so future systems can target intro, jump, first hazard, wall-jump tutorial, mixed hazard, optional route, final climb, and goal areas separately.
- `SurfaceModifier` gives future static, rule-based, or RL-driven systems a way to alter friction, movement control, wall-slide speed, and jump behaviour per surface or level section.

## Asset licences and credits

- External assets: none.
- Current visuals: original procedural runtime sprites created in this repository.
- Licence implications: no third-party art licence requirements for the current Layer 1 visual pass.

## Known limitations

- There are no authored sprite sheets yet.
- The player has no animation states yet, although the visual hierarchy is prepared for them.
- Wall slide and wall jump are functional but intentionally simple; there is no wall coyote time, stamina, or wall-climb system yet.
- Surface modifiers are structurally ready, but water, oil, wet-wall, and sticky-wall presets have not been authored into levels yet.
- Platform and hazard visuals are generated from simple procedural sprites, not final art.
- Telemetry is still in-memory/debug-log based and does not export to a file or database.
- The optional Unity menu tool rebuilds the runtime bootstrap scene; it does not author a fully hand-placed saved scene.
- The corrected shaft geometry has been reasoned from controller and collider values and compile-checked, but it still needs a final in-editor Play Mode feel pass.

## Suggested next steps

1. Open the project in Unity and confirm `Main.unity` enters Play mode cleanly.
2. Play through the complete main route, specifically checking both raised shaft entrances, recovery ledges, optional-route split/rejoin, mixed hazard gap, and portal approach.
3. For Layer 2, add simple idle, run, jump, fall, wall-slide, wall-jump, death, and portal-complete animations under the player's `Visual Root - Animation Ready` object.
4. Add authored player/platform/hazard sprites once the runtime visual direction is approved.
5. Add a simple pause/results screen and a structured telemetry export target.
6. Expand `DifficultyProfile` into static baseline configurations for the dissertation comparison.
7. Add a small set of authored `SurfaceModifier` presets for oil, water, wet walls, and sticky walls.
8. Add a rule-based adapter before implementing reinforcement-learning-driven adaptation.
