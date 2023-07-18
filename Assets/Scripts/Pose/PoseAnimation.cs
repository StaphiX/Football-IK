
using System.Collections.Generic;
using RootMotion.FinalIK;
using UnityEngine;

public class AxisCurveValue : ISerialize
{
    public float time = 0.0f;
    public float offset = 0.0f;

    public AxisCurveValue(float time, float offset)
    {
        this.time = time;
        this.offset = offset;
    }

    public void Serialize(FileSerializer serializer)
    {
        serializer.Set("time", ref time, 0.0f, 1);
    }

    public static AxisCurveValue CreateNew(int type)
    {
        return new AxisCurveValue(0.0f, 0.0f);
    }
}

public class AxisCurve : ISerialize
{
    public List<AxisCurveValue> values = null;

    public void AddValue(float time, float offset)
    {
        if (values == null)
            values = new List<AxisCurveValue>();

        int insertIndex = 0;
        for (int valueIndex = 0; valueIndex < values.Count; ++valueIndex)
        {
            insertIndex = valueIndex;
            if (time < values[valueIndex].time)
                break;
        }

        values.Insert(insertIndex, new AxisCurveValue(time, offset));
    }

    public float GetValue(float animTime)
    {
        float prevTime = 0.0f;
        float prevOffset = 0.0f;
        for (int valueIndex = 0; valueIndex < values.Count; ++valueIndex)
        {
            if (animTime < values[valueIndex].time)
            {
                float curveTime = Mathf.InverseLerp(animTime, prevTime, values[valueIndex].time);
                return MathUtil.CubicBezier(prevOffset, values[valueIndex].offset, 0.01f, 0.01f, curveTime);
            }

            prevTime = values[valueIndex].time;
            prevOffset = values[valueIndex].offset;
        }

        return animTime;
    }

    public void Serialize(FileSerializer serializer)
    {
        serializer.SetObjectArray("values", ref values, AxisCurveValue.CreateNew);
    }

    public static AxisCurve CreateNew(int type)
    {
        return new AxisCurve();
    }
}

public class AnimationAxisCurve : ISerialize
{
    List<AxisCurve> axisCurves = new List<AxisCurve> { null, null, null };

    public AnimationAxisCurve()
    {
        axisCurves[0] = null;
        axisCurves[1] = null;
        axisCurves[2] = null;
    }

    public Vector3 GetValue(Vector3 start, Vector3 end, float animTime)
    {
        float xScale = GetCurveValue(axisCurves[0], animTime);
        float yScale = GetCurveValue(axisCurves[1], animTime);
        float zScale = GetCurveValue(axisCurves[2], animTime);

        Vector3 diff = end - start;

        return new Vector3(start.x + (diff.x* xScale), start.y + (diff.y * yScale), start.z + (diff.z * zScale));
    }

    private float GetCurveValue(AxisCurve curve, float animTime)
    {
        if (curve == null || curve.values == null)
            return animTime;

        return curve.GetValue(animTime);
    }

    private void SetValue(Vector3 start, Vector3 end, float linearTime, Vector3 curveOffset)
    {
        Vector3 lerpOffset = Vector3.Lerp(start, end, linearTime);
        for (int axis = 0; axis < 3; ++axis)
        {
            if (!Equals(curveOffset[axis], lerpOffset[axis]))
            {
                if (axisCurves[axis] == null)
                    axisCurves[axis] = new AxisCurve();

                axisCurves[axis].AddValue(linearTime, curveOffset[axis]);
            }
        }
    }

    public void Serialize(FileSerializer serializer)
    {
        serializer.SetObjectArray("axisCurves", ref axisCurves, AxisCurve.CreateNew);
    }
}

public enum PoseAnimationMode
{
    None,
    Flip, // Left = Right
}

public class PoseAnimation : ISerialize
{
    public PoseAnimationMode mode = PoseAnimationMode.None;
    public List<PoseKeyframe> keyframes = new List<PoseKeyframe>();
    public AnimationCurve curve = null;

    public void SetAnimationMode(PoseAnimationMode setMode, BipedIK bipedIK)
    {
        if(mode == PoseAnimationMode.Flip || setMode == PoseAnimationMode.Flip)
        {
            FlipKeyframes(bipedIK);
        }

        mode = setMode;
    }

    public void FlipKeyframes(BipedIK bipedIK)
    {
        for(int keyframeIndex = 0; keyframeIndex < keyframes.Count; ++keyframeIndex)
        {
            keyframes[keyframeIndex].Flip(bipedIK);
        }
    }

    public void Serialize(FileSerializer serializer)
    {
        serializer.SetObjectArray("keyframes", ref keyframes, PoseKeyframe.CreateNew);
    }
}

public class PoseAnimator
{
    float time = 0.0f;
    float frameTime = 0.0f;
    float frameTimeScaled = 0.0f;
    int keyframeIndex = 0;
    bool loop = false;
    bool ended = false;

    public PoseAnimator(bool loop)
    {
        Reset(loop);
    }

    public void Delete()
    {
        ended = true;
    }

    public void Reset(bool loop = false)
    {
        this.time = 0.0f;
        this.frameTime = 0.0f;
        this.frameTimeScaled = 0.0f;
        this.keyframeIndex = 0;
        this.loop = loop;
    }

    private void End(PoseKeyframe keyframe)
    {
        frameTime = keyframe.GetTotalFrameTime();
        frameTimeScaled = 1.0f;
    }

    public bool Update(PoseAnimation animation)
    {
        if (keyframeIndex < 0 || keyframeIndex >= animation.keyframes.Count)
            return true;

        PoseKeyframe keyframe = animation.keyframes[keyframeIndex];

        if(ended)
        {
            End(keyframe);
            return true;
        }

        time += Time.deltaTime;
        frameTime += Time.deltaTime;

        float totalFrameTime = keyframe.GetTotalFrameTime();
        if (frameTime > totalFrameTime)
        {
            if(!loop && keyframeIndex >= animation.keyframes.Count-1)
            {
                End(keyframe);
                return true;
            }

            frameTime = frameTime - totalFrameTime;
            keyframeIndex = keyframeIndex + 1;

            if (keyframeIndex >= animation.keyframes.Count)
            {
                Reset(loop);
            }
        }

        keyframe = animation.keyframes[keyframeIndex];
        totalFrameTime = keyframe.GetTotalFrameTime();
        frameTimeScaled = totalFrameTime > 0.0f ? frameTime / totalFrameTime : 1.0f;

        return false;
    }

    public float GetTime()
    {
        return time;
    }

    public int GetKeyframeIndex()
    {
        return keyframeIndex;
    }

    public float GetFrameTimeScaled()
    {
        return frameTimeScaled;
    }
}