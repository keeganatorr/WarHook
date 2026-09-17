# WarHook UFO mode — progress

Branch: `ufo-abduction`

This file is a compact project-status snapshot. Durable implementation details still belong in `AGENTS.md`; this is for what is done now and what should happen next.

## Current state

The UFO variant has moved well beyond the original prototype. The current branch is a playable incremental arcade loop with persistent progression, a large tech web, round results, save slots, web support, and several new defensive/mechanical systems.

### Core flight loop — implemented

- 288×216 UFO playfield with horizontal/vertical flight and eased banking.
- Downward weapon fire and a tractor-beam abduction mechanic.
- Soldiers, engineers, aimed rockets, collision sweeps, beam carrying/dropping, and engineer repairs.
- Original bean-spawn schedule/RNG retained as the base enemy-event system.
- Abductions accelerate the difficulty clock while the reward/survival timer remains separate.
- No UFO score/high-score system; progression is based on round-end CREW rewards.
- Soldier Value upgrades increase the CREW value of future soldier abductions.

### Risk / defence systems — implemented

- `DANGER BELOW` altitude line and side-volley punishment system.
- Flight Clearance upgrade lowers the danger line.
- Shield Array absorbs a rocket and regenerates; later ranks shorten regen.
- Point Defence improves rocket interception and produces nearby missile blasts.
- Rocket impacts add directional knockback and short camera shake.
- Destroyed UFO transitions into a visible crash-landing state with smoke/fire behind the results tally.

### Round economy / results — implemented

- CREW reward is based on collected soldiers × soldier value × multiplier.
- Base multiplier grows during active play and pauses correctly.
- Growth, starting-multiplier, long-haul, interest and exponential progression effects are integrated.
- Round rewards bank before the tally animation, preventing duplicate payouts.
- Results screen reveals collected crew, multiplier and final reward before entering upgrades.

### Saves / platform support — implemented

- Three progression save slots.
- New Game / Continue flow with overwrite confirmation.
- Continue allows launch-music selection.
- Legacy single-save progression migrates into Save 1 while retaining the old save as backup.
- Desktop JSON and browser localStorage persistence.
- Web build remains based on KNI BlazorGL; physical-key mapping is preserved for gameplay controls.

## Mothership Matrix

**Chosen name for the UFO upgrade screen: `MOTHERSHIP MATRIX`.**

The underlying system is already substantially implemented:

- 34-node data-driven technology web including the permanently owned UFO Core.
- Explicit world-space node positions rather than array/grid placement.
- Multiple prerequisites and any-N prerequisite gates.
- Existing 20 upgrade IDs retained for save compatibility.
- Branches: Weapons, Beam, Ship, Hull, Yield and Hybrid.
- Branch-coloured connectors with partial prerequisite progress.
- Spatial keyboard/controller navigation.
- Free mouse drag/pan.
- Discrete 0.5× / 1× / 2× zoom.
- Clickable overview/minimap.
- Dedicated 576×432 upgrade render target while gameplay remains 288×216.
- Code-drawn pixel icons, with existing UFO artwork reused for the core/capstone.
- Mothership Link is the late-game hybrid node and requires any three completed branch capstones.

### Current tech highlights

**Weapons**
Rapid Fire → Pulse Accelerator / Point Defence → Twin Cannons → Plasma Cycler → Particle Array.

**Beam**
Tractor Drive → Wide Aperture / Dual Abduction → Beam Focus / Triple Abduction → Quad Abduction → Beam Matrix → Fleet Abduction.

**Ship / Hull**
Thrusters → Ion Engines / Flight Clearance → Warp Core, plus Hull Plating → Reinforced Frame / Engineer Tools / Shield Array → Nanohull → Auto Repair.

**Yield**
Survival Dividend → Compound Returns / Launch Dividend / Soldier Value → Long Haul / Colony Dividend → Interest Engine → Exponential Yield.

**Hybrid**
Mothership Link.

## Most recent branch work observed

- Siege mode removed from the visible UFO flow; older mode values migrate to the remaining UFO mode.
- Continue flow can choose music.
- Regenerating Shield Array added.
- Separate flailing/falling abductee sprites added.
- UFO score display/system removed in favour of CREW progression.
- Soldier Value upgrade added.
- Hull HUD changed to a rectangular health bar.
- `DANGER BELOW` warning/line made more visually prominent.

## Next pass

1. **Apply the `MOTHERSHIP MATRIX` name in-game.** Replace generic `UPGRADES` / `Tech Web` wording where appropriate without changing save IDs or progression semantics.
2. **Visual-polish the Matrix toward the approved concept direction:** dark navy CRT/arcade presentation, stronger branch identity, brighter unlocked paths, clearer selected-node framing, cleaner locked-node treatment, and a denser research-network feel.
3. **Keep the graph readable at the real 288×216 logical resolution.** The 576×432 Matrix target should improve crispness without making labels too dense.
4. **Polish the detail panel.** Prioritise title, current → next effect, rank, cost/prerequisites and BUY state; avoid paragraph-heavy descriptions.
5. **Improve graph composition rather than adding nodes just for quantity.** Preserve meaningful forks/reconnections and conspicuous milestone upgrades.
6. **Run smoke checks after UI changes**, especially graph reachability, multi-parent gates, any-three capstone logic, save migration, map mouse transforms, web input and browser audio.
7. **Capture updated screenshots** for gameplay, results and Mothership Matrix once the visual pass is complete.

## Design rule

The Matrix should feel like an incremental machine, not a conventional RPG skill tree: frequent inexpensive stat growth should lead into noticeable mechanical milestones such as extra abductees, multi-shot weapons, shield/repair systems, wider/faster beam behaviour and stronger economy effects.
