const int potPin = A0;
const byte buttonPins[5] = {A1,A2,A3,A4,A5};
boolean Connected = false;
int isAlive;

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
void loop()
{
  previous = current;
  current = millis();
  if (Connected)
  {
    processButtons();
    delay(5);
    processPot();
    delay(5);
    Liveness(current-previous);
  }
  else
  {
     IndicateScan();
     delay(100);
  }
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

void serialEvent() //Handle incoming messages
{
   byte type = Serial.read();
   if (type == 0) //update given LED
   {
     delay(10);
     byte pin = Serial.read();
     delay(10);
     byte val = Serial.read();
     digitalWrite(pin,val);
   } //Connect
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
     while (!Serial.available() || i < 50)
     {
       delay(10);
       i++;
     }
     int m = Serial.read();
     if (m == 1)
     {
       for (int i = 2; i <= 10; i++)
       {
         digitalWrite(i, HIGH);
       }
       delay(1000);
       for (int i = 2; i <= 10; i++)
       {
         digitalWrite(i, LOW);
       }
       Connected = true;
     }
   }  
}

void Liveness(int elapsed)
{
  isAlive += elapsed;
  if (isAlive > 2000)
  {
    Connected = false;
    isAlive = 0;
    for (int i = 2; i <= 10; i++)
    {
      digitalWrite(i, LOW);
    }
  } 
}

int lp[8] = {2,3,4,5,9,8,7,6};
int index = 0, index2 = 1;
void IndicateScan()
{
  digitalWrite(lp[index], LOW);
  index++;
  index2++;
  if (index > 7)
    index = 0;
  if (index2 > 7)
    index2 = 0;
  digitalWrite(lp[index2], HIGH);
}
