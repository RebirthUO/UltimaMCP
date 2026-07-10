using System.Drawing;
using Microsoft.Extensions.Options;
using Ultima;
using UltimaAPI.Configuration;
using UltimaAPI.Models;

namespace UltimaAPI.Services;

internal sealed class AudioAnimationService
{
    private const int MaximumSoundId = 0xFFE;

    private readonly UltimaSdkGateway _sdk;
    private readonly int _maxAnimationFrames;

    public AudioAnimationService(UltimaSdkGateway sdk, IOptions<UltimaOptions> options)
    {
        ArgumentNullException.ThrowIfNull(sdk);
        ArgumentNullException.ThrowIfNull(options);

        _sdk = sdk;
        _maxAnimationFrames = options.Value.Limits.MaxAnimationFrames;
    }

    public SoundInfo GetSoundInfo(int id)
    {
        _sdk.RequireCapability("sounds");
        RequestBounds.RequireInRange(id, 0, MaximumSoundId, nameof(id));

        return _sdk.Read(() =>
        {
            UOSound? sound = Sounds.GetSound(id, out bool translated);
            if (sound is null)
            {
                throw new KeyNotFoundException($"Sound {id} was not found.");
            }

            double duration = Math.Max(0, sound.buffer.Length - 44) / 44_100d;
            return new SoundInfo(
                id,
                sound.ID,
                sound.Name ?? string.Empty,
                translated,
                sound.buffer.Length,
                duration,
                $"/api/sounds/{id}.wav");
        });
    }

    public MediaAsset GetSoundWav(int id)
    {
        _sdk.RequireCapability("sounds");
        RequestBounds.RequireInRange(id, 0, MaximumSoundId, nameof(id));

        return _sdk.Read(() =>
        {
            UOSound? sound = Sounds.GetSound(id);
            if (sound is null)
            {
                throw new KeyNotFoundException($"Sound {id} was not found.");
            }

            return new MediaAsset(sound.buffer, "audio/wav", $"sound-{id:D4}.wav");
        });
    }

