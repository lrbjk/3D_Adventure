using Astraparasitus;
using BehaviorDesigner.Runtime.Tasks;
using FirstGearGames.SmoothCameraShaker;
using UnityEngine;
public class EnemyAction : Action
{
    public EnemyBase enemy;
    public override void OnAwake()
    {
        enemy = GetComponent<EnemyBase>();
    }
}

public class Idle : EnemyAction
{
    public override void OnStart()
    {
        enemy.ChangeState(EnemyState.Idle);
    }
    public override TaskStatus OnUpdate()
    {
        if (enemy.GetDie() == true)
        {
            return TaskStatus.Failure;
        }
        if (enemy.aiDetect.isDetected)
        {
            return TaskStatus.Success;
        }
        if (enemy.GetBreakState())
        {
            return TaskStatus.Success;
        }
        if (enemy.GetLegBreakState() == true)
        {
            return TaskStatus.Failure;
        }
        if (enemy.aiDetect.GetCanAttack() == true)
        {
            return TaskStatus.Failure;
        }
        if(enemy.aiDetect.GetCanFarAttack() == true)
        {
            return TaskStatus.Failure;
        }
        if (enemy.GetHit() == true)
        {
            return TaskStatus.Success;
        }

        return TaskStatus.Running;
    }
}
public class Chase : EnemyAction
{
    public override void OnStart()
    {
        enemy.UpdateAgent(GamePlay.Instance.player.transform);
        enemy.ChangeState(EnemyState.Chase);
    }
    public override TaskStatus OnUpdate()
    {
        if (enemy.GetDie() == true)
        {
            return TaskStatus.Failure;
        }
        if (!enemy.aiDetect.isDetected)
        {
            return TaskStatus.Success;
        }
        if (enemy.GetBreakState())
        {
            return TaskStatus.Success;
        }
        if (enemy.GetLegBreakState() == true)
        {
            return TaskStatus.Failure;
        }
        if (enemy.aiDetect.GetCanAttack() == true)
        {
            return TaskStatus.Failure;
        }
        if (enemy.aiDetect.GetCanFarAttack() == true)
        {
            return TaskStatus.Failure;
        }
        if (enemy.GetHit() == true)
        {
            return TaskStatus.Success;
        }

        return TaskStatus.Running;
    }
}

public class Break : EnemyAction
{
    public override void OnStart()
    {
        enemy.ChangeState(EnemyState.Break);
    }
    public override TaskStatus OnUpdate()
    {
        if (!enemy.GetBreakState())
        {
            return TaskStatus.Failure;
        }
        if (enemy.GetDie() == true)
        {
            enemy.SetBreakState(false);
            return TaskStatus.Failure;
        }
        if (enemy.GetHit())
        {
            return TaskStatus.Failure;
        }
        return TaskStatus.Running;
    }
}

