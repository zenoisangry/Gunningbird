using UnityEngine;

public class GSMainMenu : IGameState
{
    public void OnStateEnter()
    {
        UIManager.Instance.ShowUI(UIManager.UIType.MainMenu);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Time.timeScale = 1f;

        GameManager.Instance.isGameActive = false;
        GameManager.Instance.isGamePaused = false;
    }

    public void OnStateUpdate(){}

    public void OnStateExit(){}
}

public class GSOptions : IGameState
{
    private bool wasInGameplay = false;

    public void OnStateEnter()
    {
        wasInGameplay = GameManager.Instance.isGameActive;

        UIManager.Instance.ShowUI(UIManager.UIType.Options);

        if (wasInGameplay)
        {
            Time.timeScale = 0f;
            GameManager.Instance.isGamePaused = true;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OnStateUpdate(){}

    public void OnStateExit(){}
}

public class GSPause : IGameState
{
    public void OnStateEnter()
    {
        UIManager.Instance.ShowUI(UIManager.UIType.Pause);

        Time.timeScale = 0f;

        GameManager.Instance.isGamePaused = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        DisablePlayerInput();
    }

    public void OnStateUpdate()
    {
        if (Time.timeScale != 0f)
        {
            Time.timeScale = 0f;
        }
    }

    public void OnStateExit()
    {
        if (GameManager.Instance.isGameActive)
        {
            EnablePlayerInput();
        }
    }

    private void DisablePlayerInput()
    {
        if (GameManager.Instance.playerInstance == null)
        {
            return;
        }

        var playerInputComponent = GameManager.Instance.playerInstance.GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (playerInputComponent != null)
        {
            playerInputComponent.SwitchCurrentActionMap("UI");
        }

        GameObject playerObj = GameManager.Instance.playerInstance.gameObject;

        var movementScripts = new string[]
        {
            "PlayerMovement",
            "FirstPersonController",
            "PlayerController",
            "FPSController",
            "CharacterMovement",
            "PlayerLook",
            "MouseLook",
            "CameraController",
            "WeaponController",
            "PlayerShooting"
        };

        foreach (string scriptName in movementScripts)
        {
            var script = playerObj.GetComponent(scriptName) as MonoBehaviour;
            if (script != null)
            {
                script.enabled = false;
            }
        }

        var rb = playerObj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        var characterController = playerObj.GetComponent<CharacterController>();
        if (characterController != null)
        {
            characterController.enabled = false;
        }
    }

    private void EnablePlayerInput()
    {
        if (GameManager.Instance.playerInstance == null)
        {
            return;
        }

        var playerInputComponent = GameManager.Instance.playerInstance.GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (playerInputComponent != null)
        {
            playerInputComponent.SwitchCurrentActionMap("Player");
        }

        GameObject playerObj = GameManager.Instance.playerInstance.gameObject;

        var movementScripts = new string[]
        {
            "PlayerMovement",
            "FirstPersonController",
            "PlayerController",
            "FPSController",
            "CharacterMovement",
            "PlayerLook",
            "MouseLook",
            "CameraController",
            "WeaponController",
            "PlayerShooting"
        };

        foreach (string scriptName in movementScripts)
        {
            var script = playerObj.GetComponent(scriptName) as MonoBehaviour;
            if (script != null)
            {
                script.enabled = true;
            }
        }

        var rb = playerObj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
        }

        var characterController = playerObj.GetComponent<CharacterController>();
        if (characterController != null)
        {
            characterController.enabled = true;
        }
    }
}

public class GSGameplay : IGameState
{
    public void OnStateEnter()
    {
        UIManager.Instance.ShowUI(UIManager.UIType.Gameplay);

        GameManager.Instance.StartGame();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Time.timeScale = 1f;
        Debug.Log($"[GSGameplay] Time.timeScale set to {Time.timeScale}");

        EnsurePlayerInputEnabled();
    }

    public void OnStateUpdate()
    {
        if (!GameManager.Instance.isGamePaused && Time.timeScale != 1f)
        {
            Time.timeScale = 1f;
        }
    }

    public void OnStateExit()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void EnsurePlayerInputEnabled()
    {
        if (GameManager.Instance.playerInstance == null)
        {
            return;
        }

        var playerInputComponent = GameManager.Instance.playerInstance.GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (playerInputComponent != null)
        {
            playerInputComponent.ActivateInput();
            playerInputComponent.SwitchCurrentActionMap("Player");
        }

        GameObject playerObj = GameManager.Instance.playerInstance.gameObject;

        var movementScripts = new string[]
        {
            "PlayerMovement",
            "FirstPersonController",
            "PlayerController",
            "FPSController",
            "CharacterMovement",
            "PlayerLook",
            "MouseLook",
            "CameraController",
            "WeaponController",
            "PlayerShooting"
        };

        foreach (string scriptName in movementScripts)
        {
            var script = playerObj.GetComponent(scriptName) as MonoBehaviour;
            if (script != null && !script.enabled)
            {
                script.enabled = true;
            }
        }

        var rb = playerObj.GetComponent<Rigidbody>();
        if (rb != null && rb.isKinematic)
        {
            rb.isKinematic = false;
        }

        var characterController = playerObj.GetComponent<CharacterController>();
        if (characterController != null && !characterController.enabled)
        {
            characterController.enabled = true;
        }
    }
}

public class GSGameOver : IGameState
{
    public void OnStateEnter()
    {
        UIManager.Instance.ShowUI(UIManager.UIType.GameOver);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Time.timeScale = 1f;

        GameManager.Instance.isGameActive = false;
        GameManager.Instance.isGamePaused = false;

        if (GameManager.Instance.playerInstance != null)
        {
            var playerInputComponent = GameManager.Instance.playerInstance.GetComponent<UnityEngine.InputSystem.PlayerInput>();
            if (playerInputComponent != null)
            {
                playerInputComponent.SwitchCurrentActionMap("UI");
            }
        }
    }

    public void OnStateUpdate(){}

    public void OnStateExit(){}
}