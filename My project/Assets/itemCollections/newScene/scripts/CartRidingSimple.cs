using UnityEngine;

public class CartRidingWithCamera : MonoBehaviour
{
    [Header("配置")]
    public Transform playerAnchor;      // 玩家在车上的位置
    public float interactionRange = 4f; // 空格交互距离

    private GameObject playerObj;
    private Camera playerCamera;
    private CharacterController playerController;
    private bool isRiding = false;

    void Update()
    {
        if (playerObj == null)
        {
            playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerController = playerObj.GetComponent<CharacterController>();
                playerCamera = playerObj.GetComponentInChildren<Camera>(); // 自动找玩家头顶的相机
            }
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (!isRiding)
            {
                if (Vector3.Distance(playerObj.transform.position, transform.position) < interactionRange)
                    GetOnCart();
            }
            else
            {
                GetOffCart();
            }
        }
    }

    // 使用 LateUpdate 确保在小车移动完之后再强锁玩家和相机
    void LateUpdate()
    {
        if (isRiding && playerObj != null)
        {
            // 1. 强锁玩家位置（不锁旋转，允许前后左右看）
            playerObj.transform.position = playerAnchor.position;

            // 2. 强锁相机位置（防止相机因为平滑算法掉队）
            // 如果你发现相机还是抖，就把下面这行取消注释
            // if(playerCamera != null) playerCamera.transform.position = playerAnchor.position + new Vector3(0, 1.6f, 0); // 1.6是身高
        }
    }

    void GetOnCart()
    {
        isRiding = true;
        if (playerController != null) playerController.enabled = false;

        // 核心：把玩家设为小车的子物体。这样玩家的局部坐标系就跟着车走了
        playerObj.transform.SetParent(this.transform);

        // 如果你使用了 Cinemachine，需要在上车瞬间强制清除它的缓存
        // UnityEngine.Rendering.Universal.CinemachineBrain (如果你用了这个)
        // 简单暴力的方法：如果相机有平滑脚本，先关掉它
    }

    void GetOffCart()
    {
        isRiding = false;
        playerObj.transform.SetParent(null);
        if (playerController != null) playerController.enabled = true;

        // 下车时往前弹一点，防止卡在车里
        playerObj.transform.position += playerObj.transform.forward * 2f;
    }
}