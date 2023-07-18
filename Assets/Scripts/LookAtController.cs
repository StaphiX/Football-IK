using System.Collections;
using System.Collections.Generic;
using RootMotion.FinalIK;
using UnityEngine;

public class LookAtController
{
    public Vector3? lookAt;
    public PoseKeyframe currentPose = null;

    public void Update(BipedIK bipedIK)
    {
        if (lookAt == null)
            return;

        if (currentPose == null)
            currentPose = new PoseKeyframe();

        PoseUpdatePosition poseUpdate = (PoseUpdatePosition)currentPose.Get(PoseUpdateType.Position, BipedTarget.LookAt);

        if (poseUpdate != null)
            poseUpdate.Set(lookAt.Value, bipedIK);
        else
        {
            poseUpdate = new PoseUpdatePosition(BipedTarget.LookAt, lookAt.Value, bipedIK);
            currentPose.Add(poseUpdate);
        }

    }
}
