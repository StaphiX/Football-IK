using RootMotion.FinalIK;
using UnityEngine;

public static class BipedIKUtil
{
    public static void SetSolverTargetTransform(this BipedIK bipedIK, BipedTarget target, Transform transform)
    {
        switch (target)
        {
            case BipedTarget.LeftFoot:
                bipedIK.solvers.leftFoot.target = transform;
                return;
            case BipedTarget.RightFoot:
                bipedIK.solvers.rightFoot.target = transform;
                return;
            case BipedTarget.LeftHand:
                bipedIK.solvers.leftHand.target = transform;
                return;
            case BipedTarget.RightHand:
                bipedIK.solvers.rightHand.target = transform;
                return;
            case BipedTarget.Pelvis:
                bipedIK.solvers.pelvis.target = transform;
                return;
            case BipedTarget.Spine:
                bipedIK.solvers.spine.target = transform;
                return;
            case BipedTarget.LookAt:
                bipedIK.solvers.lookAt.target = transform;
                return;
            default:
                Debug.LogAssertion("Invalid Target");
                return;
        }
    }

    public static void SetSolverPosition(this BipedIK bipedIK, BipedTarget target, Vector3 position, float weight = 1.0f)
    {
        switch (target)
        {
            case BipedTarget.LeftFoot:
                bipedIK.solvers.leftFoot.SetIKPosition(position);
                bipedIK.solvers.leftFoot.SetIKPositionWeight(weight);
                return;
            case BipedTarget.RightFoot:
                bipedIK.solvers.rightFoot.SetIKPosition(position);
                bipedIK.solvers.rightFoot.SetIKPositionWeight(weight);
                return;
            case BipedTarget.LeftHand:
                bipedIK.solvers.leftHand.SetIKPosition(position);
                bipedIK.solvers.leftHand.SetIKPositionWeight(weight);
                return;
            case BipedTarget.RightHand:
                bipedIK.solvers.rightHand.SetIKPosition(position);
                bipedIK.solvers.rightHand.SetIKPositionWeight(weight);
                return;
            case BipedTarget.Pelvis:
                bipedIK.solvers.pelvis.transform.position = position;
                bipedIK.solvers.pelvis.Update();
                return;
            case BipedTarget.Spine:
                bipedIK.solvers.spine.SetIKPosition(position);
                bipedIK.solvers.spine.SetIKPositionWeight(weight);
                return;
            case BipedTarget.LookAt:
                bipedIK.solvers.lookAt.SetIKPosition(position);
                bipedIK.solvers.lookAt.SetIKPositionWeight(weight);
                return;
            default:
                Debug.LogAssertion("Invalid Target");
                return;
        }
    }

    public static void SetSolverRotation(this BipedIK bipedIK, BipedTarget target, Quaternion rotation, float weight = 1.0f)
    {
        switch (target)
        {
            case BipedTarget.LeftFoot:
                bipedIK.solvers.leftFoot.SetIKRotation(rotation);
                bipedIK.solvers.leftFoot.SetIKRotationWeight(weight);
                return;
            case BipedTarget.RightFoot:
                bipedIK.solvers.rightFoot.SetIKRotation(rotation);
                bipedIK.solvers.rightFoot.SetIKRotationWeight(weight);
                return;
            case BipedTarget.LeftHand:
                bipedIK.solvers.leftHand.SetIKRotation(rotation);
                bipedIK.solvers.leftHand.SetIKRotationWeight(weight);
                return;
            case BipedTarget.RightHand:
                bipedIK.solvers.rightHand.SetIKRotation(rotation);
                bipedIK.solvers.rightHand.SetIKRotationWeight(weight);
                return;
            case BipedTarget.Pelvis:
                bipedIK.solvers.pelvis.rotation = rotation.eulerAngles;
                bipedIK.solvers.pelvis.positionWeight = weight;
                return;
            default:
                Debug.LogAssertion("Invalid Target");
                return;
        }
    }

    public static Vector3 GetSolverPosition(this BipedIK bipedIK, BipedTarget target)
    {
        switch (target)
        {
            case BipedTarget.LeftFoot:
                return bipedIK.solvers.leftFoot.GetIKPosition();
            case BipedTarget.RightFoot:
                return bipedIK.solvers.rightFoot.GetIKPosition();
            case BipedTarget.LeftHand:
                return bipedIK.solvers.leftHand.GetIKPosition();
            case BipedTarget.RightHand:
                return bipedIK.solvers.rightHand.GetIKPosition();
            case BipedTarget.Pelvis:
                return bipedIK.solvers.pelvis.transform.position;
            case BipedTarget.Spine:
                return bipedIK.solvers.spine.GetIKPosition();
            case BipedTarget.LookAt:
                return bipedIK.solvers.lookAt.GetIKPosition();
            default:
                Debug.LogAssertion("Invalid Target");
                return Vector3.zero;
        }
    }

    public static Quaternion GetSolverRotation(this BipedIK bipedIK, BipedTarget target)
    {
        switch (target)
        {
            case BipedTarget.LeftFoot:
                return bipedIK.solvers.leftFoot.GetIKRotation();
            case BipedTarget.RightFoot:
                return bipedIK.solvers.rightFoot.GetIKRotation();
            case BipedTarget.LeftHand:
                return bipedIK.solvers.leftHand.GetIKRotation();
            case BipedTarget.RightHand:
                return bipedIK.solvers.rightHand.GetIKRotation();
            case BipedTarget.Pelvis:
                return Quaternion.Euler(bipedIK.solvers.pelvis.rotation);
            case BipedTarget.Spine:
                return Quaternion.identity;
            case BipedTarget.LookAt:
                return Quaternion.identity;
            default:
                Debug.LogAssertion("Invalid Target");
                return Quaternion.identity;
        }
    }

    public static Vector3 GetRootPosition(this BipedIK bipedIK)
    {
        return bipedIK.solvers.pelvis.transform.position;
    }

    public static Transform GetRootTransform(this BipedIK bipedIK)
    {
        return bipedIK.solvers.pelvis.transform;
    }


    public static Quaternion GetRootRotation(this BipedIK bipedIK)
    {
        return bipedIK.solvers.pelvis.transform.rotation;
    }
}