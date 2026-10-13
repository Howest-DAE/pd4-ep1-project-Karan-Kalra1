using System;

public class CrownCountModel
{
    public event Action<int> CountChanged;

    public int Count { get; private set; }

    public bool HasCrowns => Count > 0;

    public void SetCount(int count)
    {
        int clampedCount = Math.Max(0, count);

        if (Count == clampedCount)
        {
            return;
        }

        Count = clampedCount;
        CountChanged?.Invoke(Count);
    }
}