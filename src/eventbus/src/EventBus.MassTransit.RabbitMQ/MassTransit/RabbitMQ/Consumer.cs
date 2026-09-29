using Light.EventBus.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Light.MassTransit.RabbitMQ
{
    /// <summary>
    /// Base class for MassTransit consumers of an <see cref="IIntegrationEvent"/>. Logs the outcome of each message and
    /// optionally swallows handler exceptions (see <see cref="ThrowIfError"/>).
    /// </summary>
    /// <typeparam name="TMessage">The consumed integration event type.</typeparam>
    public abstract class Consumer<TMessage> : IConsumer<TMessage>
         where TMessage : class, IIntegrationEvent
    {
        private readonly ILogger _logger;

        /// <summary>
        /// When <c>true</c> (the default), an exception thrown by the handler is logged and then re-thrown, so
        /// MassTransit's fault pipeline sees it: middleware such as <c>UseMessageRetry</c> retries the message and, once
        /// retries are exhausted, it is moved to the <c>_error</c> queue.
        /// <para>
        /// When overridden to return <c>false</c>, the exception is logged and <b>swallowed</b>: the message is treated
        /// as successfully consumed and acknowledged. Retry policies, redelivery and the <c>_error</c> queue configured
        /// on the endpoint (e.g. in a <see cref="ConsumerDefinition{TMessage, TConsumer}"/>) then never apply to it, and
        /// the message is lost unless the handler persists it itself.
        /// </para>
        /// </summary>
        public virtual bool ThrowIfError => true;

        protected Consumer(ILogger logger)
        {
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<TMessage> context)
        {
            var message = context.Message;

            try
            {
                await Handle(message, context);

                _logger.LogInformation("event_bus {id} consumed", message.Id);

                _logger.LogDebug("event_bus {id} consumed data: {@Data}",
                    message.Id,
                    message);
            }
            catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
            {
                // shutdown/cancellation: always rethrow so the message is not acked without being processed
                _logger.LogInformation("event_bus {id} consume canceled", message.Id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "event_bus {id} consumed with error: {error}",
                    message.Id,
                    ex.Message);

                _logger.LogDebug("event_bus {id} failed data: {@Data}",
                    message.Id,
                    message);

                if (ThrowIfError)
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Handles the message. Called by <see cref="Handle(TMessage, ConsumeContext{TMessage})"/> unless that overload
        /// is overridden.
        /// </summary>
        public abstract Task Handle(TMessage message);

        /// <summary>
        /// Handles the message with access to the MassTransit <see cref="ConsumeContext{T}"/> (headers,
        /// <see cref="PipeContext.CancellationToken"/>, publish/send from the consume context, ...). This is the method
        /// <see cref="Consume"/> calls; by default it forwards to <see cref="Handle(TMessage)"/>.
        /// </summary>
        /// <remarks>
        /// When you override this overload, <see cref="Handle(TMessage)"/> is no longer called by the pipeline, but it
        /// remains abstract and must still be implemented (e.g. <c>=&gt; Task.CompletedTask</c>).
        /// </remarks>
        protected virtual Task Handle(TMessage message, ConsumeContext<TMessage> context)
        {
            return Handle(message);
        }
    }
}
