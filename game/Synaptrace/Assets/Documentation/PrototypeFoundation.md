# Synaptrace Prototype Foundation

The playable prototype is documented in `docs/unity-prototype-foundation.md` at the repository root.

Open `Assets/Scenes/Main.unity` and press Play. The scene uses `RuntimePrototypeBootstrapper` to create the expanded Layer 1 level, player avatar visuals, styled platforms, hazards, elevated finish portal, background, managers, HUD, and telemetry objects at runtime.

The player now supports slow wall sliding and a basic wall jump. Use the normal jump input while sliding or touching a wall in the air.

Generated platforms also receive a default `SurfaceModifier` component. It currently preserves standard movement, but it gives later levels and adaptive-difficulty experiments a clean hook for water, oil, wet-wall, sticky-wall, or custom surface behaviour.

The current level route is intentionally wider and more vertical than the original blockout. It moves through intro onboarding, basic jumps, a first spike field, a wall-jump tutorial shaft, a mixed hazard section, an optional upper route, a final vertical climb, and a higher finish portal.

Both wall-jump entrances now use raised entrance walls above continuous safe floors. The player can walk naturally into each shaft, use the opposing walls and recovery ledge, and exit toward the next route without climbing an exterior wall or crossing a blocked transition.
