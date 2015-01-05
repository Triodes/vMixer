const int potPin = A0;
const byte buttonPins[5] = {A1,A2,A3,A4,A5};
boolean Connected = false;
int isAlive = 0;

void setup()
{
  //Enable pullup on all button pins
  for (int i = 0; i < 5; i++)
  {
    digitalWrite(buttonPins[i],HIGH); 
  }
  
  //set all ledpins to output
  for (int i = 2; i < 14; i++)
  {
    pinMode(i,OUTPUT);
  }
  
  //open the serial port
  Serial.begin(9600);
}

unsigned long previous = 0, current = 0;
int elapsed1 = 0, elapsed2 = 0;
void loop()
{
  previous = current;
  current = millis();
  if (Connected)
  {
    elapsed1 += (current - previous);
    if (elapsed1 > 10)
    {
      elapsed1 = 0;
      processButtons();
      processPot();
    }
    Liveness(current - previous);
  }
  else
  {
    elapsed2 += (current - previous);
    if (elapsed2 > 600)
    {
      elapsed2 = 0;
      IndicateScan();
    }
  }
  Poll();
}


int buttonValues[5];
int oldButtonValues[5];
void processButtons() //read the button values and report changes (if any)
{
  for (int i = 0; i < 5; i++)
  {
    oldButtonValues[i] = buttonValues[i];
    buttonValues[i] = digitalRead(buttonPins[i]);
    if (buttonValues[i] == LOW && buttonValues[i] != oldButtonValues[i])
    {
      byte arr[] = {1,i+1};
      Serial.write(arr,2);
    }
  }
  if (buttonValues[0] == LOW && buttonValues[4] == LOW)
  {
    byte arr[] = {2}; 
    Serial.write(arr,1);
  }
}


int sensorValue = 0;
int potValues[5];
byte potOut = 0, potOld = 0;
void processPot() //read the pot value and report changes (if any)
{
  int potAvg = 0;
  for(int i = 0; i < 4; i++)
  {
    potValues[i] = potValues[i+1];
  }
  potValues[4] = analogRead(A0);
  for(int i = 0; i < 5; i++)
    potAvg += potValues[i];
  potAvg /= 5;
  potOld = potOut;
  potOut = constrain(map(potAvg, 5, 1018, 0, 255),0,255);
  if (potOld != potOut)
  {
    byte arr[] = {0,potOut}; 
    Serial.write(arr,2);
  } 
}

void Poll() //Handle incoming messages
{
  if (Serial.available() > 0)
  {
    byte type = Serial.read();
    if (type == 0) //update given LED
    {
      delay(10);
      byte state = Serial.read();
      byte mask = B10000000;
      for (int i = 2; i < 10; i++)
      {
         if ((state & mask) > 0)
           digitalWrite(i, HIGH);
         else
           digitalWrite(i,LOW);
         mask = mask >> 1;
      }
    }
    else if (type == 1)
    {
      delay(10);
      int state = Serial.read();
      digitalWrite(10,state);
    } 
    else if (type == 125)
    {
      isAlive = 0;
    }
    else if (type == 255)
    {
      delay(10);
      int randomVal = Serial.read();
      Serial.write(randomVal);
      int i = 0;
      while (Serial.available() <= 0 && i < 50)
      {
        delay(10);
        i++;
      }
      int m = Serial.read();
      if (m == 1)
      {
        int leds[] = {6,7,8,9};
        Flash(leds, 4, 1000);
        Connected = true;
      }
    }
  }
}

void Flash(int leds[], int nLeds, int ms)
{
  for (int i = 2; i <= 10; i++)
  {
    digitalWrite(i, LOW);
  }
  for (int i = 0; i < nLeds; i++)
  {
    digitalWrite(leds[i], HIGH);
  }
  delay(ms);
  for (int i = 2; i <= 10; i++)
  {
    digitalWrite(i, LOW);
  }
}

void Liveness(int elapsed)
{
  isAlive += elapsed;
  if (isAlive > 2500)
    Disconnect();
}

void Disconnect()
{
  Connected = false;
  isAlive = 0;
  elapsed1 = 0;
  elapsed2 = 0;
  int leds[] = {2,3,4,5};
  Flash(leds, 4, 1000);
}

boolean on;
void IndicateScan()
{
 digitalWrite(10,!on);
 on = !on;
}
