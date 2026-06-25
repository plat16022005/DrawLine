using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TrapDatabase", menuName = "InkKnight/Trap Database")]
public class TrapDatabase : ScriptableObject
{
    public List<TrapOption> trapOptions;

    public TrapOption GetTrapOptionById(string id)
    {
        foreach (TrapOption option in trapOptions)
        {
            if (option.id == id)
                return option;
        }

        return null;
    }
}