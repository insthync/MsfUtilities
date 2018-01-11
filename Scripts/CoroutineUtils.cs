using System.Collections;
using UnityEngine;

public class CoroutineUtils
{

    public static IEnumerator StartWaiting(float time,
        System.Action doneCallback,
        float increment, System.Action<float> incrementCallback,
        bool countUp = true)
    {
        float timeElapsed = 0f;
        float timeRemaining = time;

        while (timeRemaining > 0f)
        {
            yield return new WaitForSeconds(increment);
            timeRemaining -= increment;
            timeElapsed += increment;
            incrementCallback.Invoke(countUp ? timeElapsed : timeRemaining);
        }
        doneCallback.Invoke();
    }

    public static IEnumerator StartWaiting(float time, System.Action doneCallback)
    {
        yield return new WaitForSeconds(time);
        doneCallback.Invoke();
    }
}
