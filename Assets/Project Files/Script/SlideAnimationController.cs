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
    }

    [System.Serializable]
    public class SlideAnimation
    {
        [Header("Slide Settings")]
        public int slideIndex;

        [Header("Animations for this Slide")]
        public List<AnimationAction> animationsToPlay;
    }

    [Header("Slide Configuration")]
    public List<SlideAnimation> slideAnimations;

    [Header("Shared Animator Controller")]
    [Tooltip("The Animator Controller to assign to the target Animator.")]
    public RuntimeAnimatorController globalAnimator;

    private HashSet<int> playedSlides = new HashSet<int>();

    private void OnEnable()
    {
        SlideController.OnSlideChanged += OnSlideChanged;
    }

    private void OnDisable()
    {
        SlideController.OnSlideChanged -= OnSlideChanged;
    }

    private void OnSlideChanged(int currentSlideIndex)
    {
        // Don't play the same slide animation more than once
        if (playedSlides.Contains(currentSlideIndex))
            return;

        bool foundAnimation = false;

        foreach (SlideAnimation slideAnimation in slideAnimations)
        {
            if (slideAnimation.slideIndex == currentSlideIndex)
            {
                PlayAnimations(slideAnimation);

                foundAnimation = true;
            }
        }

        if (foundAnimation)
        {
            playedSlides.Add(currentSlideIndex);
        }
    }

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

            // Assign the controller ONLY if it is not already assigned
            if (action.targetAnimator.runtimeAnimatorController != globalAnimator)
            {
                action.targetAnimator.runtimeAnimatorController = globalAnimator;
            }

            // Play the requested animation
            action.targetAnimator.Play(
                action.animationName,
                0,
                0f
            );

            Debug.Log(
                $"Playing '{action.animationName}' on " +
                $"GameObject '{action.targetAnimator.gameObject.name}' " +
                $"for slide {slideAnimation.slideIndex}."
            );
        }
    }
}
