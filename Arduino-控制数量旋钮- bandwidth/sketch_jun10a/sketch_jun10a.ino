int potPin = A0; // 电位器信号脚接在 A0
int lastValue = -1;

void setup() {
  Serial.begin(9600); // 开启串口通信，波特率定为 9600
}

void loop() {
  // 【核心修改】用 1023 减去读取到的模拟值，直接反转方向！
  int sensorValue = 1023 - analogRead(potPin); 
  
  // 只有当数值发生变化时才发送，防止串口数据堵塞
  if (abs(sensorValue - lastValue) > 2) { 
    Serial.println(sensorValue);
    lastValue = sensorValue;
  }
  
  delay(30); // 30毫秒的采样率，足够保证 Unity 端丝滑流畅
}