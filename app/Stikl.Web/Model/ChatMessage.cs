namespace Stikl.Web.Model;

// TODO: we might want it to not be a message but an event that could also include "read" or other abstract things.
public record ChatMessage(
    int Pk,
    Username Sender,
    Username Recipient,
    DateTimeOffset Timestamp,
    string Message
);
