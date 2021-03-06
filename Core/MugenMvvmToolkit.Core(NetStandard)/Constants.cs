﻿#region Copyright

// ****************************************************************************
// <copyright file="Constants.cs">
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
using System.Threading.Tasks;
using MugenMvvmToolkit.Interfaces;
using MugenMvvmToolkit.Interfaces.Callbacks;
using MugenMvvmToolkit.Interfaces.Mediators;
using MugenMvvmToolkit.Interfaces.Models;
using MugenMvvmToolkit.Interfaces.Navigation;
using MugenMvvmToolkit.Interfaces.ViewModels;
using MugenMvvmToolkit.Models;
using MugenMvvmToolkit.Models.EventArg;

// ReSharper disable once CheckNamespace

namespace MugenMvvmToolkit.DataConstants
{
    public static class ViewModelConstants
    {
        #region Fields

        internal static readonly DataConstant<string> ViewModelTypeName;
        internal static readonly DataConstant<IDataContext> ViewModelState;
        internal static readonly DataConstant<Guid> IdParent;

        public static readonly DataConstant<Guid> Id;
        public static readonly DataConstant<object> View;
        public static readonly DataConstant<WeakReference> ParentViewModel;
        public static readonly DataConstant<bool> StateNotNeeded;
        public static readonly DataConstant<EventHandler<IViewModel, ViewModelClosingEventArgs>> ClosingEvent;
        public static readonly DataConstant<EventHandler<IViewModel, ViewModelClosedEventArgs>> ClosedEvent;
        public static readonly DataConstant<IViewModel> WrapperViewModel;
        public static readonly DataConstant<Func<IViewModel, object, bool>> CanCloseHandler;
        public static readonly DataConstant<Func<INavigationDispatcher, IViewModel, IDataContext, Task<bool>>> CloseHandler;
        public static readonly DataConstant<IAsyncOperation> CurrentNavigationOperation;

        #endregion

        #region Constructors

        static ViewModelConstants()
        {
            var type = typeof(ViewModelConstants);
            Id = DataConstant.Create<Guid>(type, nameof(Id));
            IdParent = DataConstant.Create<Guid>(type, nameof(IdParent));
            View = DataConstant.Create<object>(type, nameof(View), true);
            ViewModelTypeName = DataConstant.Create<string>(type, nameof(ViewModelTypeName), true);
            ViewModelState = DataConstant.Create<IDataContext>(type, nameof(ViewModelState), true);
            ParentViewModel = DataConstant.Create<WeakReference>(type, nameof(ParentViewModel), true);
            WrapperViewModel = DataConstant.Create<IViewModel>(type, nameof(WrapperViewModel), true);
            StateNotNeeded = DataConstant.Create<bool>(type, nameof(StateNotNeeded));
            ClosingEvent = DataConstant.Create<EventHandler<IViewModel, ViewModelClosingEventArgs>>(type, nameof(ClosingEvent), false);
            ClosedEvent = DataConstant.Create<EventHandler<IViewModel, ViewModelClosedEventArgs>>(type, nameof(ClosedEvent), false);
            CanCloseHandler = DataConstant.Create<Func<IViewModel, object, bool>>(type, nameof(CanCloseHandler), false);
            CloseHandler = DataConstant.Create<Func<INavigationDispatcher, IViewModel, IDataContext, Task<bool>>>(type, nameof(CloseHandler), true);
            CurrentNavigationOperation = DataConstant.Create<IAsyncOperation>(type, nameof(CurrentNavigationOperation), true);
        }

        #endregion
    }

    public static class InitializationConstants
    {
        #region Fields

        public static readonly DataConstant<IIocContainer> IocContainer;
        public static readonly DataConstant<IViewModel> ParentViewModel;
        public static readonly DataConstant<ObservationMode> ObservationMode;
        public static readonly DataConstant<string> ViewName;
        public static readonly DataConstant<string> ViewModelBindingName;
        public static readonly DataConstant<IIocParameter[]> IocParameters;
        public static readonly DataConstant<bool> IsRestored;
        public static readonly DataConstant<Type> ViewModelType;
        public static readonly DataConstant<bool> IgnoreViewModelCache;
        public static readonly DataConstant<bool> IsDesignMode;
        public static readonly DataConstant<bool> CanDisposeView;

        #endregion

        #region Constructors

        static InitializationConstants()
        {
            var type = typeof(InitializationConstants);
            IocContainer = DataConstant.Create<IIocContainer>(type, nameof(IocContainer), true);
            ParentViewModel = DataConstant.Create<IViewModel>(type, nameof(ParentViewModel), true);
            ObservationMode = DataConstant.Create<ObservationMode>(type, nameof(ObservationMode));
            ViewModelBindingName = DataConstant.Create<string>(type, nameof(ViewModelBindingName), false);
            IocParameters = DataConstant.Create<IIocParameter[]>(type, nameof(IocParameters), true);
            IsRestored = DataConstant.Create<bool>(type, nameof(IsRestored));
            ViewName = NavigationConstants.ViewName;
            ViewModelType = DataConstant.Create<Type>(type, nameof(ViewModelType), true);
            IgnoreViewModelCache = DataConstant.Create<bool>(type, nameof(IgnoreViewModelCache));
            IsDesignMode = DataConstant.Create<bool>(type, nameof(IsDesignMode));
            CanDisposeView = DataConstant.Create<bool>(type, nameof(CanDisposeView));
        }

