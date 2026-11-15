// Wrist web shooter firmware
//
// Board:   Seeed XIAO ESP32-C3
// Sensors: MPU-6050 (hand direction) and a flex sensor (finger bent = hold the web)
// Link:    Bluetooth Low Energy gamepad. Windows pairs it like any controller and the
//          game reads it with no extra program.
//
// What the game receives (this must match Game/Assets/Scripts/Game/WristDevice.cs):
//     X axis    yaw    -180..+180 degrees, sent as -32767..+32767
//     Y axis    pitch   -90..+90  degrees, sent as -32767..+32767
//     button 1  HOLD      down while the finger is bent
//     button 2  RECENTRE  down while the recentre button is pressed
//
// The game decides what "screen centre" is and how far the hand must turn to reach the
// screen edge, so this file only has to report angles. Tune feel in the game, not here.
//
// STATUS: written as a starting point. NOT compiled and NOT run on a device yet.
//
// Libraries (Arduino IDE > Library Manager):
//     ESP32-BLE-Gamepad 0.8.0 by lemmingDev (it installs NimBLE-Arduino as well)

#include <Arduino.h>
#include <Wire.h>
#include <BleGamepad.h>

// ---------------------------------------------------------------------------
// Settings: change these first
// ---------------------------------------------------------------------------

// Each device needs its own name so each laptop pairs with the right one.
const char *DEVICE_NAME = "SWING-1";

// Pins (XIAO ESP32-C3 labels). The MPU-6050 uses the board's I2C pins: SDA = D4, SCL = D5.
const int PIN_FLEX = A0;        // flex sensor voltage divider (see ../../wiring.md)
const int PIN_RECENTRE = D1;    // push button to ground; uses the internal pull-up
const int PIN_VIBRATION = D2;   // vibration motor through a transistor; -1 if not fitted

// Flex sensor: open the Serial Monitor, bend and straighten the finger, and read the
// "flex" number. Put HOLD_ABOVE a little below the bent value and RELEASE_BELOW a little
// above the straight value. Two different numbers stop the web flickering on and off.
const int FLEX_HOLD_ABOVE = 2400;
const int FLEX_RELEASE_BELOW = 2100;
// Set to true if the reading goes DOWN when the finger bends.
const bool FLEX_BENT_IS_LOWER = false;

// How the sensor sits on the hand. Assumed: flat on the back of the hand, chip's X arrow
// pointing towards the fingers, chip facing up. If the reticle moves the wrong way, flip
// InvertYaw / InvertPitch in the game instead of editing the maths here.

const unsigned long REPORT_EVERY_MS = 10;       // 100 reports a second
const unsigned long CALIBRATION_MS = 2000;      // keep the hand still this long at power-on
const float PITCH_TRUST_IN_GYRO = 0.98f;        // 0..1; lower follows the accelerometer faster
const bool PRINT_DEBUG = true;                  // angles and flex value on the Serial Monitor

// ---------------------------------------------------------------------------
// MPU-6050 (read directly over I2C, no library)
// ---------------------------------------------------------------------------

const uint8_t MPU_ADDRESS = 0x68;
const float GYRO_COUNTS_PER_DEGREE_PER_SECOND = 65.5f;   // at the +-500 deg/s range set below

struct Motion {
  float accelX, accelY, accelZ;   // in g
  float gyroX, gyroY, gyroZ;      // in degrees per second
};

float gyroOffsetX = 0, gyroOffsetY = 0, gyroOffsetZ = 0;

void writeMpuRegister(uint8_t reg, uint8_t value) {
  Wire.beginTransmission(MPU_ADDRESS);
  Wire.write(reg);
  Wire.write(value);
  Wire.endTransmission();
}

bool readMotion(Motion &m) {
  Wire.beginTransmission(MPU_ADDRESS);
  Wire.write(0x3B);                         // first data register (ACCEL_XOUT_H)
  if (Wire.endTransmission(false) != 0) return false;
  if (Wire.requestFrom((int)MPU_ADDRESS, 14) != 14) return false;

  int16_t raw[7];                           // accel x y z, temperature, gyro x y z
  for (int i = 0; i < 7; i++) {
    uint8_t high = Wire.read();
    uint8_t low = Wire.read();
    raw[i] = (int16_t)((high << 8) | low);
  }
  m.accelX = raw[0] / 16384.0f;
  m.accelY = raw[1] / 16384.0f;
  m.accelZ = raw[2] / 16384.0f;
  m.gyroX = raw[4] / GYRO_COUNTS_PER_DEGREE_PER_SECOND - gyroOffsetX;
  m.gyroY = raw[5] / GYRO_COUNTS_PER_DEGREE_PER_SECOND - gyroOffsetY;
  m.gyroZ = raw[6] / GYRO_COUNTS_PER_DEGREE_PER_SECOND - gyroOffsetZ;
  return true;
}

void setupMpu() {
  writeMpuRegister(0x6B, 0x00);   // wake up
  writeMpuRegister(0x1B, 0x08);   // gyro range +-500 degrees per second
  writeMpuRegister(0x1C, 0x00);   // accelerometer range +-2 g
  writeMpuRegister(0x1A, 0x03);   // built-in low-pass filter, about 44 Hz
}

