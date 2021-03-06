﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class M01_Red : Minion
{

    public M01_Red(Match model, Player owner, Player enemy, int orderNum) : base(model, owner, enemy, orderNum)
    {
        Name = "Red";
        Text = "Deals 4 damage to the enemy.";
        Type = MinionType.Red;
        Color = Color.red;
    }

    public override void Action()
    {
        Model.DealDamage(this, Enemy, 4);
    }
}
