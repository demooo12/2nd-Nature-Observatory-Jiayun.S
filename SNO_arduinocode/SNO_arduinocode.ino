#include <Servo.h>

Servo myServo;
const int potPin = A0; // 电位器引脚A0

// 舵机推杆
int potValue = 0;
int controlValue = 0; 
int angle = 90;
int direction = 1;
unsigned long previousServoMillis = 0;

// 摆动幅度
const int SERVO_CENTER = 90;
const int SERVO_AMPLITUDE = 55;   // 这里可以调整
const int SERVO_MIN = SERVO_CENTER - SERVO_AMPLITUDE;
const int SERVO_MAX = SERVO_CENTER + SERVO_AMPLITUDE;

const int REST_THRESHOLD = 80;

// Unity通信！
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
  
  controlValue = 1023 - potValue; 
  
  unsigned long currentMillis = millis(); 

  // ==================== 🛠️ 第一部分：Unity 串口发送逻辑 ====================
  if (currentMillis - previousUnityMillis >= unityInterval) {
    previousUnityMillis = currentMillis;
    int sensorValue = controlValue; 
    
    if (abs(sensorValue - lastValue) > 2) { 
      Serial.println(sensorValue);
      lastValue = sensorValue; 
    }
  }

  // ==================== 🦖 第二部分：大舵机推杆自动旋转逻辑 ====================
  // 旋钮拧回低端时（低于 REST_THRESHOLD），彻底静止并复位到中心 90 度
  if (controlValue <= REST_THRESHOLD) {
    angle = SERVO_CENTER;  
    direction = 1;         
    myServo.write(SERVO_CENTER);
    return;
  }

  // 防止顺时针拧到底时的极限跳变
  int safeControlValue = controlValue;
  if (safeControlValue > 980) {
    safeControlValue = 980;
  }

  // 映射速度：controlValue 越大 延迟越短，推杆运动越狂暴
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