
using RootMotion.FinalIK;
using UnityEngine;

public enum BipedTarget
{
    LeftFoot,
    RightFoot,
    LeftHand,
    RightHand,
    Pelvis,
    Spine,
    LookAt,
    Count,
}

public enum PoseUpdateType
{
    Position,
    Rotation,
    TargetOffset,
    Count
}

public abstract class PoseUpdate : ISerialize
{
    public PoseUpdate(BipedTarget target) { this.target = target; }
    public BipedTarget target;
    public AnimationAxisCurve animCurve = new AnimationAxisCurve();

    public abstract PoseUpdateType Type();
    public bool IsType(PoseUpdate compare) { return compare.Type() == Type(); }
    public abstract void Apply(BipedIK bipedIK, float frameTime = 1.0f, bool firstFrame = false, PoseAnimationMode animMode = PoseAnimationMode.None);

    public virtual void Flip(BipedIK bipedIK)
    {
        if (target == BipedTarget.LeftFoot)
            target = BipedTarget.RightFoot;
        else if (target == BipedTarget.LeftHand)
            target = BipedTarget.RightHand;
        else if (target == BipedTarget.RightFoot)
            target = BipedTarget.LeftFoot;
        else if (target == BipedTarget.RightHand)
            target= BipedTarget.LeftHand;
    }

    public virtual void Serialize(FileSerializer serializer)
    {
        int targetInt = (int)target;

        serializer.Set("target", ref targetInt);

        if (serializer.isReading)
            target = (BipedTarget)targetInt;
    }

    public static PoseUpdate CreateNew(int type)
    {
        switch ((PoseUpdateType)type)
        {
            case PoseUpdateType.Position:
                return new PoseUpdatePosition();
            case PoseUpdateType.TargetOffset:
                return new PoseUpdateTargetWithOffset();
            case PoseUpdateType.Rotation:
                return new PoseUpdateRotation();
            default:
                Debug.Assert(false, "Invalid PoseUpdate type");
                return new PoseUpdatePosition();
        }
    }

    public static int GetType(PoseUpdate poseUpdate)
    {
        return (int)poseUpdate.Type();
    }
}

public class PoseUpdatePosition : PoseUpdate
{
    public PoseUpdatePosition() : base(BipedTarget.Count) { }
    public PoseUpdatePosition(BipedTarget target, Vector3 position, BipedIK bipedIK) : base(target)
    {
        Set(position, bipedIK);
    }

    public Vector3 positionOffset;
    private Vector3? prevPosition = null;

    public override PoseUpdateType Type() { return PoseUpdateType.Position; }
    public void Set(Vector3 position, BipedIK bipedIK)
    {
        this.positionOffset = position - bipedIK.GetRootPosition();
        prevPosition = null;
    }

    public override void Flip(BipedIK bipedIK)
    {
        base.Flip(bipedIK);
        Vector3 reflectedOffset = Vector3.Reflect(positionOffset, bipedIK.GetRootTransform().right);
        positionOffset = reflectedOffset;
    }

    public override void Apply(BipedIK bipedIK, float frameTime = 1.0f, bool firstFrame = false, PoseAnimationMode animMode = PoseAnimationMode.None)
    {
        if(prevPosition == null || firstFrame)
            prevPosition = bipedIK.GetSolverPosition(target);

        Vector3 targetPosition = bipedIK.GetRootPosition() + positionOffset;
        Vector3 newPosition = animCurve.GetValue(prevPosition.Value, targetPosition, frameTime);
        bipedIK.SetSolverPosition(target, newPosition);
    }
    public override void Serialize(FileSerializer serializer)
    {
        base.Serialize(serializer);
        serializer.Set("positionOffset", ref positionOffset, Vector3.zero);
    }
}

public class PoseUpdateTargetWithOffset : PoseUpdate
{
    public PoseUpdateTargetWithOffset() : base(BipedTarget.Count) { }
    public PoseUpdateTargetWithOffset(BipedTarget target, Vector3 offsetTarget, Vector3 offset) : base(target)
    {
        this.offsetTarget = offsetTarget;
        this.offset = offset;
    }
    public Vector3 offsetTarget;
    public Vector3 offset;
    private Vector3? prevPosition = null;

    public override PoseUpdateType Type() { return PoseUpdateType.TargetOffset; }
    public void Set(Vector3 offsetTarget, Vector3 offset)
    {
        this.offsetTarget = offsetTarget;
        this.offset = offset;
        prevPosition = null;
    }

    public override void Flip(BipedIK bipedIK)
    {
        base.Flip(bipedIK);
        Vector3 offsetPosition = offsetTarget + offset;
        Vector3 offsetDir = offsetPosition - bipedIK.GetRootPosition();
        Vector3 reflectedDir = Vector3.Reflect(offsetDir, bipedIK.GetRootTransform().right);
        Vector3 newOffsetPosition = bipedIK.GetRootPosition() + reflectedDir;
        offset = newOffsetPosition - offsetTarget;
    }

    public override void Apply(BipedIK bipedIK, float frameTime = 1.0f, bool firstFrame = false, PoseAnimationMode animMode = PoseAnimationMode.None)
    {
        if (prevPosition == null || firstFrame)
            prevPosition = bipedIK.GetSolverPosition(target);

        Vector3 newPosition = animCurve.GetValue(prevPosition.Value, offsetTarget, frameTime);
        bipedIK.SetSolverPosition(target, newPosition);
    }
    public override void Serialize(FileSerializer serializer)
    {
        base.Serialize(serializer);
        serializer.Set("offsetTarget", ref offsetTarget, Vector3.zero);
        serializer.Set("offset", ref offset, Vector3.zero);
    }
}

public class PoseUpdateRotation : PoseUpdate
{
    public PoseUpdateRotation() : base(BipedTarget.Count) { }
    public PoseUpdateRotation(BipedTarget target, Quaternion rotation) : base(target) { this.rotation = rotation; }
    public Quaternion rotation;
    private Quaternion? prevRotation = null;
    public override PoseUpdateType Type() { return PoseUpdateType.Rotation; }
    public void Set(Quaternion rotation)
    {
        this.rotation = rotation;
        prevRotation = null;
    }

    public override void Flip(BipedIK bipedIK)
    {
        base.Flip(bipedIK);
        rotation = ReflectRotation(rotation, bipedIK.solvers.pelvis.transform.right);
    }

    private Quaternion ReflectRotation(Quaternion source, Vector3 normal)
    {
        return Quaternion.LookRotation(Vector3.Reflect(source * Vector3.forward, normal), Vector3.Reflect(source * Vector3.up, normal));
    }

    public override void Apply(BipedIK bipedIK, float frameTime = 1.0f, bool firstFrame = false, PoseAnimationMode animMode = PoseAnimationMode.None)
    {
        if (prevRotation == null || firstFrame)
            prevRotation = bipedIK.GetSolverRotation(target);

        Vector3 eulerAngles = animCurve.GetValue(prevRotation.Value.eulerAngles, rotation.eulerAngles, frameTime);

        Quaternion nextRotation = Quaternion.Euler(eulerAngles);
        bipedIK.SetSolverRotation(target, nextRotation);
    }

    public override void Serialize(FileSerializer serializer)
    {
        base.Serialize(serializer);
        Vector3 euler = serializer.isReading ? Vector3.zero : rotation.eulerAngles;
        serializer.Set("rotation", ref euler, Vector3.zero);

        if (serializer.isReading)
            rotation = Quaternion.Euler(euler);
    }
}