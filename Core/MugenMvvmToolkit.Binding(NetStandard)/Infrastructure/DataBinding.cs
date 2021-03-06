﻿#region Copyright

// ****************************************************************************
// <copyright file="DataBinding.cs">
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;
using MugenMvvmToolkit.Binding.DataConstants;
using MugenMvvmToolkit.Binding.Interfaces;
using MugenMvvmToolkit.Binding.Interfaces.Accessors;
using MugenMvvmToolkit.Binding.Models;
using MugenMvvmToolkit.Binding.Models.EventArg;
using MugenMvvmToolkit.Interfaces.Models;
using MugenMvvmToolkit.Models;

namespace MugenMvvmToolkit.Binding.Infrastructure
{
    public class DataBinding : IDataBinding, IDataContext, ICollection<IBindingBehavior>
    {
        #region Fields

        internal bool IsAssociated;
        private readonly IBindingSourceAccessor _sourceAccessor;
        private readonly ISingleBindingSourceAccessor _targetAccessor;
        private IDataContext _lazyContext;
        private bool _isSourceUpdating;
        private bool _isTargetUpdating;
        private IBindingBehavior[] _items;
        private byte _size;

        #endregion

        #region Constructors

        public DataBinding([NotNull] ISingleBindingSourceAccessor target, [NotNull] IBindingSourceAccessor source)
        {
            Should.NotBeNull(target, nameof(target));
            Should.NotBeNull(source, nameof(source));
            _targetAccessor = target;
            _sourceAccessor = source;
            _items = Empty.Array<IBindingBehavior>();
        }

        #endregion

        #region Implementation of IDataBinding

        public IDataContext Context => this;

        public ISingleBindingSourceAccessor TargetAccessor => _targetAccessor;

        public IBindingSourceAccessor SourceAccessor => _sourceAccessor;

        public ICollection<IBindingBehavior> Behaviors => this;

        public bool IsDisposed => ReferenceEquals(DataContext.Empty, _lazyContext);

        public virtual bool UpdateSource()
        {
            //ignoring the concurrent access, there is no need to use Interlocked or lock
            if (_isSourceUpdating)
                return false;
            var isDebuggable = _targetAccessor.Source.Path.IsDebuggable;
            try
            {
                _isSourceUpdating = true;
                if (isDebuggable)
                    DebugInfo("Binding update source");
                if (_sourceAccessor.SetValue(_targetAccessor, this, true))
                {
                    RaiseBindingUpdated(BindingEventArgs.SourceTrueArgs);
                    return true;
                }
                RaiseBindingUpdated(BindingEventArgs.SourceFalseArgs);
            }
            catch (Exception exception)
            {
                RaiseBindingException(
                    BindingExceptionManager.WrapBindingException(this, BindingAction.UpdateSource, exception),
                    exception, BindingAction.UpdateSource);
            }
            finally
            {
                if (isDebuggable)
                    DebugInfo("Binding end update source");
                _isSourceUpdating = false;
            }
            return false;
        }

        public virtual bool UpdateTarget()
        {
            //ignoring the concurrent access, there is no need to use Interlocked or lock
            if (_isTargetUpdating)
                return false;
            var isDebuggable = _targetAccessor.Source.Path.IsDebuggable;
            try
            {
                _isTargetUpdating = true;
                if (isDebuggable)
                    DebugInfo("Binding update target");
                if (_targetAccessor.SetValue(_sourceAccessor, this, true))
                {
                    RaiseBindingUpdated(BindingEventArgs.TargetTrueArgs);
                    return true;
                }
                RaiseBindingUpdated(BindingEventArgs.TargetFalseArgs);
            }
            catch (Exception exception)
            {
                RaiseBindingException(
                    BindingExceptionManager.WrapBindingException(this, BindingAction.UpdateTarget, exception), exception,
                    BindingAction.UpdateTarget);
            }
            finally
            {
                if (isDebuggable)
                    DebugInfo("Binding end update target");
                _isTargetUpdating = false;
            }
            return false;
        }