public class NoLegIdle : EnemyAction
{
    public override void OnStart()
    {
        enemy.ChangeState(EnemyState.NoLegIdle);
    }
    public override TaskStatus OnUpdate()
    {
        if (enemy.aiDetect.isDetected)
        {
            return TaskStatus.Success;
        }
        if (enemy.aiDetect.GetCanAttack() == true)
        {
            return TaskStatus.Failure;
        }
        if (enemy.GetHit() == true)
        {
            return TaskStatus.Success;
        }
        if (enemy.GetDie() == true)
        {
            return TaskStatus.Failure;
        }
        return TaskStatus.Running;
    }
}
public class NoLegChase : EnemyAction
{
    public override void OnStart()
    {
        enemy.UpdateAgent(GamePlay.Instance.player.transform);
        enemy.ChangeState(EnemyState.NoLegChase);
    }
    public override TaskStatus OnUpdate()
    {
        if (!enemy.aiDetect.isDetected)
        {
            return TaskStatus.Success;
        }
        if (enemy.aiDetect.GetCanAttack() == true)
        {
            return TaskStatus.Failure;
        }
        if (enemy.GetHit() == true)
        {
            return TaskStatus.Success;
        }
        if (enemy.GetDie() == true)
        {
            return TaskStatus.Failure;
        }
        return TaskStatus.Running;
    }
}
public class NormalAttack : EnemyAction
{
    public int attackIndex;
    public override void OnStart()
    {
        enemy.SetSkillIndex(attackIndex);
        enemy.enemyModel.SetSkillIndex(attackIndex);
        enemy.ChangeState(EnemyState.Attack);
    }
    public override TaskStatus OnUpdate()
    {
        if (enemy.GetSkillReady() == true)
        {
            return TaskStatus.Success;
        }
        if (enemy.GetHit() == true)
        {
            return TaskStatus.Success;
        }
        if (enemy.GetDie() == true)
        {
            return TaskStatus.Failure;
        }
        return TaskStatus.Running;
    }
}
public class BreakHit : EnemyAction
{
    public override void OnStart()
    {
        enemy.ChangeState(EnemyState.BreakHit);
    }
    public override TaskStatus OnUpdate()
    {
        if (enemy.GetDie() == true)
        {
            return TaskStatus.Failure;
        }
        if (enemy.GetHit() == false)
        {
            return TaskStatus.Success;
        }
        return TaskStatus.Running;
    }
}
public class Die : EnemyAction
{
    public override void OnStart()
    {
        enemy.ChangeState(EnemyState.Die);
    }
    public override TaskStatus OnUpdate()
    {
        if (enemy.GetDie() == false)
        {
            return TaskStatus.Success;
        }
        return TaskStatus.Running;
    }
}
// ����״̬
public class Recovery : EnemyAction
{
public override void OnStart()
{
    enemy.ChangeState(EnemyState.Recovery);
}
public override TaskStatus OnUpdate()
{
    if (enemy.GetEndurance() > 0)
    {
        return TaskStatus.Success;
    }
    return TaskStatus.Running;
}
}
public class Idle_Pro : EnemyAction
{
public override void OnStart()
{
    enemy.ChangeState(EnemyState.Idle);
}
public override TaskStatus OnUpdate()
{
    if (enemy.GetDie() == true)
    {
        return TaskStatus.Failure;
    }
    if (enemy.aiDetect.isDetected)
    {
        return TaskStatus.Success;
    }
    if (enemy.GetBreakState())
    {
        return TaskStatus.Success;
    }
    if (enemy.GetLegBreakState() == true)
    {
        return TaskStatus.Failure;
    }
    if (enemy.aiDetect.GetCanAttack() == true)
    {
        return TaskStatus.Failure;
    }
    if (enemy.aiDetect.GetCanFarAttack() == true)
    {
        return TaskStatus.Failure;
    }
    if(enemy.GetEndurance() <= 0)
    {
        return TaskStatus.Failure;
    }
    if (enemy.GetHit() == true)
    {
        return TaskStatus.Success;
    }

    return TaskStatus.Running;
}
}
public class Chase_Pro : EnemyAction
{
public override void OnStart()
{
        enemy.UpdateAgent(GamePlay.Instance.player.transform);
    enemy.ChangeState(EnemyState.Chase);
}
public override TaskStatus OnUpdate()
{
    if (enemy.GetDie() == true)
    {
        return TaskStatus.Failure;
    }
    if (!enemy.aiDetect.isDetected)
    {
        return TaskStatus.Success;
    }
    if (enemy.GetBreakState())
    {
        return TaskStatus.Success;
    }
    if (enemy.GetLegBreakState() == true)
    {
        return TaskStatus.Failure;
    }
    if (enemy.aiDetect.GetCanAttack() == true)
    {
        return TaskStatus.Failure;
    }
    if (enemy.aiDetect.GetCanFarAttack() == true)
    {
        return TaskStatus.Failure;
    }
    if (enemy.GetEndurance() <= 0)
    {
        return TaskStatus.Failure;
    }
    if (enemy.GetHit() == true)
    {
        return TaskStatus.Success;
    }

    return TaskStatus.Running;
}
}
public class NormalAttack_Pro : EnemyAction
{
public int attackIndex;
public bool isTC = false;
public override void OnStart()
{
    enemy.SetSkillIndex(attackIndex);
    enemy.enemyModel.SetSkillIndex(attackIndex);
    enemy.ChangeState(EnemyState.Attack);
}
public override TaskStatus OnUpdate()
{
    if (enemy.GetSkillReady() == true)
    {
        return TaskStatus.Success;
    }
    if (enemy.GetHit() == true)
    {
        return TaskStatus.Success;
    }
    if (enemy.GetDie() == true)
    {
        return TaskStatus.Failure;
    }
    if (enemy.GetEndurance() <= 0)
    {
        return TaskStatus.Failure;
    }
    if (!isTC && !enemy.aiDetect.useDistanceCheck)
    {
        return TaskStatus.Success;
    }
    return TaskStatus.Running;
}
}
public class SpAttack : EnemyAction
{
    public int attackIndex;
    public bool isTC = false;
    public override void OnStart()
    {
        enemy.SetSkillIndex(attackIndex);
        enemy.enemyModel.SetSkillIndex(attackIndex);
        enemy.ChangeState(EnemyState.SpAttack);
    }
    public override TaskStatus OnUpdate()
    {
        if (enemy.GetSkillReady() == true)
        {
            return TaskStatus.Success;
        }
        if (enemy.GetHit() == true)
        {
            return TaskStatus.Success;
        }
        if (enemy.GetDie() == true)
        {
            return TaskStatus.Failure;
        }
        if (enemy.GetEndurance() <= 0)
        {
            return TaskStatus.Failure;
        }
        if (!isTC && !enemy.aiDetect.useDistanceCheck)
        {
            return TaskStatus.Success;
        }
        return TaskStatus.Running;
    }
}
public class ChoseToTC : EnemyAction
{
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
        return TaskStatus.Success;
    }

}
public class Idle_Alptha : EnemyAction
{
    public EnemyAlptha enemyAlptha;
    public bool isAngery = false;
    public override void OnAwake()
    {
        base.OnAwake();
        enemyAlptha = GetComponent<EnemyAlptha>();
    }
    public override void OnStart()
    {
        if (isAngery)
        {
            enemy.ChangeState(EnemyState.Angery);
        }
        else
        {
            if (enemyAlptha.isfirstIdle)
            {
                enemy.ChangeState(EnemyState.Other);
            }
            else
            {
                enemy.ChangeState(EnemyState.Idle);
            }
        }
        enemyAlptha.isfirstIdle = false;
    }
    public override TaskStatus OnUpdate()
    {
        if (enemy.GetDie() == true)
        {
            return TaskStatus.Failure;
        }
        if (enemy.aiDetect.isDetected)
        {
            return TaskStatus.Success;
        }
        if (enemy.GetBreakState())
        {
            return TaskStatus.Success;
        }
        if (enemy.GetLegBreakState() == true)
        {
            return TaskStatus.Failure;
        }
        if (enemy.aiDetect.GetCanAttack() == true)
        {
            return TaskStatus.Failure;
        }
        if (enemy.aiDetect.GetCanFarAttack() == true)
        {
            return TaskStatus.Failure;
        }
        if (enemy.GetEndurance() <= 0)
        {
            return TaskStatus.Failure;
        }
        if (enemy.GetHit() == true)
        {
            return TaskStatus.Success;
        }

        return TaskStatus.Running;
    }
}
