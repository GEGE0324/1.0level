using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Opsive.UltimateCharacterController.Character;
using Opsive.UltimateCharacterController.Character.Abilities.Items;

[TaskCategory("UCC Custom")]
public class UCCMeleeAttack : Action
{
    [UnityEngine.Tooltip("攻击的目标对象（通常是玩家变量）")]
    public SharedGameObject target;

    private UltimateCharacterLocomotion locomotion;
    private Use useAbility;

    public override void OnAwake()
    {
        locomotion = GetComponent<UltimateCharacterLocomotion>();
        // 获取 UCC 的 Use 能力
        var useAbilities = locomotion.GetAbilities<Use>();
        if (useAbilities != null && useAbilities.Length > 0)
        {
            useAbility = useAbilities[0];
        }
    }

    public override TaskStatus OnUpdate()
    {

        if (useAbility == null) return TaskStatus.Failure;

        // 1. 物理朝向修正：解决 "Look rotation viewing vector is zero"
        if (target != null && target.Value != null)
        {
            Vector3 direction = target.Value.transform.position - transform.position;
            direction.y = 0; // 锁定水平方向，防止歪斜

            // 只有距离足够才旋转，防止重叠时报错
            if (direction.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        // 2. 触发攻击逻辑
        if (!useAbility.IsActive)
        {
            locomotion.TryStartAbility(useAbility);
        }

        // 返回成功，让行为树继续执行后面的 Wait 节点
        return TaskStatus.Success;
    }
}