        public virtual bool Validate()
        {
            var action = BindingAction.UpdateTarget;
            try
            {
                bool isValid = _targetAccessor.Source.Validate(true);
                action = BindingAction.UpdateSource;

                var singleSourceAccessor = _sourceAccessor as ISingleBindingSourceAccessor;
                if (singleSourceAccessor != null)
                {
                    if (isValid && !singleSourceAccessor.Source.Validate(true))
                        isValid = false;
                }
                else
                {
                    for (int index = 0; index < _sourceAccessor.Sources.Count; index++)
                    {
                        if (isValid && !_sourceAccessor.Sources[index].Validate(true))
                            isValid = false;
                    }
                }
                return isValid;
            }
            catch (Exception exception)
            {
                RaiseBindingException(
                    BindingExceptionManager.WrapBindingException(this, action, exception), exception,
                    BindingAction.UpdateTarget);
                return false;
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _lazyContext, DataContext.Empty) == DataContext.Empty)
                return;
            OnDispose();
            BindingServiceProvider.BindingManager.Unregister(this);
            BindingUpdated = null;
            ((ICollection<IBindingBehavior>)this).Clear();
            _sourceAccessor.Dispose();
            _targetAccessor.Dispose();
            if (_targetAccessor.Source.Path.IsDebuggable)
                DebugInfo("Binding disposed");
        }

        public event EventHandler<IDataBinding, BindingEventArgs> BindingUpdated;

        #endregion

        #region Methods

        protected virtual void OnBehaviorAdded([NotNull] IBindingBehavior behavior)
        {
        }

        protected virtual void OnBehaviorRemoved([NotNull] IBindingBehavior behavior)
        {
        }


        protected virtual void OnDispose()
        {
        }

        protected void RaiseBindingException(Exception exception, Exception originalException, BindingAction action)
        {
            BindingEventArgs args = null;
            var handler = BindingUpdated;
            if (handler != null)
            {
                args = new BindingEventArgs(action, exception, originalException);
                handler(this, args);
            }
            if (BindingServiceProvider.BindingExceptionHandler != null)
                BindingServiceProvider.RaiseBindingException(this, args ?? new BindingEventArgs(action, exception, originalException));
        }

        protected void RaiseBindingUpdated(BindingEventArgs args)
        {
            BindingUpdated?.Invoke(this, args);
        }

        private void InitializeContext()
        {
            if (_lazyContext == null)
            {
                Interlocked.CompareExchange(ref _lazyContext, new DataContext(), null);
                _lazyContext.AddOrUpdate(BindingConstants.Binding, this);
            }
        }

        private void CheckBehavior(IBindingBehavior newBehavior)
        {
            Should.NotBeNull(newBehavior, nameof(newBehavior));
            if (_size == 0)
                return;
            for (int index = 0; index < _size; index++)
            {
                if (_items[index].Id == newBehavior.Id)
                    throw BindingExceptionManager.DuplicateBehavior(_items[index], newBehavior);
            }
        }

        private void EnsureCapacity(int min)
        {
            if (_items.Length >= min)
                return;
            var value = _items.Length == 0 ? 2 : _items.Length + 1;
            var objArray = new IBindingBehavior[value];
            if (_size > 0)
                Array.Copy(_items, 0, objArray, 0, _size);
            _items = objArray;
        }

        private int IndexOf(IBindingBehavior item)
        {
            return Array.IndexOf(_items, item, 0, _size);
        }

        private IEnumerator<IBindingBehavior> GetBehaviorEnumerator()
        {
            return _items
                .Take(_size)
                .GetEnumerator();
        }

        private void DebugInfo(string message, object[] args = null)
        {
            BindingServiceProvider.DebugBinding(this, TargetAccessor.Source.Path.DebugTag, message, args);
        }

        #endregion

        #region Implementation of IDataContext

        int IDataContext.Count
        {
            get
            {
                if (_lazyContext == null)
                    return 1;
                return _lazyContext.Count;
            }
        }

