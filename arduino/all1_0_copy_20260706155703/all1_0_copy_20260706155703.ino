#include <Servo.h>

Servo myServo;
const int potPin = A0; // 电位器引脚固定在 A0

// --- 舵机推杆控制变量 ---
int potValue = 0;
int controlValue = 0;  // 【新增】统一反转方向后的控制值
int angle = 90;
int direction = 1;
unsigned long previousServoMillis = 0;

// --- 舵机摆动幅度设置 ---
// 以 90 度为中心，上下各摆动 SERVO_AMPLITUDE 度。
// 数值越小幅度越小、越不容易卡住。例如 20 -> 摆动范围 70~110 度。
const int SERVO_CENTER = 90;
const int SERVO_AMPLITUDE = 50;   // 想更小就调小，想更大就调大
const int SERVO_MIN = SERVO_CENTER - SERVO_AMPLITUDE;
const int SERVO_MAX = SERVO_CENTER + SERVO_AMPLITUDE;

// 旋钮回到低端时的“静止/复位”阈值。controlValue 低于这个值就回中 90 度并停摆。
// 如果拧到底舵机还在动，说明电位器到不了这么低，把这个值调大（比如 100、150）。
const int REST_THRESHOLD = 80;

// --- Unity 串口通信变量 ---
int lastValue = -1;
unsigned long previousUnityMillis = 0; 
const int unityInterval = 30;          

void setup() {
  Serial.begin(9600);     
  myServo.attach(9);      
  pinMode(potPin, INPUT); 
  myServo.write(90);      
}

void loop() {
  // 1. 读取原始值
  potValue = analogRead(potPin);
  
  // 【核心修正】直接在这里进行 1023 减法反转！
  // 这样 controlValue 就能完美符合直觉：越顺时针数值越大，越逆时针数值越小
  controlValue = 1023 - potValue; 
  
  unsigned long currentMillis = millis(); 

  // ==================== 🛠️ 第一部分：Unity 串口发送逻辑 ====================
  if (currentMillis - previousUnityMillis >= unityInterval) {
    previousUnityMillis = currentMillis;

    // 直接使用已经反转好方向的 controlValue
    int sensorValue = controlValue; 
    
    if (abs(sensorValue - lastValue) > 2) { 
      Serial.println(sensorValue);
      lastValue = sensorValue; 
    }
  }

  // ==================== 🦖 第二部分：大舵机推杆自动旋转逻辑 ====================
  // 【静止区判别】旋钮拧回低端时（低于 REST_THRESHOLD），彻底静止并复位到中心 90 度
  if (controlValue <= REST_THRESHOLD) {
    angle = SERVO_CENTER;      // 同步角度变量，下次启动不会突跳
    direction = 1;             // 复位摆动方向
    myServo.write(SERVO_CENTER);
    return;
  }

  // 防止顺时针拧到底时的极限跳变
  int safeControlValue = controlValue;
  if (safeControlValue > 980) {
    safeControlValue = 980;
  }

  // 映射速度：controlValue 越大（越顺时针），延迟越短 (6ms)，推杆运动越狂暴
  int speedDelay = map(safeControlValue, 41, 980, 25, 6);

  // 舵机专属的时间电闸
  if (currentMillis - previousServoMillis >= speedDelay) {
    previousServoMillis = currentMillis;

    angle += direction;

    // 小幅摆动范围（以 90 度为中心，上下各 SERVO_AMPLITUDE 度）
    if (angle >= SERVO_MAX) direction = -1;
    if (angle <= SERVO_MIN) direction = 1;

    myServo.write(angle);
  }
}