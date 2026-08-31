using UnityEngine;
using System.Collections.Generic;

public class SlideAnimationController : MonoBehaviour
{
    [System.Serializable]
    public class AnimationAction
    {
        [Tooltip("Drag the GameObject or Animator you want to animate here.")]
        public Animator targetAnimator;

        [Tooltip("The exact name of the animation state to play.")]
        public string animationName;

        [HideInInspector]
        public bool hasPlayed = false;
    }

    [System.Serializable]
    public class SlideAnimation
    {
        [Header("Slide Settings")]
        public int slideIndex;

        [Tooltip("If enabled, animations will NOT play automatically. Call PlayCurrentSlideAnimations() to play them.")]
        public bool requireFunctionCall;

        [Header("Animations for this Slide")]
        public List<AnimationAction> animationsToPlay;
    }

    [Header("Slide Configuration")]
    public List<SlideAnimation> slideAnimations;

    [Header("Shared Animator Controller")]
    [Tooltip("The Animator Controller to assign to the target Animator.")]
    public RuntimeAnimatorController globalAnimator;

    private int currentSlideIndex = -1;

    private void OnEnable()
    {
        SlideController.OnSlideChanged += OnSlideChanged;
    }

    private void OnDisable()
    {
        SlideController.OnSlideChanged -= OnSlideChanged;
    }

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
}