        bool IDataContext.IsReadOnly => false;

        void IDataContext.Add<T>(DataConstant<T> dataConstant, T value)
        {
            InitializeContext();
            _lazyContext.Add(dataConstant, value);
        }

        void IDataContext.AddOrUpdate<T>(DataConstant<T> dataConstant, T value)
        {
            InitializeContext();
            _lazyContext.AddOrUpdate(dataConstant, value);
        }

        T IDataContext.GetData<T>(DataConstant<T> dataConstant)
        {
            if (_lazyContext == null)
            {
                if (BindingConstants.Binding.Equals(dataConstant))
                    return (T)(object)this;
                return default(T);
            }
            return _lazyContext.GetData(dataConstant);
        }

        bool IDataContext.TryGetData<T>(DataConstant<T> dataConstant, out T data)
        {
            if (_lazyContext == null)
            {
                if (BindingConstants.Binding.Equals(dataConstant))
                {
                    data = (T)(object)this;
                    return true;
                }
                data = default(T);
                return false;
            }
            return _lazyContext.TryGetData(dataConstant, out data);
        }

        bool IDataContext.Contains(DataConstant dataConstant)
        {
            if (_lazyContext == null)
                return BindingConstants.Binding.Constant.Equals(dataConstant);
            return _lazyContext.Contains(dataConstant);
        }

        bool IDataContext.Remove(DataConstant dataConstant)
        {
            if (_lazyContext == null)
                return false;
            return _lazyContext.Remove(dataConstant);
        }

        void IDataContext.Merge(IDataContext context)
        {
            InitializeContext();
            _lazyContext.Merge(context);
        }

        void IDataContext.Clear()
        {
            _lazyContext?.Clear();
        }

        IList<DataConstantValue> IDataContext.ToList()
        {
            if (_lazyContext == null || _lazyContext == DataContext.Empty)
                return new List<DataConstantValue> { BindingConstants.Binding.ToValue(this) };
            return _lazyContext.ToList();
        }

        #endregion

        #region Implementation of ICollection<IBindingBehavior>

        IEnumerator<IBindingBehavior> IEnumerable<IBindingBehavior>.GetEnumerator()
        {
            return GetBehaviorEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetBehaviorEnumerator();
        }

        void ICollection<IBindingBehavior>.Add(IBindingBehavior item)
        {
            if (IsDisposed)
                return;
            CheckBehavior(item);
            if (!item.Attach(this))
                return;
            if (_size == _items.Length)
                EnsureCapacity(_size + 1);
            _items[_size++] = item;
            OnBehaviorAdded(item);
        }

        void ICollection<IBindingBehavior>.Clear()
        {
            for (int i = 0; i < _size; i++)
            {
                var behavior = _items[i];
                behavior.Detach(this);
                OnBehaviorRemoved(behavior);
            }
            _size = 0;
            _items = Empty.Array<IBindingBehavior>();
        }

        bool ICollection<IBindingBehavior>.Contains(IBindingBehavior item)
        {
            Should.NotBeNull(item, nameof(item));
            return IndexOf(item) >= 0;
        }

        void ICollection<IBindingBehavior>.CopyTo(IBindingBehavior[] array, int arrayIndex)
        {
            Array.Copy(_items, 0, array, arrayIndex, _size);
        }

        bool ICollection<IBindingBehavior>.Remove(IBindingBehavior item)
        {
            Should.NotBeNull(item, nameof(item));
            int index = IndexOf(item);
            if (index < 0)
                return false;
            IBindingBehavior behavior = _items[index];
            --_size;
            if (index < _size)
                Array.Copy(_items, index + 1, _items, index, _size - index);
            _items[_size] = null;
            behavior.Detach(this);
            OnBehaviorRemoved(behavior);
            return true;
        }

        int ICollection<IBindingBehavior>.Count => _size;

        bool ICollection<IBindingBehavior>.IsReadOnly => false;

        #endregion
    }
}
