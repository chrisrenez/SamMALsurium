using System.Threading.Channels;

namespace SamMALsurium.Services;

public class ImageProcessingQueueService
{
    private readonly Channel<int> _queue;

    public ImageProcessingQueueService()
    {
        // Create an unbounded channel for image processing queue
        var options = new UnboundedChannelOptions
        {
            SingleWriter = false,
            SingleReader = true
        };
        _queue = Channel.CreateUnbounded<int>(options);
    }

    public void EnqueueImage(int imageId)
    {
        if (!_queue.Writer.TryWrite(imageId))
        {
            throw new InvalidOperationException($"Failed to enqueue image ID {imageId} for processing");
        }
    }

    public ValueTask<int> DequeueImageAsync(CancellationToken cancellationToken)
    {
        return _queue.Reader.ReadAsync(cancellationToken);
    }
}
