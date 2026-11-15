# Wrist device wiring

Status: **proposed from the parts list, not built or tested.** Correct this file when the first device is wired.

## Parts for one device

| Part | Notes |
| --- | --- |
| Seeed XIAO ESP32-C3 | Bluetooth Low Energy only; has battery pads and a charger on the board |
| MPU-6050 module | From the Arduino prototype |
| Flex sensor 2.2 inch (SEN-10264) | Replaces the infrared sensor: a bent finger holds the web |
| Resistor, about 22 kΩ | Second half of the flex sensor's voltage divider |
| Push button | Recentre |
| Coin vibration motor (C0611) with a small transistor and resistor | Optional. Do not drive the motor from a pin directly |
| LiPo battery 3.7 V 500 mAh | Soldered to the BAT pads on the back of the XIAO; charges over USB-C |
| Elastic velcro band 5 × 40 cm | Strap |

## Pin table

| XIAO pin | Connects to |
| --- | --- |
| 3V3 | MPU-6050 VCC, one end of the flex sensor |
| GND | MPU-6050 GND, the 22 kΩ resistor, the button, the motor transistor |
| D4 (SDA) | MPU-6050 SDA |
| D5 (SCL) | MPU-6050 SCL |
| A0 (D0) | Other end of the flex sensor **and** the 22 kΩ resistor to GND |
| D1 | Recentre button, other side to GND |
| D2 | Transistor base through about 1 kΩ; the motor sits between 3V3 and the transistor |
| BAT+ / BAT− | LiPo battery |

The pin names are the ones in `wrist_firmware.ino`; change both together.

## To check on the first build

- Which way the flex reading moves when the finger bends, and the two thresholds.
- Whether the MPU-6050 module runs properly from 3.3 V.
- Battery life on 500 mAh at 100 reports a second.
