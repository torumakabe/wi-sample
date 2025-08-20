namespace SampleFe.Infrastructure;

public sealed class ReadinessState
{
    private volatile bool _isReady;
    public bool IsReady => _isReady;
    public void MarkReady() => _isReady = true;
}

