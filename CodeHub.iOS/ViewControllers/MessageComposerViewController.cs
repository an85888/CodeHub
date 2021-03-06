using System;
using CoreGraphics;
using Foundation;
using UIKit;
using ReactiveUI;
using System.Reactive.Linq;
using CodeHub.Core.ViewModels;
using CodeHub.iOS.ViewComponents;

namespace CodeHub.iOS.ViewControllers
{
    public abstract class MessageComposerViewController<TViewModel> : MessageComposerViewController, IViewFor<TViewModel> where TViewModel : class
    {
        private TViewModel _viewModel;
        public TViewModel ViewModel
        {
            get { return _viewModel; }
            set { this.RaiseAndSetIfChanged(ref _viewModel, value); }
        }

        object IViewFor.ViewModel
        {
            get { return _viewModel; }
            set { ViewModel = (TViewModel)value; }
        }

        protected MessageComposerViewController()
        {
            this.WhenAnyValue(x => x.ViewModel)
                .OfType<ILoadableViewModel>()
                .Subscribe(x => x.LoadCommand.ExecuteIfCan());

            this.WhenAnyValue(x => x.ViewModel)
                .OfType<IProvidesTitle>()
                .Select(x => x.WhenAnyValue(y => y.Title))
                .Switch().Subscribe(x => Title = x ?? string.Empty);

            this.WhenActivated(d => { });
        }
    }

    public class MessageComposerViewController : ReactiveViewController
    {
        private CGRect _keyboardBounds = CGRect.Empty;

        public ExtendedUITextView TextView { get; private set; }

        public MessageComposerViewController()
        {
            EdgesForExtendedLayout = UIRectEdge.None;
            TextView = new ExtendedUITextView();
            TextView.Font = UIFont.PreferredBody;

            // Work around an Apple bug in the UITextView that crashes
            if (ObjCRuntime.Runtime.Arch == ObjCRuntime.Arch.SIMULATOR)
                TextView.AutocorrectionType = UITextAutocorrectionType.No;

            NavigationItem.BackBarButtonItem = new UIBarButtonItem { Title = string.Empty };
        }

        public string Text
        {
            get { return TextView.Text; }
            set
            {
                if (string.Equals(Text, value))
                    return;

                TextView.Text = value;
                TextView.SelectedRange = new NSRange(0, 0);
            }
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            View.BackgroundColor = UIColor.White;
            Add(TextView);
        }

        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews();
            ResizeTextView();
        }

        void KeyboardWillShow (NSNotification notification)
        {
            var nsValue = notification.UserInfo.ObjectForKey (UIKeyboard.BoundsUserInfoKey) as NSValue;
            if (nsValue == null) return;
            _keyboardBounds = nsValue.RectangleFValue;
            UIView.Animate(0.25f, 0, UIViewAnimationOptions.BeginFromCurrentState | UIViewAnimationOptions.CurveEaseIn, ResizeTextView, null);
        }

        void KeyboardWillHide (NSNotification notification)
        {
            _keyboardBounds = CGRect.Empty;
            UIView.Animate(0.2, 0, UIViewAnimationOptions.BeginFromCurrentState | UIViewAnimationOptions.CurveEaseIn, ResizeTextView, null);
        }

        private void ResizeTextView()
        {
            TextView.Frame = new CGRect(0, 0, View.Bounds.Width, View.Bounds.Height - _keyboardBounds.Height);
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
            NSNotificationCenter.DefaultCenter.AddObserver(new NSString("UIKeyboardWillShowNotification"), KeyboardWillShow);
            NSNotificationCenter.DefaultCenter.AddObserver (new NSString("UIKeyboardWillHideNotification"), KeyboardWillHide);
            ResizeTextView();
        }

        public override void ViewWillDisappear(bool animated)
        {
            base.ViewWillDisappear(animated);
            NSNotificationCenter.DefaultCenter.RemoveObserver(this);
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);

            TextView.BecomeFirstResponder();
        }

        private static float CalculateHeight(UIInterfaceOrientation orientation)
        {
            if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone)
                return 44;
            if (orientation == UIInterfaceOrientation.Portrait || orientation == UIInterfaceOrientation.PortraitUpsideDown)
                return 64;
            return 88f;
        }

        public override void WillRotate(UIInterfaceOrientation toInterfaceOrientation, double duration)
        {
            base.WillRotate(toInterfaceOrientation, duration);

            if (TextView.InputAccessoryView != null)
            {
                UIView.Animate(duration, 0, UIViewAnimationOptions.BeginFromCurrentState, () =>
                {
                    var frame = TextView.InputAccessoryView.Frame;
                    frame.Height = CalculateHeight(toInterfaceOrientation);
                    TextView.InputAccessoryView.Frame = frame;
                }, null);
            }
        }
    }
}
