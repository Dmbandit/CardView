﻿using PanCardView.Enums;
using PanCardView.Extensions;
using PanCardView.Utility;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;
using static PanCardView.Processors.Constants;
using static System.Math;

namespace PanCardView.Processors
{
    public class BaseCarouselFrontViewProcessor : ICardProcessor
    {
        public uint AnimationLength { get; set; } = 300;

        public Easing AnimEasing { get; set; } = Easing.SinInOut;

        public double NoItemMaxPanDistance { get; set; } = 25;

        public double ScaleFactor { get; set; } = 1;

        public double OpacityFactor { get; set; } = 1;

        public double RotationFactor { get; set; } = 0;

        public double RotationXFactor { get; set; } = 0;

        public double RotationYFactor { get; set; } = 0;

        public virtual void HandleInitView(IEnumerable<View> views, CardsView cardsView, AnimationDirection animationDirection)
        {
            var view = views.FirstOrDefault();
            SetTranslationX(view, 0, cardsView, true);
        }

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

            if (Abs(xPos) > cardsView.GetSize() || (animationDirection == AnimationDirection.Prev && xPos < 0) || (animationDirection == AnimationDirection.Next && xPos > 0))
            {
                return;
            }

            if (animationDirection == AnimationDirection.Null)
            {
                xPos = Sign(xPos) * Min(Abs(xPos / 4), NoItemMaxPanDistance);
            }

            SetTranslationX(view, xPos, cardsView);
        }

        public virtual Task HandleAutoNavigate(IEnumerable<View> views, CardsView cardsView, AnimationDirection animationDirection, IEnumerable<View> inactiveViews)
        {
            var view = views.FirstOrDefault();
            if (view == null)
            {
                return Task.FromResult(false);
            }

            view.IsVisible = true;
            return new AnimationWrapper(v => SetTranslationX(view, v, cardsView), GetTranslationX(view, cardsView), 0)
                .Commit(view, nameof(HandleAutoNavigate), 16, AnimationLength, AnimEasing);
        }

        public virtual Task HandlePanReset(IEnumerable<View> views, CardsView cardsView, AnimationDirection animationDirection, IEnumerable<View> inactiveViews)
        {
            var view = views.FirstOrDefault();
            if (view == null)
            {
                return Task.FromResult(true);
            }
            var animTimePercent = 1 - (cardsView.GetSize() - Abs(GetTranslationX(view, cardsView))) / cardsView.GetSize();
            var animLength = (uint)(AnimationLength * animTimePercent) * 3 / 2;
            if (animLength == 0)
            {
                return Task.FromResult(true);
            }
            return new AnimationWrapper(v => SetTranslationX(view, v, cardsView), GetTranslationX(view, cardsView), 0)
                .Commit(view, nameof(HandlePanApply), 16, animLength, AnimEasing);
        }

        public virtual Task HandlePanApply(IEnumerable<View> views, CardsView cardsView, AnimationDirection animationDirection, IEnumerable<View> inactiveViews)
        {
            var view = views.FirstOrDefault();
            if (view == null)
            {
                return Task.FromResult(true);
            }
            var animTimePercent = 1 - (cardsView.GetSize() - Abs(GetTranslationX(view, cardsView))) / cardsView.GetSize();
            var animLength = (uint)(AnimationLength * animTimePercent);
            if (animLength == 0)
            {
                return Task.FromResult(true);
            }
            return new AnimationWrapper(v => SetTranslationX(view, v, cardsView), GetTranslationX(view, cardsView), 0)
                .Commit(view, nameof(HandlePanApply), 16, animLength, AnimEasing);
        }

        protected virtual double GetTranslationX(View view, CardsView cardsView)
        {
            if (view == null)
            {
                return 0;
            }
            var value = cardsView.IsHorizontalOrientation ? view.TranslationX : view.TranslationY;
            value += Sign(value) * cardsView.GetSize(view) * 0.5 * (1 - view.Scale);
            return value;
        }

        protected virtual void SetTranslationX(View view, double value, CardsView cardsView, bool? isVisible = null)
        {
            if (view == null)
            {
                return;
            }

            try
            {
                view.BatchBegin();
                view.Scale = CalculateFactoredProperty(value, ScaleFactor, cardsView);
                view.Opacity = CalculateFactoredProperty(value, OpacityFactor, cardsView);
                view.Rotation = CalculateFactoredProperty(value, RotationFactor, cardsView, 0) * Angle360 * Sign(-value);
                view.RotationX = CalculateFactoredProperty(value, RotationXFactor, cardsView, 0) * Angle360 * Sign(-value);
                view.RotationY = CalculateFactoredProperty(value, RotationYFactor, cardsView, 0) * Angle360 * Sign(-value);
                var translation = value - Sign(value) * cardsView.GetSize(view) * 0.5 * (1 - view.Scale);
                if (cardsView.IsHorizontalOrientation)
                {
                    view.TranslationX = translation;
                }
                else
                {
                    view.TranslationY = translation;
                }
                view.IsVisible = isVisible ?? view.IsVisible;
                cardsView.ProcessorDiff = value;
            }
            finally
            {
                view.BatchCommit();
            }
        }

        protected virtual double CalculateFactoredProperty(double value, double factor, CardsView cardsView, double defaultFactorValue = 1)
            => Abs(value) * (factor - defaultFactorValue) / cardsView.GetSize() + defaultFactorValue;
    }
}