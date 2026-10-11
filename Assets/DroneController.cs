using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class DroneController : MonoBehaviour
{
    [Header("Referencias")]
    public Transform visualBody;
    public Transform cam;   // Arrastra aquí la Main Camera del XR Origin

    [Header("Fuerzas")]
    public float liftForce = 45f;
    public float moveForce = 25f;
    public float hoverForce = 15f;
    public float yawSpeed = 90f;
    public float maxPitchAngle = 12f;
    public float maxRollAngle = 5f;

    [Header("Suavizado")]
    public float inputSmoothing = 7f;
    public float tiltSmoothing = 5f;

    [Header("Límites")]
    public float maxHorizontalSpeed = 12f;
    public float maxVerticalSpeed = 6f;

    [Header("VR")]
    public InputActionAsset inputActions;
    public string leftActionName = "XRI Left/Thumbstick";
    public string rightActionName = "XRI Right/Thumbstick";

    private Rigidbody rb;
    private InputAction leftAction, rightAction;
    private float sThrottle, sYaw, sForward, sStrafe;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0, -0.3f, 0);
        rb.linearDamping = 2f;
        rb.angularDamping = 12f;

        if (visualBody == null && transform.childCount > 0)
            visualBody = transform.GetChild(0);

        if (inputActions != null)
        {
            leftAction = inputActions.FindAction(leftActionName);
            rightAction = inputActions.FindAction(rightActionName);
        }
    }

    void Update()
    {
        float tThrottle = 0f, tYaw = 0f, tForward = 0f, tStrafe = 0f;

        // TECLADO
        //if (Input.GetKey(KeyCode.Space))       tThrottle += 1f;
        //if (Input.GetKey(KeyCode.LeftControl)) tThrottle -= 1f;
        //if (Input.GetKey(KeyCode.W)) tForward += 1f;
        //if (Input.GetKey(KeyCode.S)) tForward -= 1f;
        //if (Input.GetKey(KeyCode.A)) tStrafe  -= 1f;
        //if (Input.GetKey(KeyCode.D)) tStrafe  += 1f;
        //if (Input.GetKey(KeyCode.Q)) tYaw -= 1f;
        //if (Input.GetKey(KeyCode.E)) tYaw += 1f;

        // VR
        if (leftAction != null && rightAction != null)
        {
            Vector2 l = leftAction.ReadValue<Vector2>();
            Vector2 r = rightAction.ReadValue<Vector2>();
            tThrottle += l.y;
            tYaw      += l.x;
            tForward  += r.y;
            tStrafe   += r.x;
        }

        float k = 1f - Mathf.Exp(-inputSmoothing * Time.deltaTime);
        sThrottle = Mathf.Lerp(sThrottle, Mathf.Clamp(tThrottle, -1f, 1f), k);
        sYaw      = Mathf.Lerp(sYaw,      Mathf.Clamp(tYaw,      -1f, 1f), k);
        sForward  = Mathf.Lerp(sForward,  Mathf.Clamp(tForward,  -1f, 1f), k);
        sStrafe   = Mathf.Lerp(sStrafe,   Mathf.Clamp(tStrafe,   -1f, 1f), k);
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        // --- Movimiento RELATIVO A LA CÁMARA ---
        Vector3 camForward, camRight;
        if (cam != null)
        {
            // Dirección "hacia adelante" de la cámara, proyectada al plano
            camForward = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
            if (camForward.sqrMagnitude < 0.01f) camForward = Vector3.ProjectOnPlane(cam.up, Vector3.up).normalized;
            camRight = Vector3.Cross(Vector3.up, camForward);
        }
        else
        {
            // Fallback: relativo al dron
            camForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            camRight   = Vector3.ProjectOnPlane(transform.right,   Vector3.up).normalized;
        }

        Vector3 moveDir = camForward * sForward + camRight * sStrafe;
        rb.AddForce(moveDir * moveForce, ForceMode.Force);

        // Fuerza vertical
        rb.AddForce(Vector3.up * (hoverForce + sThrottle * liftForce), ForceMode.Force);

        // Límites de velocidad
        Vector3 v = rb.linearVelocity;
        Vector3 flatVel = new Vector3(v.x, 0, v.z);
        if (flatVel.magnitude > maxHorizontalSpeed)
        {
            Vector3 capped = flatVel.normalized * maxHorizontalSpeed;
            rb.linearVelocity = new Vector3(capped.x, v.y, capped.z);
        }
        if (Mathf.Abs(rb.linearVelocity.y) > maxVerticalSpeed)
        {
            rb.linearVelocity = new Vector3(
                rb.linearVelocity.x,
                Mathf.Sign(rb.linearVelocity.y) * maxVerticalSpeed,
                rb.linearVelocity.z);
        }

        // Yaw del dron (Q/E)
        if (Mathf.Abs(sYaw) > 0.01f)
        {
            Quaternion deltaYaw = Quaternion.Euler(0, sYaw * yawSpeed * dt, 0);
            rb.MoveRotation(rb.rotation * deltaYaw);
        }
    }

    void LateUpdate()
    {
        if (visualBody == null) return;
        float targetPitch = sForward * maxPitchAngle;
        float targetRoll  = -sStrafe * maxRollAngle;
        Quaternion targetTilt = Quaternion.Euler(targetPitch, 0, targetRoll);
        float k = 1f - Mathf.Exp(-tiltSmoothing * Time.deltaTime);
        visualBody.localRotation = Quaternion.Slerp(visualBody.localRotation, targetTilt, k);
    }
}