// A resting gyro does not read exactly zero. Average it while the hand is still and
// subtract that from every later reading; otherwise yaw drifts quickly.
void calibrateGyro() {
  float sumX = 0, sumY = 0, sumZ = 0;
  int count = 0;
  unsigned long start = millis();
  while (millis() - start < CALIBRATION_MS) {
    Motion m;
    if (readMotion(m)) {
      sumX += m.gyroX; sumY += m.gyroY; sumZ += m.gyroZ;
      count++;
    }
    delay(5);
  }
  if (count > 0) {
    gyroOffsetX = sumX / count;
    gyroOffsetY = sumY / count;
    gyroOffsetZ = sumZ / count;
  }
}

// ---------------------------------------------------------------------------
// Hand direction
// ---------------------------------------------------------------------------

float yawDegrees = 0;     // left/right. Only the gyro measures this, so it drifts slowly.
float pitchDegrees = 0;   // up/down. Gravity corrects this, so it does not drift.

// REPLACE THIS FUNCTION to try a different way of working out the hand direction
// (for example the method from the Arduino prototype, or a BNO085 sensor).
void updateOrientation(const Motion &m, float seconds) {
  yawDegrees += m.gyroZ * seconds;
  if (yawDegrees > 180) yawDegrees -= 360;
  if (yawDegrees < -180) yawDegrees += 360;

  float pitchFromGravity = atan2f(-m.accelX, sqrtf(m.accelY * m.accelY + m.accelZ * m.accelZ)) * 180.0f / PI;
  float pitchFromGyro = pitchDegrees + m.gyroY * seconds;
  pitchDegrees = PITCH_TRUST_IN_GYRO * pitchFromGyro + (1 - PITCH_TRUST_IN_GYRO) * pitchFromGravity;
}

// ---------------------------------------------------------------------------
// Finger and button
// ---------------------------------------------------------------------------

bool holding = false;

bool readHold(int flex) {
  if (FLEX_BENT_IS_LOWER) flex = 4095 - flex;
  if (!holding && flex > FLEX_HOLD_ABOVE) holding = true;
  else if (holding && flex < FLEX_RELEASE_BELOW) holding = false;
  return holding;
}

unsigned long vibrateUntil = 0;

void vibrate(unsigned long milliseconds) {
  if (PIN_VIBRATION < 0) return;
  digitalWrite(PIN_VIBRATION, HIGH);
  vibrateUntil = millis() + milliseconds;
}

// ---------------------------------------------------------------------------
// Bluetooth gamepad
// ---------------------------------------------------------------------------

BleGamepad gamepad(DEVICE_NAME, "SSHS AI Club", 100);

int16_t toAxis(float degrees, float degreesAtFullScale) {
  float value = constrain(degrees / degreesAtFullScale, -1.0f, 1.0f);
  return (int16_t)(value * 32767);
}

void setupGamepad() {
  BleGamepadConfiguration config;
  config.setControllerType(CONTROLLER_TYPE_GAMEPAD);
  config.setAutoReport(false);            // we send one report with everything in it
  config.setButtonCount(2);
  config.setHatSwitchCount(0);
  config.setWhichAxes(true, true, false, false, false, false, false, false);   // X and Y only
  config.setAxesMin(-32767);
  config.setAxesMax(32767);
  gamepad.begin(&config);
}

// ---------------------------------------------------------------------------

unsigned long lastReport = 0;
unsigned long lastMicros = 0;

void setup() {
  Serial.begin(115200);
  pinMode(PIN_RECENTRE, INPUT_PULLUP);
  if (PIN_VIBRATION >= 0) pinMode(PIN_VIBRATION, OUTPUT);

  Wire.begin();
  setupMpu();
  Serial.println("Keep the hand still: calibrating the gyro");
  calibrateGyro();
  vibrate(150);                            // a short buzz: ready
  setupGamepad();
  lastMicros = micros();
}

void loop() {
  // 1. Hand direction, as often as possible so fast movements are not missed.
  Motion m;
  unsigned long now = micros();
  float seconds = (now - lastMicros) / 1000000.0f;
  lastMicros = now;
  if (readMotion(m)) updateOrientation(m, seconds);

  if (vibrateUntil != 0 && millis() > vibrateUntil) {
    digitalWrite(PIN_VIBRATION, LOW);
    vibrateUntil = 0;
  }

  // 2. A report to the game 100 times a second.
  if (millis() - lastReport < REPORT_EVERY_MS) return;
  lastReport = millis();

  int flex = analogRead(PIN_FLEX);
  bool wasHolding = holding;
  bool hold = readHold(flex);
  bool recentre = digitalRead(PIN_RECENTRE) == LOW;
  if (hold && !wasHolding) vibrate(40);   // a tick when the web fires

  if (gamepad.isConnected()) {
    gamepad.setX(toAxis(yawDegrees, 180));
    gamepad.setY(toAxis(pitchDegrees, 90));
    if (hold) gamepad.press(BUTTON_1); else gamepad.release(BUTTON_1);
    if (recentre) gamepad.press(BUTTON_2); else gamepad.release(BUTTON_2);
    gamepad.sendReport();
  }

  if (PRINT_DEBUG) {
    Serial.printf("yaw %7.1f  pitch %6.1f  flex %4d  hold %d  recentre %d\n",
                  yawDegrees, pitchDegrees, flex, hold, recentre);
  }
}
