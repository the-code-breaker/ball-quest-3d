# Contact sound and impact presentation, v0.9

The reported car-like sound came from sustained 35-186 Hz oscillator fundamentals and a large speed-to-pitch sweep. The new original procedural Foley uses filtered aperiodic friction and irregular short contact grains for rolling and slipping. Resonant tones are confined to short, decaying impact transients. Speed changes mainly adjust presence; the rolling pitch is constrained to 0.88-1.14 across all surfaces.

These are newly generated sounds, not recordings or copied audio from another game. The compatibility class name TemporaryBallAudio remains, while the profile provenance and file names identify Contact Foley v9. Six existing profile assets migrate once, preserving their serialized identities. Future generation preserves profiles already marked v9.

## Sources and audition

BallAudioAssetGenerator creates 34 mono source WAV files in Assets/_BallQuest/Audio/SFX/ContactFoleyV9. Thirty unique clips are referenced by the six runtime profiles: eighteen ball-material clips, eight surface impact clips and four surface-dominant rolling textures for ice, glass, sand and grass. Four additional surface rolling sources support comparison/audition. Unreferenced source assets outside Resources do not need to enter the player package.

Project generation also creates TestResults/Audio/ContactFoleyV9-Audition.wav, a dry 42-second comparison with no music. ContactFoleyV9-Audition.txt identifies each three-second section: two seconds of rolling then an impact, first for six ball materials and then for eight surfaces. This is a source-waveform audition aid; it is not a recording of Android speaker output.

Loops use preloaded compressed data and small impact clips use PCM. The source files are already mono, avoiding forced downmix normalization. Unity 6 keeps preloadAudioData in AudioImporterSampleSettings; see the [official API reference](https://docs.unity.cn/6000.0/Documentation/ScriptReference/AudioImporterSampleSettings.html).

## Physical contact and animation

BallContactAudio reads platform-relative tangent speed and contact slip. Airborne/stationary balls remain silent, loops crossfade at surface changes, and impact playback uses four fixed voices with a cooldown. The material identity for an impact comes from the collider actually struck, independently of the material currently supporting the ball. A metal wall can therefore sound metallic while the ball remains on wood.

BallContactFeedback shows a short expanding contact ripple with the collision normal and surface tint. The ripple begins at 0.65-metre diameter and expands to 1.15-2.15 metres, depending on impulse, during its 0.24-second lifetime. This keeps the ground contact ring visible beyond the ball without changing physical scale. Hard surfaces add small chips/sparks. Six ripple slots and one 32-particle system are created once; no cosmetic colliders, ball-root scaling or Rigidbody modifications are introduced. Effects live outside the ball renderer hierarchy and are disposed with the component. Physical collision animation continues when sound is muted. This implementation deliberately uses contact animation rather than deforming the physics sphere.

## Validation

ContactFoleyV9Tests checks finite amplitude, aperiodicity/low-frequency tone concentration, impact decay and migrated profile references. It writes TestResults/contact-foley-spectrum-v9.json. TactileContactV9Tests checks a real metal-wall collision over a wooden floor, muted collision animation, bounded effect resources/disposal and restrained pitch change. It writes TestResults/tactile-contact-v9.json. Existing MaterialAudioTests retain their grounding, slip, voice-budget, pause/mute and standalone-lab checks.

Those checks establish signal and lifecycle properties. They do not establish human sound preference, phone speaker reproduction or a perceptual match to another game's audio. Consult the release report for executed results; source implementation alone is not a passing Unity result.

### Executed focused validation

The three ContactFoleyV9Tests passed in [EditMode-v9.xml](../TestResults/EditMode-v9.xml), which contains 34 passing EditMode cases. All four new TactileContactV9Tests and all four retained MaterialAudioTests passed in [FirstPresentation-v9.xml](../TestResults/FirstPresentation-v9.xml), a 23-case focused PlayMode run. These eleven audio-related cases are not additional cases to add to the full release count.

The [source-waveform measurements](../TestResults/contact-foley-spectrum-v9.json) recorded RMS levels from 0.0415 to 0.1670, maximum narrow-band tone energy fraction 0.00432 in the scanned 25-350 Hz range, and maximum tested absolute autocorrelation 0.03571. These are the generated waveform properties, not recorded output from phone speakers.

The [physical contact measurements](../TestResults/tactile-contact-v9.json) confirmed a real metal-wall impact while the ball was supported by wood, visible contact feedback with sound muted, six fixed ripple slots with at most three simultaneously active in that sequence, and resource disposal with the ball. Rolling playback pitch changed from 0.9813 to 1.0095 between the tested low/high speeds, a change below three percent. The original [audition WAV](../TestResults/Audio/ContactFoleyV9-Audition.wav) remains available for human listening; no human audition or phone audio result is claimed here.

The final tactile/native rerun passed all five cases in 10.535 seconds after the ripple-diameter refinement: [FinalTactile-v9.xml](../TestResults/FinalTactile-v9.xml). It covers all four tactile-contact cases and the native capture fixture. The [final contact image](../TestResults/Previews/V9ContactFeedback.png) shows the enlarged ring around the actual rendered ball. The 96-case retained regression also passed; current evidence contains 132 distinct passing Unity cases overall, with focused reruns counted once. Android speaker output and human sound preference remain unverified.
