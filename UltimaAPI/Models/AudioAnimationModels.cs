namespace UltimaAPI.Models;

internal sealed record SoundInfo(
    int Id,
    int ResolvedId,
    string Name,
    bool Translated,
    int ByteLength,
    double DurationSeconds,
    string MediaUrl);

internal sealed record AnimationSetInfo(int FileType, string FileName, int BodyCount);

internal sealed record AnimationFrameInfo(
    int Index,
    int Width,
    int Height,
    int CenterX,
    int CenterY,
    string MediaUrl);

internal sealed record AnimationInfo(
    int FileType,
    int Body,
    int Action,
    int Direction,
    int FrameCount,
    IReadOnlyList<AnimationFrameInfo> Frames);

internal sealed record AnimationDataInfo(
    int Id,
    int FrameCount,
    int FrameInterval,
    int FrameStart,
    int Unknown,
    IReadOnlyList<int> FrameData);
