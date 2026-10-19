# Baseline: Unity 6000.6.6f1, original player (macOS, development build)

Scenario (`-autotest`, `DevPerfProbe`): Title -> Game with 3 mods + 1 track, 20 pose switches (every 2.5 s, a character switch every 4th), back to Title, repeated twice.

Result: 6477 frames; 42 frames over 33 ms; the repeated ~33.3 ms entries are a 30 fps cadence, so the count that matters is frames over 40 ms.

Summary (original player): **14 frames over 40 ms, 4 over 100 ms, worst 557.6 ms** (excluding the ~2 s app startup frame).
Worst stalls occur right after pose/character changes during play and at Title -> Game transitions.
