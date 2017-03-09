﻿using System;

namespace CR.MessageDispatch.Core
{
    public class OnExceptionSkipOrExitDispatcher<TMessage> : IDispatcher<TMessage>
    {
        private readonly IDispatcher<TMessage> _dispatcher;
        private readonly Func<TMessage, Exception, bool> _shouldExit;

        public OnExceptionSkipOrExitDispatcher(IDispatcher<TMessage> dispatcher, Func<TMessage, Exception, bool> shouldExit)
        {
            _dispatcher = dispatcher;
            _shouldExit = shouldExit;
        }

        public OnExceptionSkipOrExitDispatcher(IDispatcher<TMessage> dispatcher, bool alwaysExit)
        {
            _dispatcher = dispatcher;
            _shouldExit = (_, __) => alwaysExit;
        }

        public void Dispatch(TMessage message)
        {
            try
            {
                _dispatcher.Dispatch(message);
            }
            catch (Exception e)
            {
                if (_shouldExit(message, e))
                {
                    Environment.Exit(1);
                }
            }
        }
    }
}