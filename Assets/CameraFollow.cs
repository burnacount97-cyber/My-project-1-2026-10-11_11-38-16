using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 baseOffset = new Vector3(0, 3, -6);

    [Header("Control de Cámara (Flechas)")]
    public float orbitSpeed = 90f;
    public float pitchSpeed = 60f;
    public float minPitch = -60f;
    public float maxPitch = 60f;
    [Tooltip("Si está activo, la cámara gira con el dron (Q/E también giran la cámara)")]
    public bool followDroneYaw = true;

    [Header("Suavizado")]
    public float positionSmooth = 6f;
    public float rotationSmooth = 6f;

    [HideInInspector] public float orbitYaw = 0f;   // El DroneController lo usa para el movimiento relativo
    private float orbitPitch = 0f;

    void LateUpdate()
    {
        if (target == null) return;

        // Flechas controlan SOLO la cámara
        if (Input.GetKey(KeyCode.LeftArrow))  orbitYaw -= orbitSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.RightArrow)) orbitYaw += orbitSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.UpArrow))    orbitPitch -= pitchSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.DownArrow))  orbitPitch += pitchSpeed * Time.deltaTime;
        orbitPitch = Mathf.Clamp(orbitPitch, minPitch, maxPitch);

        // Yaw total = yaw del dron + órbita del usuario
        float totalYaw = followDroneYaw ? target.eulerAngles.y + orbitYaw : orbitYaw;
        Quaternion orbitRot = Quaternion.Euler(orbitPitch, totalYaw, 0);
        Vector3 desiredPosition = target.position + orbitRot * baseOffset;

        float kPos = 1f - Mathf.Exp(-positionSmooth * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, kPos);

        Vector3 lookDir = target.position - transform.position;
        if (lookDir.sqrMagnitude > 0.01f)
        {
            Quaternion desiredRotation = Quaternion.LookRotation(lookDir, Vector3.up);
            float kRot = 1f - Mathf.Exp(-rotationSmooth * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, kRot);
        }
    }
}