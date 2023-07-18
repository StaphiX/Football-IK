
using RootMotion.FinalIK;
using System.Collections.Generic;

public class PoseKeyframe : ISerialize
{
    public List<PoseUpdate> poseUpdates = new List<PoseUpdate>();

    public void Clear(PoseUpdate update)
    {
        poseUpdates.Add(update);
    }

    public void Add(PoseUpdate update)
    {
        poseUpdates.Add(update);
    }

    public PoseUpdate Get(PoseUpdateType type, BipedTarget target)
    {
        for (int updateIndex = 0; updateIndex < poseUpdates.Count; ++updateIndex)
        {
            if (poseUpdates[updateIndex].target == target &&
                poseUpdates[updateIndex].Type() == type)

                return poseUpdates[updateIndex];
        }

        return null;
    }

    public void Flip(BipedIK bipedIk)
    {
        for (int updateIndex = 0; updateIndex < poseUpdates.Count; ++updateIndex)
        {
            PoseUpdate update = poseUpdates[updateIndex];
            update.Flip(bipedIk);
        }
    }

    public void Apply(BipedIK bipedIK, float frameTime = 1.0f, bool firstFrame = false, PoseAnimationMode animMode = PoseAnimationMode.None)
    {
        for (int updateIndex = 0; updateIndex < poseUpdates.Count; ++updateIndex)
        {
            poseUpdates[updateIndex].Apply(bipedIK, frameTime, firstFrame, animMode);
        }
    }

    public float GetTotalFrameTime()
    {
        return 1.0f;
    }

    public static PoseKeyframe CreateNew(int type)
    {
        return new PoseKeyframe();
    }

    public void Serialize(FileSerializer serializer)
    {
        serializer.SetObjectArray("updates", ref poseUpdates, PoseUpdate.CreateNew, PoseUpdate.GetType);
    }
}