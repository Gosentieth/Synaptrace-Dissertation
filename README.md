# Synaptrace

Synaptrace is a 4th year Computer Science dissertation project focused on designing and evaluating a 2D platformer game with reinforcement learning-driven adaptive gameplay.

## Planned Project Objective
The project investigates how adaptive AI systems can dynamically modify gameplay elements based on player behaviour and performance in order to improve player engagement and overall game experience.

## Planned Core Features
- 2D platformer gameplay
- Adaptive difficulty system
- Reinforcement learning-driven gameplay adjustments
- Telemetry collection for player behaviour analysis
- Experimental evaluation of adaptive versus static gameplay

## Planned Repository Structure
- `game/` Unity project
- `ml/` reinforcement learning scripts, experiments, and model outputs
- `docs/` dissertation notes, design documents, and diagrams
- `data/` telemetry logs, test results, and evaluation data
- `assets-external/` source art and external creative assets
- `references/` academic references and supporting material

## Planned Technologies
- Unity
- C#
- Python
- Git/GitHub
- Aseprite

## Current Unity Prototype

An initial playable Unity prototype now exists under `game/Synaptrace`.

- Main scene: `game/Synaptrace/Assets/Scenes/Main.unity`
- Prototype documentation: `docs/unity-prototype-foundation.md`
- Core gameplay scripts: `game/Synaptrace/Assets/Scripts`

The current implementation is a clean base 2D platformer with movement, ground jumping, slow wall sliding, basic wall jumping, hazards, restart flow, completion detection, debug HUD feedback, telemetry tracking, and placeholder adaptation structure. The Layer 1 visual pass adds a dark sci-fi/simulation look, styled platforms, a glowing player avatar, spike-field hazards, a finish portal, and a larger first-level route with onboarding jumps, stabilized raised-entry wall-jump shafts, optional upper routing, mixed hazards, and an elevated finish. Player movement is now prepared for future surface modifiers such as water, oil, wet walls, or sticky walls without implementing those systems yet. It does not implement reinforcement learning yet.
