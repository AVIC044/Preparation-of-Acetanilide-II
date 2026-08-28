using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Holds Animator + AnimationClip mappings per slide index and swaps the
/// active clip on the matching Animator(s) whenever a given index becomes
/// active. Only one instance should exist at a time - duplicates destroy
/// themselves in Awake.
///
/// Two ways to drive it:
///  1. Automatic - toggle "Listen To Slide Controller" on and it subscribes
///     to SlideController.OnSlideChanged, so it swaps clips the moment
///     SlideController advances/goes back to a page.
///  2. Manual - call the public SwapClipForIndex(int) method directly. It
///     can be wired into any UnityEvent (e.g. a PageData.onNextClick /
///     onNextCompleted / onBackClick entry on SlideController) by picking
///     it under that event's "Static Parameters" section and typing in the
///     index for that page.
/// </summary>
public class SwapAnimationClipPerIndex : MonoBehaviour
{
    [System.Serializable]
    public class SlideConfiguration
    {
        [Header("Slide")]
        [Tooltip("The slide index (matches SlideController's page index) this entry applies to.")]
        public int slideIndex;

        [Header("Target")]
        [Tooltip("The Animator whose playing clip should be swapped for this slide index.")]
        public Animator targetAnimator;

        [Header("Clip To Swap In")]
        [Tooltip("The clip that should play on targetAnimator when this slide index becomes active.")]
        public AnimationClip newClip;

        [Header("Optional")]
        [Tooltip("The specific clip inside targetAnimator's controller to replace. Leave empty to auto-use the first clip found in the controller (fine when that controller only drives a single clip). Set this explicitly if the controller has more than one clip.")]
        public AnimationClip originalClip;
    }

    public static SwapAnimationClipPerIndex Instance { get; private set; }

    [Header("Index -> Animator/Clip Mappings")]
    public List<SlideConfiguration> slideConfigurations = new List<SlideConfiguration>();

    [Header("Auto-Sync With SlideController")]
    [Tooltip("If enabled, automatically calls SwapClipForIndex whenever SlideController.OnSlideChanged fires.")]
    public bool listenToSlideController = true;

    [Header("Playback")]
    [Tooltip("If enabled, restarts the animator's current state from frame 0 right after swapping, so the new clip is visible immediately.")]
    public bool restartAnimatorOnSwap = true;

    // One AnimatorOverrideController per Animator, created lazily and reused
    // so repeated swaps don't keep allocating new override controllers.
    private readonly Dictionary<Animator, AnimatorOverrideController> overrideControllers = new Dictionary<Animator, AnimatorOverrideController>();

    // The true original clips per Animator, captured once before any override
    // is applied, so later swaps can still find the right key to replace
    // even after a previous swap has already run on that Animator.
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

    /// <summary>
    /// Swaps in the configured clip(s) for the given slide index. Public so
    /// it can be assigned to a UnityEvent (with index as a Static Parameter)
    /// or subscribed to directly as an Action&lt;int&gt;.
    /// </summary>
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

    /// <summary>
    /// Convenience for UnityEvents that can't pass a parameter at all -
    /// swaps using SlideController's current index.
    /// </summary>
    public void SwapClipForCurrentIndex()
    {
        SwapClipForIndex(SlideController.CurrentIndex);
    }

    void ApplyClip(SlideConfiguration config)
    {
        if (config.targetAnimator == null)
        {
            Debug.LogWarning("[SwapAnimationClipPerIndex] No Animator assigned for slide index " + config.slideIndex + ".", this);
            return;
        }

        if (config.newClip == null)
        {
            Debug.LogWarning("[SwapAnimationClipPerIndex] No new clip assigned for slide index " + config.slideIndex + " on " + config.targetAnimator.name + ".", config.targetAnimator);
            return;
        }

        AnimatorOverrideController overrideController = GetOrCreateOverrideController(config.targetAnimator);
        if (overrideController == null)
            return;

        AnimationClip clipToReplace = config.originalClip;
        if (clipToReplace == null)
            clipToReplace = FindFirstOriginalClip(config.targetAnimator);

        if (clipToReplace == null)
        {
            Debug.LogWarning("[SwapAnimationClipPerIndex] Could not find a clip to replace on " + config.targetAnimator.name + " for slide index " + config.slideIndex + ".", config.targetAnimator);
            return;
        }

        overrideController[clipToReplace] = config.newClip;

        if (restartAnimatorOnSwap)
        {
            AnimatorStateInfo stateInfo = config.targetAnimator.GetCurrentAnimatorStateInfo(0);
            config.targetAnimator.Play(stateInfo.fullPathHash, 0, 0f);
        }
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

        // Cache the original clips BEFORE wrapping, so later swaps can still
        // find the correct key even after overrides have been applied.
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