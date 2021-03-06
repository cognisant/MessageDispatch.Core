﻿// <copyright file="IConsume.cs" company="Corsham Science">
// Copyright (c) Corsham Science. All rights reserved.
// </copyright>

namespace CorshamScience.MessageDispatch.Core
{
    /// <summary>
    /// A common interface for implementing a consumer which takes in and handles messages.
    /// </summary>
    /// <typeparam name="TMessage">Represents the type of messages the consumer can handle.</typeparam>
    public interface IConsume<in TMessage>
    {
        /// <summary>
        /// The method for processing messages.
        /// </summary>
        /// <param name="message">A message which should be handled.</param>
        void Handle(TMessage message);
    }
}
