using UnityEngine;
using System.Collections.Generic;

public class SlideAnimationController : MonoBehaviour
{
    public Animator animator;
    [System.Serializable]
    public class SlideAnimation
    {
        [Header("Slide")]
        public int slideIndex;

        public string animationName;
    }

    [Header("Slide Animations")]
    public SlideAnimation[] slideAnimations;

    // Stores slides whose animation has already played
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
        if (playedSlides.Contains(currentSlideIndex))
            return;

        bool foundAnimation = false;

        foreach (SlideAnimation slideAnimation in slideAnimations)
        {
            if (slideAnimation.slideIndex == currentSlideIndex)
            {
                PlayAnimation(slideAnimation);

                Debug.Log(
                    $"Playing animation '{slideAnimation.animationName}' for slide {currentSlideIndex}."
                );

                foundAnimation = true;
            }
        }

        if (foundAnimation)
        {
            playedSlides.Add(currentSlideIndex);
        }
    }

    private void PlayAnimation(SlideAnimation slideAnimation)
    {
        if (animator == null)
            return;

        if (string.IsNullOrEmpty(slideAnimation.animationName))
            return;

        animator.Play(
            slideAnimation.animationName,
            0,
            0f
        );
    }
}