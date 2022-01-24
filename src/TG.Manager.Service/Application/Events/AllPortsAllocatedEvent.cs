using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TG.Manager.Service.Application.Events;

public record AllPortsAllocatedEvent(int Port) : INotification;

public class AllPortsAllocatedEventHandler : INotificationHandler<AllPortsAllocatedEvent>
{
    private const string Message = "All ports were allocated. attempt to allocate port {0} out of range";
    private readonly ILogger<AllPortsAllocatedEventHandler> _logger;

    public AllPortsAllocatedEventHandler(ILogger<AllPortsAllocatedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(AllPortsAllocatedEvent notification, CancellationToken cancellationToken)
    {
        var message = string.Format(Message, notification.Port);
        _logger.LogCritical(message);
        throw new ApplicationException(message);
    }
}