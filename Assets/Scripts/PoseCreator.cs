using UnityEngine;
using RootMotion.FinalIK;

public class PoseCreator : MonoBehaviour
{
    bool isInitialised = false;
    PoseAnimation poseAnimation;
    PoseAnimationMode poseAnimMode = PoseAnimationMode.None;
    PoseAnimator poseAnimator = null;
    PoseKeyframe currentKeyframe = null;
    int currentKeyframeIndex = 0;

    BipedIK bipedIK;

    GameObject[] IKPositions = new GameObject[(int)BipedTarget.Count];
    GameObject[] limbTargets = new GameObject[(int)BipedTarget.Count];

    BipedTarget selectedTarget = BipedTarget.Count;
    bool isLimbTargetSelected = false;

    Color baseColor = Color.magenta;

    private void Awake()
    {
        bipedIK = GetComponent<BipedIK>();
        CreateIKTargets();
    }

    // BipedIK script is forced to initialise late so we need to work around that to affect it on the first frame
    private void Init()
    {
        if (isInitialised)
            return;

        isInitialised = true;

        bool loadSuccess = LoadPose("test");

        if (!loadSuccess)
        {
            poseAnimation = new PoseAnimation();
            SetCurrentKeyframe(0, true);
            SetSelectedTarget(BipedTarget.LeftFoot);
        }
    }

    private void FixedUpdate()
    {
        Init();

        UpdateAnimator();
    }

    public void CreateIKTargets()
    {
        GameObject targetPrefab = Resources.Load("Prefabs/IKTarget") as GameObject;
        if (targetPrefab == null)
            return;

        MeshRenderer render = targetPrefab.GetComponent<MeshRenderer>();

        baseColor = render.sharedMaterial.color;

        GameObject parent = GameObject.Find("IKTargets") ?? new GameObject("IKTargets");
        for (BipedTarget target = BipedTarget.LeftFoot; target < BipedTarget.Count; ++target)
        {
            string name = target.ToString();
            if (IKPositions[(int)target] == null)
            {
                Vector3 ikPosition = bipedIK.GetSolverPosition(target);
                Quaternion ikRotation = bipedIK.GetSolverRotation(target);
                IKPositions[(int)target] = GameObject.Instantiate(targetPrefab, ikPosition, ikRotation, parent.transform);
                IKPositions[(int)target].name = name;
            }

            limbTargets[(int)target] = null;
        }
    }

    public void SetSelectedTarget(BipedTarget target, bool useLimbTarget = false)
    {
        if (selectedTarget == target && useLimbTarget == isLimbTargetSelected)
        {
            return;
        }

        DeselectTarget(selectedTarget);

        selectedTarget = target;
        isLimbTargetSelected = useLimbTarget;

        GameObject limbTarget = limbTargets[(int)selectedTarget];
        GameObject IKPosition = IKPositions[(int)selectedTarget];

        if (isLimbTargetSelected && limbTarget == null)
        {
            limbTarget = limbTargets[(int)selectedTarget] = Instantiate(IKPosition, IKPosition.transform.position, IKPosition.transform.rotation, IKPosition.transform.parent);
            limbTargets[(int)selectedTarget].transform.localScale = IKPosition.transform.localScale;

            limbTarget.name = IKPosition.name + " Target";
        }

        if (limbTarget != null)
        {
            MeshRenderer ltRenderer = limbTarget.GetComponent<MeshRenderer>();
            ltRenderer.material.color = ColorUtil.RGB(100, 255, 231);
        }

        if (IKPosition != null)
        {
            MeshRenderer IkRenderer = IKPosition.GetComponent<MeshRenderer>();
            IkRenderer.material.color = ColorUtil.RGB(255, 179, 231);
        }
    }

    public void DeselectTarget(BipedTarget target)
    {
        if (selectedTarget < 0 || selectedTarget >= BipedTarget.Count)
            return;

        GameObject limbTarget = limbTargets[(int)selectedTarget];
        GameObject IKPosition = IKPositions[(int)selectedTarget];

        if (limbTarget != null)
        {
            MeshRenderer ltRenderer = limbTarget.GetComponent<MeshRenderer>();
            ltRenderer.material.color = baseColor;
        }

        if (IKPosition != null)
        {
            MeshRenderer IkRenderer = IKPosition.GetComponent<MeshRenderer>();
            IkRenderer.material.color = baseColor;
        }
    }

    public void HandleInput()
    {
        //Play animation
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if(poseAnimator != null)
            {
                poseAnimator.Delete();
                return;
            }
            poseAnimator = new PoseAnimator(true);
            return;
        }