    public PageResult<SoundInfo> SearchSounds(string? query, int offset, int? limit)
    {
        string term = RequestBounds.RequireSearchTerm(query, nameof(query));
        int take = RequestBounds.NormalizeLimit(limit, 25, 100);
        RequestBounds.RequireInRange(offset, 0, MaximumSoundId, nameof(offset));
        _sdk.RequireCapability("sounds");

        return _sdk.Read(() =>
        {
            List<SoundInfo> items = [];
            int total = 0;

            for (int id = 0; id <= MaximumSoundId; id++)
            {
                if (!Sounds.IsValidSound(id, out string name) || !name.Contains(term, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (total >= offset && items.Count < take)
                {
                    UOSound sound = Sounds.GetSound(id, out bool translated);
                    items.Add(new SoundInfo(
                        id,
                        sound.ID,
                        name,
                        translated,
                        sound.buffer.Length,
                        Math.Max(0, sound.buffer.Length - 44) / 44_100d,
                        $"/api/sounds/{id}.wav"));
                }

                total++;
            }

            return new PageResult<SoundInfo>(offset, take, total, items);
        });
    }

    public IReadOnlyList<AnimationSetInfo> GetAnimationSets()
        => _sdk.Read(() => Enumerable.Range(1, 5)
            .Where(IsAnimationFileAvailable)
            .Select(fileType => new AnimationSetInfo(
                fileType,
                fileType == 1 ? "anim.mul" : $"anim{fileType}.mul",
                Animations.GetAnimCount(fileType)))
            .ToArray());

    public AnimationInfo GetAnimationInfo(int fileType, int body, int action, int direction)
    {
        ValidateAnimation(fileType, body, action, direction);

        return _sdk.Read(() =>
        {
            Frame[]? frames = Animations.GetAnimation(body, action, direction, fileType);
            if (frames is null)
            {
                throw new KeyNotFoundException("The requested animation was not found.");
            }

            try
            {
                AnimationFrameInfo[] frameModels = frames
                    .Take(_maxAnimationFrames)
                    .Select((frame, index) => ToFrameInfo(frame, index, fileType, body, action, direction))
                    .ToArray();
                return new AnimationInfo(fileType, body, action, direction, frames.Length, frameModels);
            }
            finally
            {
                DisposeFrames(frames);
            }
        });
    }

    public MediaAsset GetAnimationFramePng(int fileType, int body, int action, int direction, int frameIndex)
    {
        ValidateAnimation(fileType, body, action, direction);
        RequestBounds.RequireInRange(frameIndex, 0, _maxAnimationFrames - 1, nameof(frameIndex));

        return _sdk.Read(() =>
        {
            Frame[]? frames = Animations.GetAnimation(body, action, direction, fileType);
            if (frames is null || frameIndex >= frames.Length)
            {
                DisposeFrames(frames);
                throw new KeyNotFoundException("The requested animation frame was not found.");
            }

            try
            {
                Bitmap? bitmap = frames[frameIndex].Bitmap;
                if (bitmap is null)
                {
                    throw new KeyNotFoundException("The requested animation frame has no image data.");
                }

                return MediaEncoder.ToPng(bitmap, $"animation-{fileType}-{body}-{action}-{direction}-{frameIndex}.png");
            }
            finally
            {
                DisposeFrames(frames);
            }
        });
    }

    public AnimationDataInfo GetAnimationData(int id)
    {
        _sdk.RequireCapability("animationData");
        RequestBounds.RequireInRange(id, 0, int.MaxValue, nameof(id));

        return _sdk.Read(() =>
        {
            Animdata.Data? data = Animdata.GetAnimData(id);
            if (data is null)
            {
                throw new KeyNotFoundException($"Animation data {id} was not found.");
            }

            int frameCount = Math.Min(data.FrameCount, data.FrameData.Length);
            int[] frameData = data.FrameData.Take(frameCount).Select(static value => (int)value).ToArray();
            return new AnimationDataInfo(id, data.FrameCount, data.FrameInterval, data.FrameStart, data.Unknown, frameData);
        });
    }

    private void ValidateAnimation(int fileType, int body, int action, int direction)
    {
        RequestBounds.RequireInRange(fileType, 1, 5, nameof(fileType));
        if (!IsAnimationFileAvailable(fileType))
        {
            throw new UltimaSdkUnavailableException($"Animation file set {fileType} is unavailable.");
        }

        (int bodyCount, int actionCount) = _sdk.Read(() =>
            (Animations.GetAnimCount(fileType), Animations.GetAnimLength(body, fileType)));
        RequestBounds.RequireInRange(body, 0, bodyCount - 1, nameof(body));
        RequestBounds.RequireInRange(action, 0, actionCount - 1, nameof(action));
        RequestBounds.RequireInRange(direction, 0, 7, nameof(direction));
    }

    private bool IsAnimationFileAvailable(int fileType)
    {
        string suffix = fileType == 1 ? string.Empty : fileType.ToString();
        return _sdk.IsFileAvailable($"anim{suffix}.idx") && _sdk.IsFileAvailable($"anim{suffix}.mul");
    }

    private static AnimationFrameInfo ToFrameInfo(
        Frame frame,
        int index,
        int fileType,
        int body,
        int action,
        int direction)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(6, 1))
        {
            throw new PlatformNotSupportedException("Animation image processing requires Windows 7 or later.");
        }

        Bitmap? bitmap = frame.Bitmap;
        return new AnimationFrameInfo(
            index,
            bitmap?.Width ?? 0,
            bitmap?.Height ?? 0,
            frame.Center.X,
            frame.Center.Y,
            $"/api/animations/{fileType}/{body}/{action}/{direction}/frames/{index}.png");
    }

    private static void DisposeFrames(Frame[]? frames)
    {
        if (frames is null)
        {
            return;
        }

        if (!OperatingSystem.IsWindowsVersionAtLeast(6, 1))
        {
            throw new PlatformNotSupportedException("Animation image processing requires Windows 7 or later.");
        }

        foreach (Frame frame in frames)
        {
            frame.Bitmap?.Dispose();
        }
    }
}
