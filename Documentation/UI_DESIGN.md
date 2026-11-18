# Arcade interface design

The new interface follows the supplied visual reference family: a full-bleed sunny wooden skyway, blue city scenery, dimensional orange/cyan title, glossy lime/gold/blue buttons, dark blue collection panels, outlined white lettering, and gold reward stars. It replaces the prior apricot editorial layout rather than reskinning that layout.

The home screen uses the generated HomeBackdrop texture with a live rotating BallPreview, profile/wallet bar and side actions. Secondary screens share a compact back/header/wallet row. Worlds use the four-strip WorldAtlas; the default all-level view uses a dense three-column grid, while world filters display only actual courses. The ball collection has one live large preview, cached three-dimensional thumbnails, actual ownership/unlock state, category filters and pagination.

Missions are driven by ArcadeProgress aggregate game statistics. Claims mutate the actual local save and cannot be repeated. Daily rewards use the seven-day schedule and show claimed/current state. The free spin grants once per day; the wheel ends on the actual awarded sector and the reward is saved before animation so navigation interruptions cannot lose it. Shop prices use earned coins or gem-to-coin exchanges. No mock payment or advertisement flows are presented.

Gameplay keeps the scene visible with a compact blue progress bar, outlined level label and coin wallet. Short-lived swipe guidance explains the controls. Pause, fail and completion screens dim the live world. Result ribbons are native vector meshes; completion uses layered stars, an unscaled pop animation and native confetti. Press animations work while game time is paused. Settings control actual audio, vibration, sensitivity and graphics preferences.

QuestUI.cs owns screen composition. ArcadeRibbonGraphic, ArcadeWheelGraphic, ArcadeHandGraphic, ArcadePanelStroke, ArcadeContactShadowGraphic and ArcadeConfetti are code-native scalable graphics with CanvasRenderer dependencies. BallPreview and the generated texture assets are provided by the art pack. Borders emit only contour geometry, preserving transparent dark reward panels. Native feathered contact shadows ground the home/splash/failure ball portraits. A solid white sprite lets the native HUD fill animate from zero without stretching a circle. The UI never renders a prerecorded screenshot as gameplay.

Physical-device safe areas, thumb reach, text density and GPU load still require phone testing; editor render captures are the current visual evidence.

