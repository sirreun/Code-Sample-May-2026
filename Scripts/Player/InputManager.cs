using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : NetworkBehaviour
{
    private PlayerInput playerInput;
    private PlayerInput.OnFootActions onFoot;
    private PlayerInput.UIActions ui;

    private PlayerManager playerManager;
    private InventoryManager inventoryManager;
    private PlayerUI playerUI;
    private Player player;
    private InputMap currentMap = InputMap.OnFoot;

    private enum InputMap
    {
        OnFoot,
        UI
    }

    #region /// INITIALIZATION ///
    void Awake()
    {
        playerInput = new PlayerInput();

        playerManager = GetComponent<PlayerManager>();
        inventoryManager = GetComponent<InventoryManager>();
        playerUI = GetComponent<PlayerUI>();
        player = GetComponent<Player>();

        playerUI.ShowPauseMenu(false);
    }

    private void InitPlayerOnFootActions()
    {
        onFoot = playerInput.OnFoot;

        // Callback context to call Jump function
        onFoot.Jump.performed += ctx => playerManager.Jump();

        // Callback context to call Crouch function
        onFoot.Crouch.performed += ctx => playerManager.Crouch();

        // Callback context to call Sprint function
        onFoot.Sprint.performed += ctx => playerManager.Sprint();

        // Callback context to call DropItem function
        onFoot.DropItem.performed += ctx => playerManager.DropItem();

        // Callback context to navigate inventory
        onFoot.InventoryItem1.performed += ctx => inventoryManager.ChangeCurrentInventorySlotWithNumber(1);
        onFoot.InventoryItem2.performed += ctx => inventoryManager.ChangeCurrentInventorySlotWithNumber(2);
        onFoot.InventoryItem3.performed += ctx => inventoryManager.ChangeCurrentInventorySlotWithNumber(3);
        onFoot.InventoryItem4.performed += ctx => inventoryManager.ChangeCurrentInventorySlotWithNumber(4);
        onFoot.InventoryItem5.performed += ctx => inventoryManager.ChangeCurrentInventorySlotWithNumber(5);

        // Callback context to interact with a held item
        onFoot.ItemInteract.performed += ctx => inventoryManager.ItemInteract();

        // Callback context to open pause menu
        onFoot.PauseMenu.performed += ctx => OpenPauseMenu();

        onFoot.Enable();
    }

    private void InitPlayerUIActions()
    {
        ui = playerInput.UI;

        ui.Cancel.performed += ctx => ClosePauseMenu();

        ui.Disable();
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            playerInput.Enable(); // NOTE: input action MUST be enabled before enabling/disabling action maps, otherwise it will overwrite map disables
            InitPlayerOnFootActions();
            InitPlayerUIActions();
        }
        else
        {
            // Disable other players' input on your local game
            playerInput.Disable();
            // Enable(false);

            // Disable Camera and UI for other players
            playerUI._Camera.tag = "OtherCamera";
            transform.GetChild(0).gameObject.SetActive(false);
            transform.GetChild(3).gameObject.SetActive(false);
        }
    }
    #endregion

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;

        inventoryManager.ClearInventory();

        PlayerDatabase.instance.TryRemovePlayerFromDatabaseServerRpc(player.ID); // Must happen last
    }
    
    void OnBeforeApplicationQuit()
    {
        if (!IsOwner) return;
        
        //TODO: call server rpc to remove from playerdatabase
    }

    private void OnApplicationFocus(bool focus)
    {
        if (!IsOwner) return;

        Enable(focus);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!IsSpawned) return;
        if (!IsOwner) return;

        // Tell the player manager to move using the value from the movement input action.
        playerManager.ProcessMove(onFoot.Movement.ReadValue<Vector2>());

        // Tell the player manager to navigate inventory UI from the navigateUI input action
        playerManager.ProcessScroll(onFoot.NavigateInventory.ReadValue<Vector2>());

    }

    void LateUpdate()
    {
        if (!IsSpawned) return;
        if (!IsOwner) return;
        // Tell the player manager to look using the value from the look input action.
        playerManager.ProcessLook(onFoot.Look.ReadValue<Vector2>());
    }

    #region /// ENABLE and DISABLE ///
    private void OnEnable()
    {
        if (!IsOwner) return;

        onFoot.Enable();
    }
    private void OnDisable()
    {
        if (!IsOwner) return;

        onFoot.Disable();
    }

    private void Enable(bool enable)
    {
        if (!IsOwner) return;

        if (enable)
        {
            switch (currentMap)
            {
                case InputMap.OnFoot:
                    onFoot.Enable();
                    break;
                case InputMap.UI:
                    ui.Enable();
                    Cursor.visible = true;
                    break;
            }
        }
        else
        {
            onFoot.Disable();
            ui.Disable();
        }
    }
    #endregion

    /// Returns a bool representing if Interact has been triggered.
    public bool InteractTriggered()
    {
        if (!IsOwner) return false;

        return playerInput.OnFoot.Interact.triggered;
    }

    public bool AttackTriggered()
    {
        if (!IsOwner) return false;
        
        if (inventoryManager.HoldingItem())
        {
            // Already checked if weapon in playerinteract
            inventoryManager.GetCurrentItemInfo().ItemAttackAnimations();
        }
        return playerInput.OnFoot.Attack.triggered;
    }

    private IEnumerator WaitForFrame()
    {
        yield return null;
    }

    public void OpenPauseMenu()
    {
        StartCoroutine(OpenPauseMenuIEnumerator());
    }

    private IEnumerator OpenPauseMenuIEnumerator()
    {
        onFoot.Disable();
        playerUI.ShowPauseMenu(true);
        //Debug.Log("OnFoot: esc pressed");
        yield return StartCoroutine(WaitForFrame());
        //Debug.Log("opening pause menu");
        ui.Enable();
        currentMap = InputMap.UI;
    }

    public void ClosePauseMenu()
    {
        StartCoroutine(ClosePauseMenuIEnumerator());
    }

    private IEnumerator ClosePauseMenuIEnumerator()
    {
        ui.Disable();
        playerUI.ShowPauseMenu(false);
        //Debug.Log("UI: esc pressed");
        yield return StartCoroutine(WaitForFrame());
        //Debug.Log("closing pause menu");
        onFoot.Enable();
        currentMap = InputMap.OnFoot;
    }
}
