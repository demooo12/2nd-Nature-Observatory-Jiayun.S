#include <Servo.h>

Servo myServo;
const int potPin = A0; // 电位器引脚固定在 A0

// --- 舵机推杆控制变量 ---
int potValue = 0;
int controlValue = 0;  // 【新增】统一反转方向后的控制值
int angle = 90;
int direction = 1;
unsigned long previousServoMillis = 0; 

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
  // 【静止区判别】使用全新的 controlValue。最逆时针到底时（小于40），彻底静止在 90 度
  if (controlValue <= 40) {
    myServo.write(90);
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

    // 大行程推杆范围 (0度 到 180度) —— 舵机满行程摆动
    if (angle >= 180) direction = -1;
    if (angle <= 0)   direction = 1;

    myServo.write(angle); 
  }
}