        #endregion
    }

    public static class ValidationConstants
    {
        #region Fields

        public static readonly DataConstant<IViewModel> ViewModel;

        #endregion

        #region Constructors

        static ValidationConstants()
        {
            var type = typeof(ViewModelConstants);
            ViewModel = DataConstant.Create<IViewModel>(type, nameof(ViewModel), true);
        }

        #endregion
    }

    public static class NavigationConstants
    {
        #region Fields

        internal static readonly DataConstant<object> IsFirstNavigation;

        public static readonly DataConstant<bool> SuppressTabNavigation;
        public static readonly DataConstant<bool> SuppressWindowNavigation;
        public static readonly DataConstant<bool> SuppressPageNavigation;
        public static readonly DataConstant<bool> SuppressRootNavigation;
        public static readonly DataConstant<IViewModel> ViewModel;
        public static readonly DataConstant<string> ViewName;
        public static readonly DataConstant<bool> IsDialog;
        public static readonly DataConstant<bool> ClearBackStack;
        public static readonly DataConstant<bool> UseAnimations;
        public static readonly DataConstant<object> CloseParameter;
        public static readonly DataConstant<bool> ImmediateClose;
        public static readonly DataConstant<bool> SuppressNavigationCallbackOnClose;
        public static readonly DataConstant<Task<bool>> NavigationCompletedTask;
        public static readonly DataConstant<object> NavigationParameter;
        public static readonly DataConstant<bool> DoNotTrackViewModelFrom;
        public static readonly DataConstant<bool> DoNotTrackViewModelTo;
        public static readonly DataConstant<bool> DoNotDisposeNoStateViewModel;
        public static readonly DataConstant<bool> IgnoreOwner;

        #endregion

        #region Constructors

        static NavigationConstants()
        {
            var type = typeof(NavigationConstants);
            SuppressPageNavigation = DataConstant.Create<bool>(type, nameof(SuppressPageNavigation));
            SuppressWindowNavigation = DataConstant.Create<bool>(type, nameof(SuppressWindowNavigation));
            SuppressTabNavigation = DataConstant.Create<bool>(type, nameof(SuppressTabNavigation));
            SuppressRootNavigation = DataConstant.Create<bool>(type, nameof(SuppressRootNavigation));
            ViewModel = DataConstant.Create<IViewModel>(type, nameof(ViewModel), true);
            ViewName = DataConstant.Create<string>(type, nameof(ViewName), false);
            IsDialog = DataConstant.Create<bool>(type, nameof(IsDialog));
            ClearBackStack = DataConstant.Create<bool>(type, nameof(ClearBackStack));
            UseAnimations = DataConstant.Create<bool>(type, nameof(UseAnimations));
            CloseParameter = DataConstant.Create<object>(type, nameof(CloseParameter), false);
            ImmediateClose = DataConstant.Create<bool>(type, nameof(ImmediateClose));
            SuppressNavigationCallbackOnClose = DataConstant.Create<bool>(type, nameof(SuppressNavigationCallbackOnClose));
            NavigationCompletedTask = DataConstant.Create<Task<bool>>(type, nameof(NavigationCompletedTask), true);
            NavigationParameter = DataConstant.Create<object>(type, nameof(NavigationParameter), true);
            IsFirstNavigation = DataConstant.Create<object>(type, nameof(IsFirstNavigation), false);
            DoNotTrackViewModelFrom = DataConstant.Create<bool>(type, nameof(DoNotTrackViewModelFrom));
            DoNotTrackViewModelTo = DataConstant.Create<bool>(type, nameof(DoNotTrackViewModelTo));
            DoNotDisposeNoStateViewModel = DataConstant.Create<bool>(type, nameof(DoNotDisposeNoStateViewModel));
            IgnoreOwner = DataConstant.Create<bool>(type, nameof(IgnoreOwner));
        }

        #endregion
    }

    public static class OpeartionCallbackConstants
    {
        #region Fields

        public static readonly DataConstant<bool> ContinueOnCapturedContext;

        #endregion

        #region Constructors

        static OpeartionCallbackConstants()
        {
            var type = typeof(OpeartionCallbackConstants);
            ContinueOnCapturedContext = DataConstant.Create<bool>(type, nameof(ContinueOnCapturedContext));
        }

        #endregion
    }

    public static class WindowPresenterConstants
    {
        #region Fields

        public static readonly DataConstant<IWindowViewMediator> WindowViewMediator;
        public static readonly DataConstant<object> RestoredView;
        public static readonly DataConstant<bool> IsViewOpened;

        #endregion

        #region Constructors

        static WindowPresenterConstants()
        {
            var type = typeof(WindowPresenterConstants);
            WindowViewMediator = DataConstant.Create<IWindowViewMediator>(type, nameof(WindowViewMediator), true);
            RestoredView = DataConstant.Create<object>(type, nameof(RestoredView), true);
            IsViewOpened = DataConstant.Create<bool>(type, nameof(IsViewOpened));
        }

        #endregion
    }
}