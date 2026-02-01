using System.Collections.Concurrent;

namespace Times.Infrastructure.Auth
{
	/// <summary>
	/// In-memory store for revoked JTI values. Cleans expired entries on check.
	/// For production across multiple instances, replace with a distributed store (e.g. Redis).
	/// </summary>
	public sealed class RevokedTokenStore : IRevokedTokenStore
	{
		// JTI -> expiry utc (we remove when expired)
		private readonly ConcurrentDictionary<string, DateTime> _revoked = new();

		public Task RevokeAsync(string jti, DateTime expiresAtUtc, CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrEmpty(jti)) return Task.CompletedTask;
			_revoked[jti] = expiresAtUtc;
			return Task.CompletedTask;
		}

		public Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrEmpty(jti)) return Task.FromResult(false);
			PruneExpired();
			return Task.FromResult(_revoked.TryGetValue(jti, out var expires) && expires > DateTime.UtcNow);
		}

		private void PruneExpired()
		{
			var now = DateTime.UtcNow;
			foreach (var kv in _revoked.ToArray())
			{
				if (kv.Value <= now)
					_revoked.TryRemove(kv.Key, out _);
			}
		}
	}
}
