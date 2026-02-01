namespace Times.Infrastructure.Auth
{
	/// <summary>
	/// Stores revoked JWT IDs so logged-out tokens are rejected until they expire.
	/// </summary>
	public interface IRevokedTokenStore
	{
		/// <summary>Marks a token as revoked until the given expiry.</summary>
		Task RevokeAsync(string jti, DateTime expiresAtUtc, CancellationToken cancellationToken = default);

		/// <summary>Returns true if the token has been revoked (and not yet expired).</summary>
		Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken = default);
	}
}
