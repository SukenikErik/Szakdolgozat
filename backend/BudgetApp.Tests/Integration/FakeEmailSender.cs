using System.Collections.Concurrent;
using BudgetApp.Api.Services.Email;

namespace BudgetApp.Tests.Integration;

/// <summary>Collects the emails instead of sending them. Addresses starting with "undeliverable" fail.</summary>
public class FakeEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<EmailMessage> _sent = new();

    public Task SendAsync(EmailMessage message)
    {
        if (message.To.StartsWith("undeliverable"))
        {
            throw new InvalidOperationException("Simulated delivery error.");
        }

        _sent.Enqueue(message);
        return Task.CompletedTask;
    }

    public List<EmailMessage> SentTo(string email) => _sent.Where(m => m.To == email).ToList();
}
