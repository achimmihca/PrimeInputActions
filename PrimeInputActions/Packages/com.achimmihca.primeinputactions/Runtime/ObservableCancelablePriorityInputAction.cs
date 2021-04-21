﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UniRx;
using UniRx.Triggers;

namespace PrimeInputActions
{
    /**
     * Wrapper for an InputAction.
     * Provides methods to create an Observable for the InputAction's events.
     * Thereby, subscribers are notified in the order of their priority.
     * When cancelled, then following subscribers in the queue will not be notified for this frame.
     * Furthermore, all registered events are removed when the Owner GameObject is Destroyed.
     */
    public class ObservableCancelablePriorityInputAction
    {
        public GameObject Owner { get; private set; }
        public InputAction InputAction { get; private set; }

        private int notifyCancelledInFrame;

        private List<InputActionSubscriber> performedSubscribers;
        private List<InputActionSubscriber> startedSubscribers;
        private List<InputActionSubscriber> canceledSubscribers;

        // Methods and properties that delegate to the wrapped InputAction.
        public TValue ReadValue<TValue>() where TValue : struct => InputAction.ReadValue<TValue>();
        public object ReadValueAsObject() => InputAction.ReadValueAsObject();
        public bool triggered => InputAction.triggered;
        public bool enabled => InputAction.enabled;

        public ObservableCancelablePriorityInputAction(InputAction inputAction, GameObject owner)
        {
            this.InputAction = inputAction;
            this.Owner = owner;
        }
        
        public IObservable<InputAction.CallbackContext> StartedAsObservable(int priority = 0)
        {
            void OnRemoveSubscriber(Action<InputAction.CallbackContext> onNext)
            {
                InputActionSubscriber subscriber = startedSubscribers
                    .FirstOrDefault(it => it.Priority == priority && it.OnNext == onNext);
                startedSubscribers.Remove(subscriber);

                // No subscriber left in event queue. Thus, remove the callback from the InputAction.
                if (startedSubscribers.Count == 0)
                {
                    InputAction.started -= NotifyStartedSubscribers;
                    startedSubscribers = null;
                }
            }
            
            void OnAddSubscriber(Action<InputAction.CallbackContext> onNext)
            {
                // Add the one callback that will update all subscribers
                if (startedSubscribers == null)
                {
                    startedSubscribers = new List<InputActionSubscriber>();
                    InputAction.started += NotifyStartedSubscribers;
                    if (Owner != null)
                    {
                        Owner.OnDestroyAsObservable().Subscribe(_ => InputAction.started -= NotifyStartedSubscribers);
                    }
                }

                startedSubscribers.Add(new InputActionSubscriber(priority, onNext));
                startedSubscribers.Sort(CompareByPriority);
            }

            return Observable.FromEvent<InputAction.CallbackContext>(OnAddSubscriber, OnRemoveSubscriber);
        }
        
        public IObservable<InputAction.CallbackContext> PerformedAsObservable(int priority = 0)
        {
            void OnRemoveSubscriber(Action<InputAction.CallbackContext> onNext)
            {
                InputActionSubscriber subscriber = performedSubscribers
                    .FirstOrDefault(it => it.Priority == priority && it.OnNext == onNext);
                performedSubscribers.Remove(subscriber);

                // No subscriber left in event queue. Thus, remove the callback from the InputAction.
                if (performedSubscribers.Count == 0)
                {
                    InputAction.performed -= NotifyPerformedSubscribers;
                    performedSubscribers = null;
                }
            }
            
            void OnAddSubscriber(Action<InputAction.CallbackContext> onNext)
            {
                // Add the one callback that will update all subscribers
                if (performedSubscribers == null)
                {
                    performedSubscribers = new List<InputActionSubscriber>();
                    InputAction.performed += NotifyPerformedSubscribers;
                    if (Owner != null)
                    {
                        Owner.OnDestroyAsObservable().Subscribe(_ => InputAction.performed -= NotifyPerformedSubscribers);
                    }
                }

                performedSubscribers.Add(new InputActionSubscriber(priority, onNext));
                performedSubscribers.Sort(CompareByPriority);
            }

            return Observable.FromEvent<InputAction.CallbackContext>(OnAddSubscriber, OnRemoveSubscriber);
        }

