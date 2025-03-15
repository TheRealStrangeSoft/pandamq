using System.Text.Json.Serialization;

namespace PandaMQ.Abstractions;

[JsonDerivedType(typeof(PublishMessage), "publish")]
[JsonDerivedType(typeof(DeliverMessage), "deliver")]
[JsonDerivedType(typeof(AcknowledgeMessage), "acknowledge")]
[JsonDerivedType(typeof(SubscribeMessage), "subscribe")]
[JsonDerivedType(typeof(UnsubscribeMessage), "unsubscribe")]
[JsonDerivedType(typeof(RejectMessage), "reject")]
[JsonDerivedType(typeof(ServerGreetingMessage), "greeting")]
[JsonDerivedType(typeof(ClientHelloMessage), "hello")]
public interface IPandaMQMessage;

// Greeting and Hello will be expanded in the future to describe features, version compatibility, and more.