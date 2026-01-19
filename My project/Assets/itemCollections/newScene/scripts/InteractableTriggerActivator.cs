using UnityEngine;
using Opsive.UltimateCharacterController.Traits;
using Opsive.UltimateCharacterController.Character.Abilities;

namespace Art.Scripts
{
    /// <summary>
    /// 当玩家与该物体交互时，激活或禁用场景中的其他物体。
    /// 必须配合 UCC 的 Interactable 组件使用。
    /// </summary>
    public class InteractableTriggerActivator : MonoBehaviour, IInteractableTarget
    {
        [Header("激活/禁用设置")]
        [Tooltip("交互时需要【显示/激活】的物体列表")]
        [SerializeField] protected GameObject[] m_ObjectsToActivate;
        
        [Tooltip("交互时需要【隐藏/关闭】的物体列表")]
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

        /// <summary>
        /// UCC 接口：判断当前是否可以交互。
        /// </summary>
        public bool CanInteract(GameObject character, Interact interactAbility)
        {
            if (m_TriggerOnce && m_HasTriggered) return false;
            return true;
        }

        /// <summary>
        /// UCC 接口：执行交互逻辑。
        /// </summary>
        public void Interact(GameObject character, Interact interactAbility)
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
            Debug.Log($"[InteractableTriggerActivator] {gameObject.name} 被交互，正在执行触发逻辑。");

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
                        Debug.Log($"[InteractableTriggerActivator] 已取消 {health.gameObject.name} 的无敌状态。");
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
    }
}