        public IObservable<InputAction.CallbackContext> CanceledAsObservable(int priority = 0)
        {
            void OnRemoveSubscriber(Action<InputAction.CallbackContext> onNext)
            {
                InputActionSubscriber subscriber = canceledSubscribers
                    .FirstOrDefault(it => it.Priority == priority && it.OnNext == onNext);
                canceledSubscribers.Remove(subscriber);

                // No subscriber left in event queue. Thus, remove the callback from the InputAction.
                if (canceledSubscribers.Count == 0)
                {
                    InputAction.canceled -= NotifyCanceledSubscribers;
                    canceledSubscribers = null;
                }
            }
            
            void OnAddSubscriber(Action<InputAction.CallbackContext> onNext)
            {
                // Add the one callback that will update all subscribers
                if (canceledSubscribers == null)
                {
                    canceledSubscribers = new List<InputActionSubscriber>();
                    InputAction.canceled += NotifyCanceledSubscribers;
                    if (Owner != null)
                    {
                        Owner.OnDestroyAsObservable().Subscribe(_ => InputAction.canceled -= NotifyCanceledSubscribers);
                    }
                }

                canceledSubscribers.Add(new InputActionSubscriber(priority, onNext));
                canceledSubscribers.Sort(CompareByPriority);
            }

            return Observable.FromEvent<InputAction.CallbackContext>(OnAddSubscriber, OnRemoveSubscriber);
        }
        
        public void CancelNotifyForThisFrame()
        {
            notifyCancelledInFrame = Time.frameCount;
        }

        private void NotifyStartedSubscribers(InputAction.CallbackContext callbackContext)
        {
            // Iteration over index to enable removing subscribers as part of the callback (i.e., during iteration).
            for (int i = 0; startedSubscribers != null && i < startedSubscribers.Count; i++)
            {
                if (notifyCancelledInFrame != Time.frameCount)
                {
                    InputActionSubscriber subscriber = startedSubscribers[i];
                    subscriber.OnNext.Invoke(callbackContext);
                }
                else
                {
                    return;
                }
            }
        }
        
        private void NotifyPerformedSubscribers(InputAction.CallbackContext callbackContext)
        {
            // Iteration over index to enable removing subscribers as part of the callback (i.e., during iteration).
            for (int i = 0; performedSubscribers != null && i < performedSubscribers.Count; i++)
            {
                if (notifyCancelledInFrame != Time.frameCount)
                {
                    InputActionSubscriber subscriber = performedSubscribers[i];
                    subscriber.OnNext.Invoke(callbackContext);
                }
                else
                {
                    return;
                }
            }
        }
        
        private void NotifyCanceledSubscribers(InputAction.CallbackContext callbackContext)
        {
            // Iteration over index to enable removing subscribers as part of the callback (i.e., during iteration).
            for (int i = 0; canceledSubscribers != null && i < canceledSubscribers.Count; i++)
            {
                if (notifyCancelledInFrame != Time.frameCount)
                {
                    InputActionSubscriber subscriber = canceledSubscribers[i];
                    subscriber.OnNext.Invoke(callbackContext);
                }
                else
                {
                    return;
                }
            }
        }
        
        private int CompareByPriority(InputActionSubscriber a, InputActionSubscriber b)
        {
            // Sort descending: compare b to a
            return b.Priority.CompareTo(a.Priority);
        }
        
        private class InputActionSubscriber
        {
            public int Priority { get; private set; }
            public Action<InputAction.CallbackContext> OnNext { get; private set; }

            public InputActionSubscriber(int priority, Action<InputAction.CallbackContext> onNext)
            {
                Priority = priority;
                OnNext = onNext;
            }
        }

        public bool HasAnySubscribers()
        {
            return HasStartedSubscribers()
                   || HasPerformedSubscribers()
                   || HasCanceledSubscribers();
        }
        
        public bool HasStartedSubscribers()
        {
            return !startedSubscribers.IsNullOrEmpty();
        }

        public bool HasPerformedSubscribers()
        {
            return !performedSubscribers.IsNullOrEmpty();
        }
        
        public bool HasCanceledSubscribers()
        {
            return !canceledSubscribers.IsNullOrEmpty();
        }
    }
}
