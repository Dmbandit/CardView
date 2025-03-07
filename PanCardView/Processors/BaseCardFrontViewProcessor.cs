﻿using PanCardView.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;
using static PanCardView.Processors.Constants;
using static System.Math;
using PanCardView.Utility;
using PanCardView.Extensions;

namespace PanCardView.Processors
{
    public class BaseCardFrontViewProcessor : ICardProcessor
    {
        public uint ResetAnimationLength { get; set; } = 150;

        public uint AutoNavigateAnimationLength { get; set; } = 150;

        public Easing ResetEasing { get; set; } = Easing.CubicInOut;

        public Easing AutoNavigateEasing { get; set; } = Easing.Linear;

        public double NoItemMaxPanDistance { get; set; } = 25;

        public virtual void HandleInitView(IEnumerable<View> views, CardsView cardsView, AnimationDirection animationDirection)
        => ResetInitialState(views.FirstOrDefault(), cardsView);

        public virtual void HandlePanChanged(IEnumerable<View> views, CardsView cardsView, double xPos, AnimationDirection animationDirection, IEnumerable<View> inactiveViews)
        {
            var view = views.FirstOrDefault();
            var inactiveView = inactiveViews.FirstOrDefault();
            if (view != null)
            {
                view.IsVisible = true;
            }
            if (inactiveView != null && inactiveView != view)
            {
                inactiveView.IsVisible = false;
            }

            var multiplier = 1;
            if (animationDirection == AnimationDirection.Null)
            {
                xPos = Sign(xPos) * Min(Abs(xPos / 4), NoItemMaxPanDistance);
                multiplier = -multiplier;
            }

            if (view != null)
            {
                SetTranslationX(view, xPos, cardsView);
                view.TranslationY = multiplier * Abs(xPos) / 10;
                view.Rotation = multiplier * 0.3 * Rad * (xPos / cardsView.GetSize());
            }
        }

        public virtual Task HandleAutoNavigate(IEnumerable<View> views, CardsView cardsView, AnimationDirection animationDirection, IEnumerable<View> inactiveViews)
        {
            var view = views.FirstOrDefault();
            if (view == null)
            {
                return Task.FromResult(false);
            }

            view.IsVisible = true;
            return new AnimationWrapper(v => view.Scale = v, view.Scale, 1)
                .Commit(view, nameof(HandleAutoNavigate), 16, AutoNavigateAnimationLength, AutoNavigateEasing);
        }

        public virtual async Task HandlePanReset(IEnumerable<View> views, CardsView cardsView, AnimationDirection animationDirection, IEnumerable<View> inactiveViews)
        {
            var view = views.FirstOrDefault();
            var tcs = new TaskCompletionSource<bool>();

            if (!CheckIsInitialPosition(view))
            {
                var animLength = (uint)(ResetAnimationLength * Min(Abs(view.TranslationX / cardsView.RealMoveDistance), 1.0));

                if (animLength == 0)
                {
                    ResetInitialState(view, cardsView);
                    return;
                }

                await new AnimationWrapper {
                    { 0, 1, new AnimationWrapper (v => SetTranslationX(view, v, cardsView), view.TranslationX, 0) },
                    { 0, 1, new AnimationWrapper (v => view.TranslationY = v, view.TranslationY, 0) },
                    { 0, 1, new AnimationWrapper (v => view.Rotation = v, view.Rotation, 0) }
                }.Commit(view, nameof(HandlePanReset), 16, animLength, ResetEasing);
            }

            ResetInitialState(view, cardsView);
        }

        public virtual Task HandlePanApply(IEnumerable<View> views, CardsView cardsView, AnimationDirection animationDirection, IEnumerable<View> inactiveViews)
        {
            var view = views.FirstOrDefault();
            if (view != null)
            {
                view.Scale = 1;
            }
            return Task.FromResult(true);
        }

        protected virtual void ResetInitialState(View view, CardsView cardsView, bool isVisible = true)
        {
            if (view != null)
            {
                view.BatchBegin();
                view.Scale = 1;
                view.Opacity = 1;
                SetTranslationX(view, 0, cardsView, isVisible);
                view.Rotation = 0;
                view.TranslationY = 0;
                view.BatchCommit();
            }
        }

        private bool CheckIsInitialPosition(View view)
        => (int)view.TranslationX == 0 && (int)view.TranslationY == 0 && (int)view.Rotation == 0;

        protected virtual void SetTranslationX(View view, double value, CardsView cardsView, bool? isVisible = null)
        {
            view.TranslationX = value;
            view.IsVisible = isVisible ?? view.IsVisible;
            cardsView.ProcessorDiff = value;
        }
    }
}