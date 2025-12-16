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
        if (!playerCamera)
            playerCamera = GetComponentInChildren<Camera>();

        if (!fireTransform)
            Debug.LogError("[WeaponController] FireTransform missing");
    }

    public Transform GetFireTransform() => fireTransform;
    public Transform GetCameraTransform() => playerCamera.transform;
    public Animator GetAnimator() => animator;

    public void AddRecoil(Vector2 recoil)
    {
        accumulatedRecoil += recoil;
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
            if (!weaponController) return;

            currentRecoil += weaponController.ConsumeRecoil();

            currentRecoil = Vector2.Lerp(
                currentRecoil,
                Vector2.zero,
                recoilReturnSpeed * Time.deltaTime
            );

            transform.localRotation *= Quaternion.Euler(
                -currentRecoil.y,
                currentRecoil.x,
                0f
            );
        }
    }
}