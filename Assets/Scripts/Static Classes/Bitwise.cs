using UnityEngine;

public static class Bitwise
{
    public static int GetSetBitCount(long lValue)
    {
        int iCount = 0;

        //Loop the value while there are still bits
        while (lValue != 0)
        {
            //Remove the end bit
            lValue = lValue & (lValue - 1);
            iCount++;
        }

        return iCount;
    }
}
