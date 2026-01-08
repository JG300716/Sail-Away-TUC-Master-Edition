using UnityEngine;

public static class SailAeroCalculator
{
    public static Sail.SailForceResult Calculate(
        Transform sailTransform,
        Rigidbody yachtRb,
        UnifiedWindManager windSystem,
        Vector3 windDirection,
        float windSpeed,
        float sailArea,
        float sailLength,
        float sailWidth,
        float aspectRatio,
        float maxCL,
        float maxCD,
        float oswaldEfficiency,
        float forceMultiplier)
    {
        // === SKOPIOWANE 1:1 z Sail.CalculateWindForceWithTorque ===

        Sail.SailForceResult result = new Sail.SailForceResult();

        windDirection.y = 0;
        if (windDirection.sqrMagnitude < 0.001f)
        {
            result.force = Vector3.zero;
            result.inDeadZone = true;
            return result;
        }
        windDirection.Normalize();

        Vector3 sailChord = sailTransform.forward;
        sailChord.y = 0;
        sailChord.Normalize();

        float angleOfAttack = Vector3.SignedAngle(windDirection, sailChord, Vector3.up);
        result.angleOfAttack = angleOfAttack;

        Vector3 yachtForward = yachtRb.transform.forward;
        yachtForward.y = 0;
        yachtForward.Normalize();

        float yachtAngleToWind = Vector3.SignedAngle(yachtForward, windDirection, Vector3.up);

        float deadZoneFactor = windSystem.GetEfficiencyMultiplier(yachtAngleToWind);
        UnifiedWindManager.PointOfSail pos = windSystem.GetPointOfSailForAngle(yachtAngleToWind);
        UnifiedWindManager.PointOfSail dead =
            windSystem.pointsOfSail[windSystem.pointsOfSail.Length - 1];

        result.inDeadZone = pos.name == dead.name;

        float area = sailArea > 0 ? sailArea : sailLength * sailWidth;
        float AR = aspectRatio > 0 ? aspectRatio : (sailLength * sailLength) / area;

        float airDensity = 1.225f;
        float q = 0.5f * airDensity * windSpeed * windSpeed;

        if (deadZoneFactor <= 0f)
        {
            result.force = Vector3.zero;
            return result;
        }

        float absAoA = Mathf.Abs(angleOfAttack);
        float CL;

        if (absAoA < 15f)
            CL = (maxCL / 15f) * absAoA;
        else if (absAoA < 30f)
            CL = Mathf.Lerp(maxCL, maxCL * 0.53f, (absAoA - 15f) / 15f);
        else
            CL = Mathf.Lerp(maxCL * 0.53f, maxCL * 0.067f, (absAoA - 30f) / 60f);

        float inducedDrag = (CL * CL) / (Mathf.PI * oswaldEfficiency * AR);
        float parasiticDrag = Mathf.Lerp(0.05f, maxCD, absAoA / 90f);
        float CD = inducedDrag + parasiticDrag;

        float lift = CL * q * area;
        float drag = CD * q * area;

        Vector3 liftDir = Vector3.Cross(windDirection, Vector3.up);
        if (angleOfAttack < 0) liftDir = -liftDir;

        Vector3 force =
            (liftDir * lift + windDirection * drag) *
            deadZoneFactor *
            forceMultiplier;

        result.force = force;
        result.torqueMultiplier = deadZoneFactor;
        return result;
    }
}
