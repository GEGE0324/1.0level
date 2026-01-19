
using UnityEngine;
using Opsive.Shared.Events;

namespace Art.Scripts
{
    /// <summary>
    /// 当物体受到攻击（或伤害）时，激活或禁用场景中的其他物体。
    /// 适用于触发机关、破坏障碍物开启路径等场景。
    /// </summary>
    public class HitTriggerActivator : MonoBehaviour
    {
        [Header("激活/禁用设置")]
        [Tooltip("触发时需要【显示/激活】的物体列表")]
        [SerializeField] protected GameObject[] m_ObjectsToActivate;
        
        [Tooltip("触发时需要【隐藏/关闭】的物体列表")]
        [SerializeField] protected GameObject[] m_ObjectsToDeactivate;

        [Header("逻辑设置")]
        [Tooltip("是否只触发一次？")]
        [SerializeField] protected bool m_TriggerOnce = true;
        
        [Tooltip("触发后是否立即隐藏自己？")]
        [SerializeField] protected bool m_DeactivateSelfOnTrigger = false;

        [Header("解锁无敌设置")]
        [Tooltip("触发时需要【取消无敌状态】的 Health 组件列表")]
        [SerializeField] protected Opsive.UltimateCharacterController.Traits.Health[] m_InvulnerableHealths;

        private bool m_HasTriggered = false;

        private void Awake()
        {
            // 注册 UCC 的伤害事件。当任何武器或射弹击中此物体（且此物体有 Health 组件或能接收伤害）时触发。
            // 修正后的签名：float (伤害量), Vector3 (击中位置), Vector3 (推力), GameObject (攻击者), Collider (击中部位)
            EventHandler.RegisterEvent<float, Vector3, Vector3, GameObject, Collider>(gameObject, "OnHealthDamage", OnDamage);
        }

        /// <summary>
        /// 当接收到伤害时调用的回调。
        /// </summary>
        private void OnDamage(float amount, Vector3 position, Vector3 force, GameObject attacker, Collider hitCollider)
        {
            if (m_TriggerOnce && m_HasTriggered) return;

            ExecuteTrigger();
        }

        /// <summary>
        /// 执行物体的显示和隐藏逻辑。
        /// </summary>
        [ContextMenu("Debug Trigger")]
        public void ExecuteTrigger()
        {
            if (m_TriggerOnce && m_HasTriggered) return;
            
            m_HasTriggered = true;
            Debug.Log($"[HitTriggerActivator] {gameObject.name} 被击中，正在执行触发逻辑。");

            // 激活列表
            if (m_ObjectsToActivate != null)
            {
                foreach (var obj in m_ObjectsToActivate)
                {
                    if (obj != null) obj.SetActive(true);
                }
            }

            // 禁用列表
            if (m_ObjectsToDeactivate != null)
            {
                foreach (var obj in m_ObjectsToDeactivate)
                {
                    if (obj != null) obj.SetActive(false);
                }
            }

            // 解锁无敌状态
            if (m_InvulnerableHealths != null)
            {
                foreach (var health in m_InvulnerableHealths)
                {
                    if (health != null)
                    {
                        Debug.Log($"[HitTriggerActivator] 已取消 {health.gameObject.name} 的无敌状态。");
                        health.Invincible = false;
                    }
                }
            }

            // 如果需要，禁用掉自己
            if (m_DeactivateSelfOnTrigger)
            {
                gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            // 记得注销事件，防止内存泄漏。
            EventHandler.UnregisterEvent<float, Vector3, Vector3, GameObject, Collider>(gameObject, "OnHealthDamage", OnDamage);
        }
    }
}
