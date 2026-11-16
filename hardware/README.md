# Hardware: the wrist web shooter

Everything needed to rebuild a device lives here: firmware, wiring and the pin table.

Status: **starting point only.** The Arduino Uno prototype (MPU-6050 and an infrared sensor, on a USB cable) works outside this repository. The firmware here is a first version for the new parts; it has not been compiled or run on a device.

## How the device talks to the game

The ESP32 presents itself to Windows as a **Bluetooth gamepad**. Windows pairs it under Bluetooth settings like any controller, and the game reads it as a controller. There is no bridge program, no Wi-Fi and no COM port. The device works out the hand's direction itself and sends only the result.

| Signal | Sent as | Meaning |
| --- | --- | --- |
| Yaw | Stick X axis | Hand turned left or right. −180° to +180° over the full axis. |
| Pitch | Stick Y axis | Hand tilted down or up. −90° to +90° over the full axis. |
| Hold | Button 1 | Down while the finger is bent; the game holds the web for as long as it is down. |
| Recentre | Button 2 | The hand's current direction becomes the middle of the screen. |

The game also recentres at every countdown and when **C** is pressed on the keyboard.

The game decides how far the hand must turn to reach the screen edge, which way is positive, and how much to smooth. Those settings are at the top of [`WristDevice.cs`](../Game/Packages/com.sshs.manhattan-swing/Runtime/Game/WristDevice.cs), so feel can be tuned without reflashing a device.

Each device has its own name (`SWING-1`, `SWING-2`, `SWING-3`), set at the top of the firmware. The game uses any controller whose name contains `SWING`.

## Working without a device

Any ordinary gamepad stands in for the device: the left stick moves the reticle and the bottom face button holds the web. The game shows "Stand-in gamepad" at the bottom of the screen.

## Files

| File | What it is |
| --- | --- |
| [`wrist/firmware/wrist_firmware/wrist_firmware.ino`](wrist/firmware/wrist_firmware/wrist_firmware.ino) | Firmware for the XIAO ESP32-C3. Settings are at the top; the function that works out the hand direction is marked for replacement. |
| [`wrist/wiring.md`](wrist/wiring.md) | Parts and pin table |

## Flashing a device

1. Install the Arduino IDE and add the ESP32 boards (Boards Manager, "esp32" by Espressif).
2. Install the library **ESP32-BLE-Gamepad 0.8.0** by lemmingDev. It installs NimBLE-Arduino too.
3. Open `wrist_firmware.ino`, set `DEVICE_NAME`, choose the board **XIAO_ESP32C3** and upload.
4. Open the Serial Monitor at 115200 to see angles and the flex reading, and set the two flex thresholds.
5. In Windows, add a Bluetooth device and pick `SWING-1`.

Write down the board package version that works the first time, here, so every device is built the same way.

## Changes from the prototype

| Prototype | Now | Why |
| --- | --- | --- |
| Arduino Uno on a USB cable | XIAO ESP32-C3 over Bluetooth, on a battery | Wireless and small enough to wear |
| Infrared sensor, a short cover fires once | Flex sensor, bent finger holds and straight finger lets go | Swinging needs hold and release |
| Cover the sensor for 2 seconds to recentre | A separate button, plus automatic recentre at the countdown | A long swing would have recentred in mid-air |
| The PC program turned angles into aim | The game does it | One place to tune |

## Rules for this folder

- Anyone with the parts list can rebuild a device in one session. If a step lives only in someone's head, write it down.
- Pin the board package and library versions.
- No personal paths, COM port numbers or device addresses in committed files.
- Write "not tested" when something has not been tried on a real device.

## Still to decide

- Keep the MPU-6050 or move to a BNO085, after measuring yaw drift over a 3-minute run.
- Whether vibration is worth the extra wiring.
- A cable fallback, if Bluetooth proves unreliable in a crowded room.
