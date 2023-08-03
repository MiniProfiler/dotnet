using System;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Samples.AspNetCore
{
    /// <summary>
    /// Nonce service (custom implementation) for sharing a random nonce for the lifetime of a request.
    /// </summary>
    public class NonceService
    {
        public string RequestNonce { get; } = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    public static class NonceExtensions
    {
        public static string? GetNonce(this HttpContext context) => context.RequestServices.GetService<NonceService>()?.RequestNonce;
    }
}
