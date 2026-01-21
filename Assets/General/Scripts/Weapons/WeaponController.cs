using UnityEngine;

public class WeaponController : MonoBehaviour, IWeaponOwner
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform fireTransform;
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource audioSource;

    private Vector2 accumulatedRecoil;

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        if (fireTransform == null)
            Debug.LogError("[WeaponController] FireTransform missing! Please assign a fire transform.", this);

        if (animator == null)
            animator = GetComponent<Animator>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    public Transform GetFireTransform() => fireTransform;
    public Transform GetCameraTransform() => playerCamera != null ? playerCamera.transform : null;
    public Animator GetAnimator() => animator;

    public void AddRecoil(Vector2 recoil)
    {
        accumulatedRecoil += recoil;
        // Clamp recoil to prevent excessive values
        accumulatedRecoil.x = Mathf.Clamp(accumulatedRecoil.x, -90f, 90f);
        accumulatedRecoil.y = Mathf.Clamp(accumulatedRecoil.y, -90f, 90f);
    }

    public Vector2 ConsumeRecoil()
    {
        Vector2 r = accumulatedRecoil;
        accumulatedRecoil = Vector2.zero;
        return r;
    }

    public void PlaySound(AudioClip clip)
    {
        if (clip && audioSource)
            audioSource.PlayOneShot(clip);
    }

    public class CameraRecoil : MonoBehaviour
    {
        [SerializeField] private WeaponController weaponController;
        [SerializeField] private float recoilReturnSpeed = 20f;

        private Vector2 currentRecoil;

        private void LateUpdate()
        {
            if (weaponController == null) return;

            currentRecoil += weaponController.ConsumeRecoil();

            currentRecoil = Vector2.Lerp(
                currentRecoil,
                Vector2.zero,
                recoilReturnSpeed * Time.deltaTime
            );

            // Apply recoil rotation
            float pitch = -currentRecoil.y;
            float yaw = currentRecoil.x;
            
            // Clamp pitch to prevent over-rotation
            pitch = Mathf.Clamp(pitch, -90f, 90f);
            
            transform.localRotation *= Quaternion.Euler(pitch, yaw, 0f);
        }
    }
}