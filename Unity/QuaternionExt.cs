using UnityEngine;

public static class QuaternionExt
{
    public static Quaternion Average(this Quaternion[] qArray)
    {
        if (qArray.Length == 0)
        {
            Debug.LogWarning("Trying to average Quaternion array of Length 0");
            return Quaternion.identity;
        }

        Quaternion qAvg = qArray[0];
        for (int i = 1; i < qArray.Length; i++)
        {
            qAvg = Quaternion.Slerp(qAvg, qArray[i], 1.0f / (float)(i + 1));
        }
        return qAvg;
    }
}