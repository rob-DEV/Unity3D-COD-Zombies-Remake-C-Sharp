using UnityEngine;
using System.Collections;

public class WeaponSway : MonoBehaviour
{

    public float amount = 0.02f;
    public float maxAmount = 0.03f;
    public float smooth = 3;
    public float smoothRotation = 2;
    public float tiltAngle = 25;
    private Vector3 dif;

    // Use this for initialization
    void Start()
    {
        dif = transform.localPosition;
    }

    // Update is called once per frame
    void Update()
    {
        float factorX = -Input.GetAxis("Mouse X") * amount;
        float factorY = -Input.GetAxis("Mouse Y") * amount;

        if (factorX > maxAmount)
            factorX = maxAmount;

        if (factorX < -maxAmount)
            factorX = -maxAmount;

        if (factorY > maxAmount)
            factorY = maxAmount;

        if (factorY < -maxAmount)
            factorY = -maxAmount;

        Vector3 final = new Vector3(dif.x + factorX, dif.y + factorY, dif.z);
        transform.localPosition = Vector3.Lerp(transform.localPosition, final, Time.deltaTime * smooth);

        float tiltAroundZ = Input.GetAxis("Mouse X") * tiltAngle;
        float tiltAroundX = Input.GetAxis("Mouse Y") * tiltAngle;
        Quaternion target = Quaternion.Euler(tiltAroundX, 0, tiltAroundZ);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, target, Time.deltaTime * smoothRotation);
        MoveSwayController();
    }
    void MoveSwayController()
    {
        float factorX = -Input.GetAxis("Horizontal") * amount;
        float factorY = -Input.GetAxis("Vertical") * amount;

        if (factorX > maxAmount)
            factorX = maxAmount;

        if (factorX < -maxAmount)
            factorX = -maxAmount;

        if (factorY > maxAmount)
            factorY = maxAmount;

        if (factorY < -maxAmount)
            factorY = -maxAmount;

        float tiltAroundZ = Input.GetAxis("Horizontal") * tiltAngle;
        float tiltAroundX = Input.GetAxis("Vertical") * tiltAngle;
        Quaternion target = Quaternion.Euler(tiltAroundX, 0, tiltAroundZ);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, target, Time.deltaTime * smoothRotation);
    }
}
