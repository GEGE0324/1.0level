using BehaviorDesigner.Runtime.Tasks;
using Opsive.UltimateCharacterController.Character;
using Opsive.UltimateCharacterController.Character.Abilities.Items;

[TaskCategory("UCC Custom")]
[TaskDescription("停止使用武器（用于放箭或停止近战攻击）")]
public class UCCStopUse : Action
{
    private UltimateCharacterLocomotion locomotion;
    private Use useAbility;

    public override void OnAwake()
    {
        // 获取底层的位移控制器
        locomotion = GetComponent<UltimateCharacterLocomotion>();
        // 获取使用道具的能力
        useAbility = locomotion.GetAbility<Use>();
    }

    public override TaskStatus OnUpdate()
    {
        if (locomotion == null || useAbility == null) return TaskStatus.Failure;

        // 如果 Use 能力正在运行，则停止它
        // 在弓箭逻辑中，这一步会触发“放箭”动画和箭矢生成
        if (useAbility.IsActive)
        {
            locomotion.TryStopAbility(useAbility);
        }

        return TaskStatus.Success;
    }
}