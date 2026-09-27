Youtube: https://youtu.be/l7iAX4bRHMo

# Emberbound - The Obsidian Duel

A small 2.5D Unity dragon duel made for the supplied Dexhigh technical assessment. The blue dragon is the player; Cinder is a distance-aware AI opponent. Built for **Unity 6000.3.6f1**, Built-in Render Pipeline, Windows x64, legacy Input Manager.

## Open and play

1. Add this folder in Unity Hub and open with Unity 6000.3.6f1, with an active license.
2. Open `Assets/Scenes/Duel.unity`. The saved scene shows the arena, blue & red DragonUsurper models, lighting, camera and editable Canvas HUD before Play. Runtime replaces the arena preview with fresh match objects and keeps the saved UI.
3. If input settings have been changed, set **Active Input Handling** to **Input Manager (Old)** or **Both** in Player Settings.

## Controls

| Input | Action |
| --- | --- |
| WASD | Move on the arena plane |
| Facing | Automatically faces the opponent; mouse aiming is not needed |
| 1 or Q | Fire breath: 24 damage, 6.5-unit frontal cone, 3.5-second cooldown |
| 2 or E | Tail sweep: 32 damage, 3-unit radius, 5-second cooldown, stronger knockback |
| 3 or R | Skyfall: leap toward the rival's cast-time position up to 7 units away, 42 damage in a 2.8-unit landing radius, 9-second cooldown |
| Escape | Pause / resume |
| M | Toggle sound |

## Landing shake and collected audio

R/3 (Skyfall) triggers a 0.42-second camera shake when the dragon reaches the ground, including missed attacks. The shake decays smoothly and returns to the original camera position. Ground impact, particles, damage and the end of airborne immunity share the same landing moment. Smaller damage shakes cannot overwrite the stronger landing impulse.

The supplied `Assets/Audio` clips are assigned in `Assets/Resources/DuelAudioSettings.asset`:

- `FireAttack.mp3`: fire emission.
- `TailAttack.mp3`: tail sweep impact.
- `FlyAttackSound.mp3`: flight launch.
- `BGmusic.mp3`: continuous background music, streamed at 15% volume.

Effects play at 75% volume. Long attack recordings are bounded to the action and fade out over their last 0.12 seconds; source files are unchanged. A short synthesized thud reinforces landing and hits. Edit Effects Volume / Music Volume in the audio settings asset to rebalance them. M mutes both; Escape pauses audio and camera shake with gameplay. Audio source/licensing attribution should be supplied by the user who collected the clips.

## DragonUsurper team models

Configured from the user's imported **Four Evil Dragons Pack PBR**, version 1.3 (package ID 78923, as recorded in its asset metadata).

- `Assets/Resources/PlayerDragonSkin.asset`: DragonUsurper/Blue.prefab.
- `Assets/Resources/EnemyDragonSkin.asset`: DragonUsurper/Red.prefab.
- Each configuration contains its prefab, scale, ground offset and animation mappings.
- Assigned clips: idle01, Walk, attackFlame, attackHand, FlyFlame and Die. The pack has no named tail-sweep clip; attackHand is combined with the procedural full-body spin for that ability.
- Attack clip time follows gameplay attack progress. Idle and walking loop; death holds its last pose.

The arena, icons, effects and synthesized sounds are original procedural content. The dragon models, textures and animation clips come from the imported package.
## Audio 

The audios was downloaded from the internet and edited via an online audio editor.

## AI usage note

Codex was used to read the brief, write the C# implementation and supporting scripts.

One correction during code review: the initial bootstrap used `RuntimeInitializeOnLoadMethod(AfterSceneLoad)`, which runs for initial startup rather than every subsequent scene reload. That would leave a restarted empty scene without a match controller. It was changed to register a `SceneManager.sceneLoaded` handler before initial load, so every reload creates a new match. Runtime restart verification is still pending -> lead to empty scene.

AI implemented an additional feature on its own the player dragon was facing where the cursor was, which was not intented, I corrected it and made it so that the dragons always face eachother pulling agro.

AI accelerated the first implementation and repeatable checks.

This project was created by Adish Sharma as part of the technical assessment
This repository is provided solely for the purpose of reviewing and evaluating my technical assessment and development skills.
The source code and project materials may not be copied, redistributed, published, or used for commercial purposes without my explicit permission.
© 2026 Adish Sharma. All rights reserved.
