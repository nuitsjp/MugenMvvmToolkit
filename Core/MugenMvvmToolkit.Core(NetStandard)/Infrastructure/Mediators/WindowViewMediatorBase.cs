﻿#region Copyright

// ****************************************************************************
// <copyright file="WindowViewMediatorBase.cs">
// Copyright (c) 2012-2017 Vyacheslav Volkov
// </copyright>
// ****************************************************************************
// <author>Vyacheslav Volkov</author>
// <email>vvs0205@outlook.com</email>
// <project>MugenMvvmToolkit</project>
// <web>https://github.com/MugenMvvmToolkit/MugenMvvmToolkit</web>
// <license>
// See license.txt in this solution or http://opensource.org/licenses/MS-PL
// </license>
// ****************************************************************************

#endregion

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MugenMvvmToolkit.DataConstants;
using MugenMvvmToolkit.Interfaces;
using MugenMvvmToolkit.Interfaces.Mediators;
using MugenMvvmToolkit.Interfaces.Models;
using MugenMvvmToolkit.Interfaces.Navigation;
using MugenMvvmToolkit.Interfaces.ViewModels;
using MugenMvvmToolkit.Models;
using MugenMvvmToolkit.Models.Messages;

namespace MugenMvvmToolkit.Infrastructure.Mediators
{
    public abstract class WindowViewMediatorBase<TView> : IWindowViewMediator, IHandler<ForegroundNavigationMessage>, IHandler<BackgroundNavigationMessage>
        where TView : class
    {
        #region Fields

        private readonly IThreadManager _threadManager;
        private readonly IViewManager _viewManager;
        private readonly IWrapperManager _wrapperManager;
        private readonly INavigationDispatcher _navigationDispatcher;
        private readonly IEventAggregator _eventAggregator;
        private IViewModel _viewModel;
        private CancelEventArgs _cancelArgs;
        private IDataContext _closeParameter;
        private bool _isOpen;
        private bool _shouldClose;
        private TaskCompletionSource<bool> _closingTcs;

        #endregion

        #region Constructors

        protected WindowViewMediatorBase([NotNull] IThreadManager threadManager, [NotNull] IViewManager viewManager, [NotNull] IWrapperManager wrapperManager, [NotNull] INavigationDispatcher navigationDispatcher,
            [NotNull] IEventAggregator eventAggregator)
        {
            Should.NotBeNull(threadManager, nameof(threadManager));
            Should.NotBeNull(viewManager, nameof(viewManager));
            Should.NotBeNull(wrapperManager, nameof(wrapperManager));
            Should.NotBeNull(navigationDispatcher, nameof(navigationDispatcher));
            Should.NotBeNull(eventAggregator, nameof(eventAggregator));
            _threadManager = threadManager;
            _viewManager = viewManager;
            _wrapperManager = wrapperManager;
            _navigationDispatcher = navigationDispatcher;
            _eventAggregator = eventAggregator;
            _eventAggregator.Subscribe(this);
        }

        #endregion

        #region Properties

        public TView View { get; private set; }

        protected bool IsClosing => _cancelArgs != null;

        protected IViewManager ViewManager => _viewManager;

        protected IThreadManager ThreadManager => _threadManager;

        protected IWrapperManager WrapperManager => _wrapperManager;

        protected INavigationDispatcher NavigationDispatcher => _navigationDispatcher;

        protected IEventAggregator EventAggregator => _eventAggregator;

        #endregion

        #region Implementation of IWindowViewMediator

        public bool IsOpen => _isOpen;

        object IWindowViewMediator.View => View;

        public virtual IViewModel ViewModel => _viewModel;

        public void Initialize(IViewModel viewModel, IDataContext context)
        {
            Should.NotBeNull(viewModel, nameof(viewModel));
            if (ReferenceEquals(_viewModel, viewModel))
                return;
            if (_viewModel != null)
                throw ExceptionManager.ObjectInitialized(GetType().Name, this);
            _viewModel = viewModel;
            OnInitialized(viewModel, context);
        }

        public Task<bool> ShowAsync(IDataContext context)
        {
            ViewModel.NotBeDisposed();
            if (context == null)
                context = DataContext.Empty;
            if (IsOpen)
            {
                if (ActivateView(View, context))
                {
                    NavigationDispatcher.OnNavigated(CreateOpenContext(context, NavigationMode.Refresh));
                    return Empty.TrueTask;
                }
                return Empty.FalseTask;
            }
            var tcs = new TaskCompletionSource<bool>();
            OnNavigatingTo(context)
                .TryExecuteSynchronously(task =>
                {
                    try
                    {
                        if (!task.Result)
                        {
                            tcs.TrySetCanceled();
                            NavigationDispatcher.OnNavigationCanceled(CreateOpenContext(context, NavigationMode.New));
                            return;
                        }

                        _isOpen = true;
                        ShowInternal(context, tcs);
                    }
                    catch (Exception e)
                    {
                        tcs.TrySetException(e);
                        NavigationDispatcher.OnNavigationFailed(CreateOpenContext(context, NavigationMode.New), e);
                    }
                });
            return tcs.Task;
        }

        public Task<bool> CloseAsync(IDataContext context)
        {
            if (!IsOpen)
            {
                Tracer.Error(ExceptionManager.WindowClosedString);
                return Empty.TrueTask;
            }
            var closingTask = _closingTcs?.Task;
            if (closingTask != null)
                return closingTask;

            _closingTcs = new TaskCompletionSource<bool>();
            closingTask = _closingTcs.Task;
            _closeParameter = context;
            OnClosing(context)
                .TryExecuteSynchronously(task =>
                {
                    try
                    {
                        if (task.Result)
                            CloseViewImmediate();
                        else
                        {
                            _closingTcs?.TrySetResult(false);
                            _closingTcs = null;
                        }
                    }
                    catch (Exception e)
                    {
                        _closingTcs?.TrySetException(e);
                        _closingTcs = null;
                        NavigationDispatcher.OnNavigationFailed(CreateCloseContext(context), e);
                    }
                });
            return closingTask;
        }

        public void UpdateView(object view, bool isOpen, IDataContext context)
        {
            if (ReferenceEquals(View, view))
                return;
            var oldView = View;
            if (view != null)
            {
                view = WrapperManager.Wrap(view, typeof(TView), context);
                if (ReferenceEquals(oldView, view))
                    return;
            }
            _isOpen = isOpen;
            if (oldView != null)
                CleanupView(oldView);
            View = (TView)view;
            if (View != null)
                InitializeView(View, context);
            OnViewUpdated(View, context);
            if (oldView == null && view != null && isOpen)
                ThreadManager.Invoke(ExecutionMode.AsynchronousOnUiThread, this, context,
                        (@base, dataContext) => @base.NavigationDispatcher.OnNavigated(@base.CreateOpenContext(dataContext, NavigationMode.Refresh)), OperationPriority.Low);
        }

        void IHandler<ForegroundNavigationMessage>.Handle(object sender, ForegroundNavigationMessage message)
        {
            if (IsOpen && _viewModel != null && !ViewModel.IsDisposed)
                NavigationDispatcher.OnNavigated(new NavigationContext(NavigationType.Page, NavigationMode.Foreground, null, ViewModel, this, message.Context));
        }

        void IHandler<BackgroundNavigationMessage>.Handle(object sender, BackgroundNavigationMessage message)
        {
            if (IsOpen && _viewModel != null && !ViewModel.IsDisposed)
                NavigationDispatcher.OnNavigated(new NavigationContext(NavigationType.Page, NavigationMode.Background, ViewModel, null, this, message.Context));
        }

        #endregion

        #region Methods

        protected abstract void ShowView([NotNull] TView view, bool isDialog, IDataContext context);

        protected abstract bool ActivateView([NotNull] TView view, IDataContext context);

        protected abstract void InitializeView([NotNull] TView view, IDataContext context);

        protected abstract void CleanupView([NotNull] TView view);

        protected abstract void CloseView([NotNull] TView view);

        protected virtual void OnShown([NotNull] IDataContext context)
        {
            NavigationDispatcher.OnNavigated(CreateOpenContext(context, NavigationMode.New));
        }

        protected virtual Task<bool> OnClosing(IDataContext context)
        {
            return NavigationDispatcher.OnNavigatingAsync(CreateCloseContext(context));
        }

        protected virtual void OnClosed(INavigationContext context)
        {
            NavigationDispatcher.OnNavigated(context);
        }

        protected virtual void OnViewUpdated(TView view, IDataContext context)
        {
        }

        protected virtual void OnInitialized(IViewModel viewModel, IDataContext context)
        {
        }

        protected virtual INavigationContext CreateCloseContext(IDataContext context)
        {
            bool doNotTrackNavigation;
            var viewModelTo = NavigationDispatcher.GetPreviousOpenedViewModelOrParent(ViewModel, NavigationType.Window, out doNotTrackNavigation);
            return new NavigationContext(NavigationType.Window, NavigationMode.Back, ViewModel, viewModelTo, this, context)
            {
                {NavigationConstants.DoNotTrackViewModelTo, doNotTrackNavigation}
            };
        }

        protected virtual NavigationContext CreateOpenContext(IDataContext context, NavigationMode mode)
        {
            bool doNotTrackNavigation;
            var viewModelFrom = NavigationDispatcher.GetPreviousOpenedViewModelOrParent(ViewModel, NavigationType.Window, out doNotTrackNavigation);
            return new NavigationContext(NavigationType.Window, mode, viewModelFrom, ViewModel, this, context)
            {
                {NavigationConstants.DoNotTrackViewModelFrom, doNotTrackNavigation}
            };
        }

        protected virtual Task<bool> OnNavigatingTo(IDataContext context)
        {
            bool doNotTrackNavigation;
            var viewModelFrom = NavigationDispatcher.GetPreviousOpenedViewModelOrParent(ViewModel, NavigationType.Window, out doNotTrackNavigation);
            if (viewModelFrom == null)
                return Empty.TrueTask;
            var ctx = new NavigationContext(NavigationType.Window, NavigationMode.New, viewModelFrom, ViewModel, this, context)
            {
                {NavigationConstants.DoNotTrackViewModelFrom, doNotTrackNavigation}
            };
            return NavigationDispatcher.OnNavigatingAsync(ctx);
        }

        protected void OnViewClosing(object sender, CancelEventArgs e)
        {
            try
            {
                _cancelArgs = e;
                if (_shouldClose)
                {
                    e.Cancel = false;
                    CompleteCloseAsync();
                    return;
                }
                e.Cancel = true;
                CloseAsync(null);
            }
            finally
            {
                _cancelArgs = null;
            }
        }

        protected void OnViewClosed(object sender, EventArgs e)
        {
            if (!IsClosing)
                CompleteClose();
        }

        private void ShowInternal(IDataContext context, TaskCompletionSource<bool> tcs)
        {
            ViewManager
                .GetViewAsync(ViewModel, context)
                .TryExecuteSynchronously(task =>
                {
                    View = (TView)WrapperManager.Wrap(task.Result, typeof(TView), context);
                    ViewManager.InitializeViewAsync(ViewModel, task.Result, context);
                    InitializeView(View, context);

                    bool isDialog;
                    if (!context.TryGetData(NavigationConstants.IsDialog, out isDialog))
                        isDialog = true;
                    //NOTE to call method OnShown after ShowView.
                    ThreadManager.Invoke(ExecutionMode.AsynchronousOnUiThread, this, context, tcs,
                        (@base, dataContext, cs) =>
                        {
                            @base.OnShown(dataContext);
                            cs.TrySetResult(true);
                        }, OperationPriority.Low);
                    ThreadManager.Invoke(ExecutionMode.AsynchronousOnUiThread, this, isDialog, context, (@base, b, arg3) =>
                    {
                        try
                        {
                            @base.ShowView(@base.View, b, arg3);
                        }
                        catch (Exception e)
                        {
                            @base.NavigationDispatcher.OnNavigationFailed(@base.CreateOpenContext(arg3, NavigationMode.New), e);
                            throw;
                        }
                    }, OperationPriority.High);
                }, ViewModel.DisposeCancellationToken);
        }

        private void CloseViewImmediate()
        {
            if (ThreadManager.IsUiThread)
                CloseViewImmediateInternal();
            else
                ThreadManager.InvokeOnUiThreadAsync(CloseViewImmediateInternal);
        }

        private void CloseViewImmediateInternal()
        {
            if (_cancelArgs != null)
            {
                _cancelArgs.Cancel = false;
                CompleteCloseAsync();
            }
            else if (View != null)
            {
                _shouldClose = true;
                CloseView(View);
            }
        }

        private void CompleteCloseAsync()
        {
            //NOTE to minimize the time of closing the window.
            ThreadManager.InvokeAsync(CompleteClose);
        }

        private void CompleteClose()
        {
            INavigationContext context = CreateCloseContext(_closeParameter);
            OnClosed(context);

            _closingTcs?.TrySetResult(true);
            _closingTcs = null;
            _closeParameter = null;
            _shouldClose = false;
            _isOpen = false;
            TView view = View;
            if (view == null)
                return;
            ThreadManager.Invoke(ExecutionMode.AsynchronousOnUiThread, this, view, context, (@base, v, ctx) =>
            {
                @base.CleanupView(v);
                @base.ViewManager.CleanupViewAsync(@base.ViewModel, ctx);
            });
            View = null;
        }

        #endregion
    }
}
