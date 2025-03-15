namespace PandaMQ.Abstractions;

public enum DeliveryOption
{
    BestEffort,
    AtLeastOnce,
    AtMostOnce,
    ExactlyOnce
}