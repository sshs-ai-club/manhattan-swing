# Hardware: the wrist web shooter

Everything needed to rebuild a device lives here: firmware, wiring and the pin table. Firmware is committed here from its first version, not kept in a personal project.

Status: **plan only.** The Arduino + HC-05 prototype works outside this repository. The move to ESP32 over Bluetooth is in progress. Dated tasks are in the [plan](../docs/PLAN.md#web-shooter-bluetooth).

## How the device talks to the game

Proposed, not yet tested (assumption A7 in [DECISIONS](../docs/DECISIONS.md)): the ESP32 presents itself to Windows as a **Bluetooth Low Energy gamepad**. Windows pairs it under Bluetooth settings like any controller, and the game reads it as a joystick. There is no bridge program, no Wi-Fi hotspot and no COM port.

| Signal | Sent as | Meaning |
| --- | --- | --- |
| Yaw | Stick X axis | Right is positive. The full axis range maps to the device's yaw range. |
| Pitch | Stick Y axis | Up is positive. |
| Hold | Button 1 | Down while the player holds the web; up lets go. |
| Recentre | Button 2 | The current direction becomes the middle of the screen. |

Each device advertises its own name (`SWING-1`, `SWING-2`, `SWING-3`) so each booth laptop pairs with the right one.

Because the device looks like a gamepad, any ordinary gamepad can stand in for it while writing the game side.

If the first-week test shows this does not work, the fallback is a Bluetooth serial link that sends the old `yaw,pitch,fire` text line, with a small bridge program on the laptop.

## Layout

```text
hardware/
  wrist/
    firmware/     ESP32 sketch, pinned board package and library versions, flashing steps
    wiring.md     parts list, pin table, photo or diagram
```

## Rules for this folder

- Anyone with the parts list can rebuild a device in one session. If a step lives only in someone's head, write it down.
- Pin the board package and library versions. "Latest" breaks later.
- No personal paths, COM port numbers, device addresses or passwords in committed files.
- Write "not tested" when something has not been tried on a real device.

## Still to decide

- Which ESP32 board (Q4), and gamepad or serial (Q5).
- Motion sensor: keep the MPU-6050 or move to a BNO085, after the 3-minute drift test.
- How the player fires, and where the recentre button goes (Q6).
- Power: a small battery or a power bank. Many power banks switch off at the ESP32's low current, so test the one you plan to use.
