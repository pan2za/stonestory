using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// copy from https://gitee.com/yuanzhi0515/playground/blob/master/pommerman/constants.py
// https://gitee.com/yuanzhi0515/playground/blob/master/docs/environment.md
public enum PommermanItem
{
    Passage = 0,
    Rigid = 1,
    Wood = 2,
    // above 3 will be show in board[x,y]
    // below all will not be show in board.
    Bomb = 3,
    Flames = 4, //火焰，此处有火焰，火焰可以消失
    Fog = 5, 
    // all the ExtraBomb, IncrRange, Kick will not revealed until the Wood has been removed.
    ExtraBomb = 6,
    IncrRange = 7, //增加长度的道具 
    Kick = 8,
    AgentDummy = 9,
    // below 4 will be show in board[x, y]
    Agent0 = 10,
    Agent1 = 11,
    Agent2 = 12,
    Agent3 = 13,
    // Not show
    // Wood + ExtraBomb will not revealed to anyone, but the programmer himself.
    // Wood + IncrRange
    //No Speed up
    SpeedUp = 14,
}
