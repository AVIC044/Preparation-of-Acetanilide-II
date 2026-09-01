using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Holds Animator + AnimationClip mappings per slide index and swaps the
/// active clip on the matching Animator(s) whenever a given index becomes
/// active. The controller is NEVER removed.
/// </summary>
public class SwapAnimationClipPerIndex : MonoBehaviour
{
    [System.Serializable]
    public class SlideConfiguration
    {
        [Header("Slide")]
        public int slideIndex;

        [Header("Target")]
        public Animator targetAnimator;

        [Header("Clip To Swap In")]
        public AnimationClip newClip;

        [Header("Optional")]
        public AnimationClip originalClip;
    }

    public static SwapAnimationClipPerIndex Instance { get; private set; }

    [Header("Index -> Animator/Clip Mappings")]
    public List<SlideConfiguration> slideConfigurations = new List<SlideConfiguration>();

    [Header("Auto-Sync With SlideController")]
    public bool listenToSlideController = true;

    [Header("Playback")]
    public bool restartAnimatorOnSwap = true;

    // Caches to manage controllers and prevent memory leaks
    private readonly Dictionary<Animator, AnimatorOverrideController> overrideControllers = new Dictionary<Animator, AnimatorOverrideController>();
    private readonly Dictionary<Animator, AnimationClip[]> originalClipsByAnimator = new Dictionary<Animator, AnimationClip[]>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[SwapAnimationClipPerIndex] Duplicate instance on " + gameObject.name + " - destroying it.", this);
            Destroy(this);
            return;
        }

        Instance = this;
    }

    void OnEnable()
    {
        if (listenToSlideController)
            SlideController.OnSlideChanged += SwapClipForIndex;
    }

    void OnDisable()
    {
        if (listenToSlideController)
            SlideController.OnSlideChanged -= SwapClipForIndex;
    }

    public void SwapClipForIndex(int index)
    {
        for (int i = 0; i < slideConfigurations.Count; i++)
        {
            SlideConfiguration config = slideConfigurations[i];
            if (config.slideIndex != index)
                continue;

            ApplyClip(config);
        }
    }

    public void SwapClipForCurrentIndex()
    {
        SwapClipForIndex(SlideController.CurrentIndex);
    }

    void ApplyClip(SlideConfiguration config)
    {
        if (config.targetAnimator == null || config.newClip == null)
            return;

        AnimatorOverrideController overrideController = GetOrCreateOverrideController(config.targetAnimator);
        if (overrideController == null)
            return;

        AnimationClip clipToReplace = config.originalClip;
        if (clipToReplace == null)
            clipToReplace = FindFirstOriginalClip(config.targetAnimator);

        if (clipToReplace == null)
            return;

        // Swap the clip
        overrideController[clipToReplace] = config.newClip;

        // Play the animation
        if (restartAnimatorOnSwap)
        {
            AnimatorStateInfo stateInfo = config.targetAnimator.GetCurrentAnimatorStateInfo(0);
            config.targetAnimator.Play(stateInfo.fullPathHash, 0, 0f);
            config.targetAnimator.Update(0f);
        }

        Debug.Log($"[SwapAnimationClipPerIndex] Swapped and playing '{config.newClip.name}' on '{config.targetAnimator.name}'.");
    }

    AnimatorOverrideController GetOrCreateOverrideController(Animator animator)
    {
        if (overrideControllers.TryGetValue(animator, out AnimatorOverrideController existing))
            return existing;

        RuntimeAnimatorController baseController = animator.runtimeAnimatorController;
        if (baseController == null)
        {
            Debug.LogWarning("[SwapAnimationClipPerIndex] " + animator.name + " has no Runtime Animator Controller assigned.", animator);
            return null;
        }

        // Cache the original clips BEFORE wrapping
        originalClipsByAnimator[animator] = baseController.animationClips;

        AnimatorOverrideController overrideController = new AnimatorOverrideController(baseController);
        animator.runtimeAnimatorController = overrideController;

        overrideControllers[animator] = overrideController;
        return overrideController;
    }

    AnimationClip FindFirstOriginalClip(Animator animator)
    {
        if (originalClipsByAnimator.TryGetValue(animator, out AnimationClip[] clips) && clips.Length > 0)
            return clips[0];

        return null;
    }
}