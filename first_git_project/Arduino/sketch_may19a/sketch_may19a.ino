const int buttonPin = 7;
const int ledPin = 13;

int lastButtonState = HIGH;

void setup() {
  pinMode(buttonPin, INPUT_PULLUP);
  pinMode(ledPin, OUTPUT);

  Serial.begin(9600);
}

void loop() {
  // === Arduino Button -> Unity ===
  int buttonState = digitalRead(buttonPin);

  if (buttonState != lastButtonState) {
    lastButtonState = buttonState;

    if (buttonState == LOW) {
      Serial.println("JUMP_DOWN");
    } else {
      Serial.println("JUMP_UP");
    }
  }

  // === Unity -> Arduino LED ===
  if (Serial.available() > 0) {
    String command = Serial.readStringUntil('\n');
    command.trim();

    if (command == "LED_ON") {
      digitalWrite(ledPin, HIGH);
    }

    if (command == "LED_OFF") {
      digitalWrite(ledPin, LOW);
    }
  }

  delay(10);
}