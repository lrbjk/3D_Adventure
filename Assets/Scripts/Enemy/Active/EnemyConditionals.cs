using Astraparasitus;
using BehaviorDesigner.Runtime.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyConditionals : Conditional
{
    public EnemyBase enemy;
    public override void OnAwake()
    {
        enemy = GetComponent<EnemyBase>();
    }
}

public class DetectedPlayer : EnemyConditionals
{
    public override TaskStatus OnUpdate()
    {
        if (enemy.aiDetect.isDetected)
        {
            return TaskStatus.Success;
        }
        else
        {
            return TaskStatus.Failure;
        }
    }
}
public class InBreak : EnemyConditionals
{
    public override TaskStatus OnUpdate()
    {
        if (enemy.GetBreakState())
        {
            return TaskStatus.Success;
        }
        else
        {
            return TaskStatus.Failure;
        }
    }
}
public class InNoLeg : EnemyConditionals
{
    public override TaskStatus OnUpdate()
    {
        if (enemy.GetLegBreakState() == true)
        {
            return TaskStatus.Success;
        }
        else
        {
            return TaskStatus.Failure;
        }
    }
}
public class CanAttack : EnemyConditionals
{
    //public float distance;
    public override TaskStatus OnUpdate()
    {
        if ((enemy.aiDetect.GetCanAttack() == true)&&(enemy.GetEndurance()>0))
        {
            return TaskStatus.Success;
        }
        else
        {
            return TaskStatus.Failure;
        }
    }
}
public class CanFarAttack : EnemyConditionals
{
    //public float distance;
    public override TaskStatus OnUpdate()
    {
        if ((enemy.aiDetect.GetCanFarAttack() == true) && (enemy.GetEndurance() > 0) && enemy.enemyModel.CheckWeapon())
        {
            return TaskStatus.Success;
        }
        else
        {
            return TaskStatus.Failure;
        }
    }
}
public class IsBreakHit : EnemyConditionals
{
    public override TaskStatus OnUpdate()
    {
        if (enemy.GetHit() == true)
        {
            return TaskStatus.Success;
        }
        else
        {
            return TaskStatus.Failure;
        }
    }
}
public class IsDead : EnemyConditionals
{
    public override TaskStatus OnUpdate()
    {
        if (enemy.GetDie() == true)
        {
            return TaskStatus.Success;
        }
        else
        {
            return TaskStatus.Failure;
        }
    }
}
public class HaveWeapon: EnemyConditionals
{
    public override TaskStatus OnUpdate()
    {
        if (enemy.enemyModel.CheckWeapon())
        {
            return TaskStatus.Success;
        }
        else
        {
            return TaskStatus.Failure;
        }
    }
}
public class EmptyEndurance : EnemyConditionals
{
    public override TaskStatus OnUpdate()
    {
        if(enemy.GetEndurance() <= 0)
        {
            return TaskStatus.Success;
        }
        else
        {
            return TaskStatus.Failure;
        }
    }
}
public class InTC : EnemyConditionals
{
    public bool notInTc;
    public override TaskStatus OnUpdate()
    {
        if (!notInTc)
        {
            if ((enemy.aiDetect.GetCanAttack() == true) && (enemy.GetEndurance() > 0) && !enemy.aiDetect.useDistanceCheck)
            {
                return TaskStatus.Success;
            }
            else
            {
                return TaskStatus.Failure;
            }
        }
        else
        {
            if (enemy.aiDetect.useDistanceCheck)
            {
                return TaskStatus.Success;
            }
            else
            {
                return TaskStatus.Failure;
            }
        }
        
    }
}
public class CanAttack_Pro : EnemyConditionals
{
    //public float distance;
    public float rate = 0.5f;
    public override void OnStart()
    {
        if (rate >= Random.Range(0.0f, 1.0f))
        {
            enemy.aiDetect.useDistanceCheck = false;
        }
        else
        {
            enemy.aiDetect.useDistanceCheck = true;
        }
    }
    public override TaskStatus OnUpdate()
    {
        if ((enemy.aiDetect.GetCanAttack() == true) && (enemy.GetEndurance() > 0))
        {
            return TaskStatus.Success;
        }
        else
        {
            return TaskStatus.Failure;
        }
    }
}
