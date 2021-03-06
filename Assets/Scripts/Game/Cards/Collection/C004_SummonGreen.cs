﻿using System.Collections;
using System.Collections.Generic;
using System;

public class C004_GreenBlood : Card
{

    public C004_GreenBlood()
    {
        Id = 4;
        Name = "Green+";
        Text = "Summon a Green.";
        Cost = 2;
    }

    public override void Action(Match Model, Player self, Player enemy)
    {
        Model.SummonMinions(self, new List<Tuple<MinionType, Player>>() {
            new Tuple<MinionType, Player>(MinionType.Green, self),
        }, summonProtection: true);
    }
}
