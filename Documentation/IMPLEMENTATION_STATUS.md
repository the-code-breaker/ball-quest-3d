# Responsive testing candidate 0.6.0 - implementation status

[Download the current testing APK](../BallQuest3D-Responsive-v0.6.0.apk). Exact version, bytes, SHA-256 and package checks are in [responsive-release.json](../TestResults/responsive-release.json); see [DELIVERY_V6.md](DELIVERY_V6.md).

The current source uses swipe velocity with filtering before saturation, stronger drive/braking and lower rolling drag. Short strokes respond promptly; repeated swipes build actual momentum. All directions use the same force controller. The camera turns smoothly toward travel, including reverse; each gesture retains a stable world frame through the turn. Fresh or resumed strokes use the new view. Save version 6 selects the new defaults once while preserving progression and later explicit preferences.

Road visuals remove overlapping coplanar patches while retaining the complete padded collision solids. Gravity Lab uses a continuous visible loop. Deck normals, UVs/filtering, shadow filtering/cascades and camera depth range are corrected. Raised gate headers and fading world labels preserve the chase-camera view.

The 55-course campaign, five signature adventures, five racing tracks/formats, Physics Lab, native interface, skins, music, material audio and progression remain. All 87 distinct Unity tests passed: 25 EditMode and 62 PlayMode. All 55 campaign courses and 15 adventure variants completed with no falls. Actual touch speed, camera output, moving-render captures and Editor timings are recorded, not inferred from compilation.

Phone touch comfort, hardware-specific visual confirmation, lifecycle and sustained mobile profiling remain unverified. Contact audio still uses temporary procedural samples. Release signing/distribution, final Foley, unfinished tournament process-death resume and iOS/Mac tooling remain separate work. Editor validation and a debug APK are not a production-readiness claim.
