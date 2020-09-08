enum ConditionType
{
    LitIndicator,   //"If the bomb has a lit SND indicator".
    UnlitIndicator, //"If the bomb has an unlit CAR indicator".
    Port,           //"If the bomb has a serial port".
    EmptyPlate,     //"If the bomb has an empty port plate".
    BatteryCount,   //"If the bomb has at least 3 batteries".
    SerialParity,   //"If the last digit of the bomb's serial number is even".
    SerialVowel     //"If the bomb's serial number contains a vowel."
}

sealed class Condition
{
    public ConditionType Type;
    public int ConditionParam;
    public int[] Sounds;
}