namespace TmpIO;

public record RiffChunk(string Id, byte[] Data, IReadOnlyList<RiffChunk>? SubChunks = null);