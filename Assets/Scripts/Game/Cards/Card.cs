﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Card
{
    public int Id;
    public string Name;
    public string Text;

    public Card(int id)
    {
        Id = id;
    }

    public abstract void Action(Match model, Player self, Player enemy);
}
