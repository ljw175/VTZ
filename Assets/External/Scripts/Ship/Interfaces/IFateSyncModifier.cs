public interface IFateSyncModifier
{
    float DetachThreshold { get; }
    float AttachThreshold { get; }
    float MaxFateDistance { get; }
    float SlipstreamRadius { get; }
}
