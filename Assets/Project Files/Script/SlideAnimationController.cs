
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class SlideAnimationController : MonoBehaviour
{
    // =========================================================
    // ANIMATION FRAME EVENT
    // =========================================================

    [System.Serializable]
    public class AnimationFrameEvent
    {
        [Tooltip("Frame number where the event should be triggered.")]
        public int frame = 55;

        [Tooltip("Event that will be invoked when the animation reaches this frame.")]
        public UnityEvent onFrameReached;

        [HideInInspector]
        public bool hasTriggered = false;
    }

    // =========================================================
    // ANIMATION ACTION
    // =========================================================

    [System.Serializable]
    public class AnimationAction
    {
        [Tooltip("Drag the GameObject or Animator you want to animate here.")]
        public Animator targetAnimator;

        [Tooltip("The exact name of the animation state to play.")]
        public string animationName;

        [Header("Frame Events")]
        [Tooltip("Add events that should happen at specific frames.")]
        public List<AnimationFrameEvent> frameEvents;

        [HideInInspector]
        public bool hasPlayed = false;

        [HideInInspector]
        public float animationStartTime = -1f;
    }

    // =========================================================
    // SLIDE ANIMATION
    // =========================================================

    [System.Serializable]
    public class SlideAnimation
    {
        [Header("Slide Settings")]
        public int slideIndex;

        [Tooltip(
            "If enabled, animations will NOT play automatically. " +
            "Call PlayCurrentSlideAnimations() to play them."
        )]
        public bool requireFunctionCall;

        [Header("Animations for this Slide")]
        public List<AnimationAction> animationsToPlay;
    }

    // =========================================================
    // INSPECTOR SETTINGS
    // =========================================================

    [Header("Slide Configuration")]
    public List<SlideAnimation> slideAnimations;

    [Header("Shared Animator Controller")]
    [Tooltip("The Animator Controller to assign to the target Animator.")]
    public RuntimeAnimatorController globalAnimator;

    // =========================================================
    // INTERNAL VARIABLES
    // =========================================================

    private int currentSlideIndex = -1;

    // =========================================================
    // ENABLE / DISABLE
    // =========================================================

    private void OnEnable()
    {
        SlideController.OnSlideChanged += OnSlideChanged;
    }

    private void OnDisable()
    {
        SlideController.OnSlideChanged -= OnSlideChanged;
    }

    // =========================================================
    // UPDATE
    // Checks animation frame events
    // =========================================================

    private void Update()
    {
        CheckAnimationFrameEvents();
    }

    // =========================================================
    // SLIDE CHANGED
    // =========================================================

    private void OnSlideChanged(int slideIndex)
    {
        currentSlideIndex = slideIndex;

        foreach (SlideAnimation slideAnimation in slideAnimations)
        {
            if (slideAnimation.slideIndex != currentSlideIndex)
                continue;

            // If checkbox is enabled,
            // DON'T play automatically.
            if (slideAnimation.requireFunctionCall)
            {
                Debug.Log(
                    $"Slide {slideIndex} requires a function call before animations play."
                );

                continue;
            }

            // Normal automatic animation
            PlayAnimations(slideAnimation);
        }
    }

    // =========================================================
    // PUBLIC FUNCTION
    // Call this function from another script / UnityEvent
    // =========================================================

    public void PlayCurrentSlideAnimations()
    {
        foreach (SlideAnimation slideAnimation in slideAnimations)
        {
            if (slideAnimation.slideIndex != currentSlideIndex)
                continue;

            PlayAnimations(slideAnimation);

            Debug.Log(
                $"Function called. Playing animations for slide {currentSlideIndex}."
            );

            return;
        }

        Debug.LogWarning(
            $"No animation configuration found for slide {currentSlideIndex}."
        );
    }

    // =========================================================
    // PLAY ANIMATIONS
    // =========================================================

    private void PlayAnimations(SlideAnimation slideAnimation)
    {
        foreach (AnimationAction action in slideAnimation.animationsToPlay)
        {
            if (action.targetAnimator == null)
            {
                Debug.LogWarning(
                    $"Missing Animator reference on slide {slideAnimation.slideIndex}!"
                );

                continue;
            }

            if (string.IsNullOrEmpty(action.animationName))
            {
                Debug.LogWarning(
                    $"Animation name is empty on slide {slideAnimation.slideIndex}!"
                );

                continue;
            }

            // Don't play the same animation twice
            if (action.hasPlayed)
                continue;

            // Assign shared Animator Controller
            if (globalAnimator != null)
            {
                if (action.targetAnimator.runtimeAnimatorController
                    != globalAnimator)
                {
                    action.targetAnimator.runtimeAnimatorController =
                        globalAnimator;
                }
            }

            // Reset frame events
            ResetFrameEvents(action);

            // Store animation start time
            action.animationStartTime = Time.time;

            // Play animation
            action.targetAnimator.Play(
                action.animationName,
                0,
                0f
            );

            action.hasPlayed = true;

            Debug.Log(
                $"Playing '{action.animationName}' on " +
                $"'{action.targetAnimator.gameObject.name}' " +
                $"for slide {slideAnimation.slideIndex}."
            );
        }
    }

    // =========================================================
    // CHECK FRAME EVENTS
    // =========================================================

    private void CheckAnimationFrameEvents()
    {
        if (slideAnimations == null)
            return;

        foreach (SlideAnimation slideAnimation in slideAnimations)
        {
            if (slideAnimation.slideIndex != currentSlideIndex)
                continue;

            if (slideAnimation.animationsToPlay == null)
                continue;

            foreach (AnimationAction action in slideAnimation.animationsToPlay)
            {
                if (!action.hasPlayed)
                    continue;

                if (action.targetAnimator == null)
                    continue;

                if (action.frameEvents == null ||
                    action.frameEvents.Count == 0)
                    continue;

                AnimatorStateInfo stateInfo =
                    action.targetAnimator.GetCurrentAnimatorStateInfo(0);

                // Make sure the requested animation is currently playing
                if (!stateInfo.IsName(action.animationName))
                    continue;

                // Get animation clip length
                AnimationClip clip = FindAnimationClip(
                    action.targetAnimator,
                    action.animationName
                );

                if (clip == null)
                    continue;

                // Current normalized time
                float normalizedTime = stateInfo.normalizedTime % 1f;

                // Convert normalized time to seconds
                float currentTime =
                    normalizedTime * clip.length;

                // Convert seconds to frame
                float currentFrame =
                    currentTime * clip.frameRate;

                // Check every frame event
                foreach (AnimationFrameEvent frameEvent in action.frameEvents)
                {
                    if (frameEvent == null)
                        continue;

                    if (frameEvent.hasTriggered)
                        continue;

                    // Check if animation reached requested frame
                    if (currentFrame >= frameEvent.frame)
                    {
                        TriggerFrameEvent(
                            action,
                            frameEvent
                        );
                    }
                }
            }
        }
    }

    // =========================================================
    // TRIGGER FRAME EVENT
    // =========================================================

    private void TriggerFrameEvent(
        AnimationAction action,
        AnimationFrameEvent frameEvent)
    {
        frameEvent.hasTriggered = true;

        Debug.Log(
            $"Animation '{action.animationName}' reached frame " +
            $"{frameEvent.frame}."
        );

        frameEvent.onFrameReached?.Invoke();
    }

    // =========================================================
    // RESET FRAME EVENTS
    // =========================================================

    private void ResetFrameEvents(AnimationAction action)
    {
        if (action.frameEvents == null)
            return;

        foreach (AnimationFrameEvent frameEvent in action.frameEvents)
        {
            if (frameEvent != null)
            {
                frameEvent.hasTriggered = false;
            }
        }
    }

    // =========================================================
    // FIND ANIMATION CLIP
    // =========================================================

    private AnimationClip FindAnimationClip(
        Animator animator,
        string animationName)
    {
        if (animator == null)
            return null;

        RuntimeAnimatorController controller =
            animator.runtimeAnimatorController;

        if (controller == null)
            return null;

        AnimationClip[] clips = controller.animationClips;

        foreach (AnimationClip clip in clips)
        {
            if (clip == null)
                continue;

            if (clip.name == animationName)
            {
                return clip;
            }
        }

        return null;
    }

    // =========================================================
    // OPTIONAL FUNCTION
    // Play animations for a specific slide manually
    // =========================================================

    public void PlaySlideAnimations(int slideIndex)
    {
        foreach (SlideAnimation slideAnimation in slideAnimations)
        {
            if (slideAnimation.slideIndex == slideIndex)
            {
                PlayAnimations(slideAnimation);

                Debug.Log(
                    $"Manually playing animations for slide {slideIndex}."
                );

                return;
            }
        }

        Debug.LogWarning(
            $"No animation configuration found for slide {slideIndex}."
        );
    }

    // =========================================================
    // OPTIONAL PUBLIC FUNCTION
    // MANUALLY TRIGGER A SPECIFIC FRAME EVENT
    // =========================================================

    public void TriggerAnimationFrameEvent(
        int slideIndex,
        int animationIndex,
        int frameIndex)
    {
        if (slideAnimations == null)
            return;

        foreach (SlideAnimation slideAnimation in slideAnimations)
        {
            if (slideAnimation.slideIndex != slideIndex)
                continue;

            if (slideAnimation.animationsToPlay == null)
                return;

            if (animationIndex < 0 ||
                animationIndex >= slideAnimation.animationsToPlay.Count)
                return;

            AnimationAction action =
                slideAnimation.animationsToPlay[animationIndex];

            if (action.frameEvents == null)
                return;

            foreach (AnimationFrameEvent frameEvent in action.frameEvents)
            {
                if (frameEvent.frame == frameIndex)
                {
                    frameEvent.onFrameReached?.Invoke();

                    Debug.Log(
                        $"Manually triggered frame {frameIndex} " +
                        $"event for animation '{action.animationName}'."
                    );

                    return;
                }
            }
        }
    }
}