        // Change Selected Target / Limb Target
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            SetSelectedTarget((BipedTarget)((int)(selectedTarget + 1) % (int)BipedTarget.Count));
            return;
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            SetSelectedTarget(selectedTarget, !isLimbTargetSelected);
            return;
        }

        // Keyframe Inputs
        if (Input.GetKeyDown(KeyCode.Period))
        {
            SetCurrentKeyframe(currentKeyframeIndex + 1, false);
            return;
        }

        if (Input.GetKeyDown(KeyCode.Comma))
        {
            SetCurrentKeyframe(currentKeyframeIndex - 1, false);
            return;
        }

        if (Input.GetKeyDown(KeyCode.Minus))
        {
            DeleteCurrentKeyframe();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Equals))
        {
            AddNewKeyframe();
            return;
        }

        // Move Targets
        if (selectedTarget >= 0 && selectedTarget < BipedTarget.Count)
        {
            if (Input.GetKey(KeyCode.LeftControl))
            {
                if (Input.GetKeyDown(KeyCode.S))
                    Save();
                return;
            }

            if (Input.GetKey(KeyCode.A))
            {
                AxisMoveTarget(Vector3.left);
            }
            else if (Input.GetKey(KeyCode.D))
            {
                AxisMoveTarget(Vector3.right);
            }
            if (Input.GetKey(KeyCode.W))
            {
                AxisMoveTarget(Vector3.up);
            }
            else if (Input.GetKey(KeyCode.S))
            {
                AxisMoveTarget(Vector3.down);
            }
            if (Input.GetKey(KeyCode.Q))
            {
                AxisMoveTarget(Vector3.back);
            }
            else if (Input.GetKey(KeyCode.E))
            {
                AxisMoveTarget(Vector3.forward);
            }
        }
    }

    public void SnapToTarget()
    {
        GameObject limbTarget = limbTargets[(int)selectedTarget];
        GameObject IKPosition = IKPositions[(int)selectedTarget];

        if (limbTarget != null && IKPosition != null)
        {
            IKPosition.transform.position = limbTarget.transform.position;
        }

        OnTargetUpdate(selectedTarget);
    }

    public void Save()
    {
        BipedAnimSerializer.SaveJson("test", poseAnimation);
        //BipedAnimSerializer.SaveBinary("test", poseAnimation);
    }

    public void AxisMoveTarget(Vector3 axis)
    {
        float moveScale = 1f;
        GameObject selectedObject = GetSelectedObject();
        if (selectedObject == null)
            return;

        selectedObject.transform.position = selectedObject.transform.position + (axis * moveScale * Time.deltaTime);

        OnTargetUpdate(selectedTarget);
    }

    public void Update()
    {
        HandleInput();
    }

    void OnTargetUpdate(BipedTarget target)
    {
        GameObject ikTarget = GetIKTarget(target);
        bipedIK.SetSolverPosition(target, ikTarget.transform.position, 1.0f);

        if (target == BipedTarget.Pelvis)
        {
            for (BipedTarget updateTarget = 0; updateTarget < BipedTarget.Count; ++updateTarget)
            {
                UpdateTarget(updateTarget);
            }
        }

        OnPositionChanged();
    }

    void UpdateTarget(BipedTarget target)
    {
        Vector3 position = bipedIK.GetSolverPosition(target);
        GameObject ikTarget = GetIKTarget(target);
        ikTarget.transform.position = position;
    }

    bool LoadPose(string pose)
    {
        bool loadSuccess = BipedAnimSerializer.LoadJson(pose, out poseAnimation);

        //BipedAnimSerializer.LoadBinary(pose, out PoseAnimation tempPose);     

        if (!loadSuccess)
            return false;

        SetCurrentKeyframe(0, true);
        SetSelectedTarget(BipedTarget.LeftFoot);

        return true;
    }

    GameObject GetIKTarget(BipedTarget target)
    {
        return IKPositions[(int)target];
    }

    GameObject GetSelectedObject()
    {
        return isLimbTargetSelected ? limbTargets[(int)selectedTarget] : IKPositions[(int)selectedTarget];
    }

    public void SetCurrentKeyframe(int keyframeIndex, bool addNew)
    {
        if (keyframeIndex < 0)
            keyframeIndex = Mathf.Max(poseAnimation.keyframes.Count - 1, 0);

        while (keyframeIndex >= poseAnimation.keyframes.Count)
        {
            if (addNew)
                poseAnimation.keyframes.Add(new PoseKeyframe());
            else
                keyframeIndex = 0;
        }

        bool addAllKeyframes = currentKeyframeIndex != keyframeIndex-1;

        currentKeyframeIndex = keyframeIndex;
        currentKeyframe = poseAnimation.keyframes[currentKeyframeIndex];
        UpdateFromKeyframe(addAllKeyframes);
    }

    public void AddNewKeyframe()
    {
        int insertIndex = currentKeyframeIndex + 1;
        if (poseAnimation.keyframes.Count < 1)
            insertIndex = 0;

        poseAnimation.keyframes.Insert(insertIndex, new PoseKeyframe());

        SetCurrentKeyframe(insertIndex, false);
    }

    public void DeleteCurrentKeyframe()
    {
        if (currentKeyframe == null)
            return;

        if (poseAnimation.keyframes.Count == 1)
            return;

        poseAnimation.keyframes.RemoveAt(currentKeyframeIndex);

        SetCurrentKeyframe(Mathf.Max(currentKeyframeIndex - 1, 0), false);
    }

    public void UpdateFromKeyframe(bool addAllKeyframes)
    {
        if (currentKeyframe == null)
            return;

        int keyframeStart = addAllKeyframes ? 0 : currentKeyframeIndex;
        for (int keyframeIndex = keyframeStart; keyframeIndex < currentKeyframeIndex+1; ++keyframeIndex)
        {
            PoseKeyframe poseKeyframe = poseAnimation.keyframes[keyframeIndex];
            for (int updateIndex = 0; updateIndex < poseKeyframe.poseUpdates.Count; ++updateIndex)
            {
                PoseUpdate update = poseKeyframe.poseUpdates[updateIndex];
                update.Apply(bipedIK);
                UpdateTarget(update.target);
            }
        }
    }

    public void UpdateAnimator()
    {
        if (poseAnimator == null || poseAnimation == null)
            return;

        bool ended = poseAnimator.Update(poseAnimation);

        int animatorKeyframe = poseAnimator.GetKeyframeIndex();

        bool firstFrame = false;
        if(currentKeyframeIndex != animatorKeyframe)
        {
            if (animatorKeyframe < 0 || animatorKeyframe >= poseAnimation.keyframes.Count)
            {
                poseAnimator = null;
                return;
            }

            firstFrame = true;
            currentKeyframeIndex = animatorKeyframe;
            currentKeyframe = poseAnimation.keyframes[currentKeyframeIndex];
        }

        int nextIndex = currentKeyframeIndex < poseAnimation.keyframes.Count - 1 ? currentKeyframeIndex + 1 : 0;
        PoseKeyframe nextKeyframe = poseAnimation.keyframes[nextIndex];
        float frameTimeScaled = poseAnimator.GetFrameTimeScaled();

        nextKeyframe.Apply(bipedIK, frameTimeScaled, firstFrame, poseAnimMode);

        for (int updateIndex = 0; updateIndex < nextKeyframe.poseUpdates.Count; ++updateIndex)
        {
            PoseUpdate update = nextKeyframe.poseUpdates[updateIndex];
            UpdateTarget(update.target);
        }

        if(ended)
            poseAnimator = null;
    }

    public void OnPositionChanged()
    {
        if (currentKeyframe == null)
            return;

        bool hasTarget = limbTargets[(int)selectedTarget] != null;

        if (hasTarget)
        {
            PoseUpdateTargetWithOffset poseUpdate = (PoseUpdateTargetWithOffset)currentKeyframe.Get(PoseUpdateType.TargetOffset, selectedTarget);
            Vector3 target = limbTargets[(int)selectedTarget].transform.position;
            Vector3 offset = IKPositions[(int)selectedTarget].transform.position - target;
            if (poseUpdate == null)
            {
                poseUpdate = new PoseUpdateTargetWithOffset(selectedTarget, target, offset);
                currentKeyframe.Add(poseUpdate);
            }

            poseUpdate.Set(target, offset);
        }
        else
        {
            PoseUpdatePosition poseUpdate = (PoseUpdatePosition)currentKeyframe.Get(PoseUpdateType.Position, selectedTarget);
            Vector3 position = IKPositions[(int)selectedTarget].transform.position;
            if (poseUpdate == null)
            {
                poseUpdate = new PoseUpdatePosition(selectedTarget, position, bipedIK);
                currentKeyframe.Add(poseUpdate);
            }

            poseUpdate.Set(position, bipedIK);
        }
    }
}