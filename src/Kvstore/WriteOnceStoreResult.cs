namespace Kvstore;

public sealed record WriteOnceStoreResult(bool Created, string VersionId);
