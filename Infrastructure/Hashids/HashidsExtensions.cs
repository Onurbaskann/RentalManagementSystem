using System;
using HashidsNet;
using Microsoft.Extensions.DependencyInjection;

namespace KiraTakip
{
    public static class HashidsExtensions
    {
        private static IServiceProvider? _serviceProvider;

        public static void Configure(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public static string ToHashId(this int value)
        {
            if (_serviceProvider == null) return value.ToString();
            var hashids = _serviceProvider.GetService<IHashids>();
            return hashids != null ? hashids.Encode(value) : value.ToString();
        }

        public static string ToHashId(this int? value)
        {
            if (!value.HasValue) return string.Empty;
            return value.Value.ToHashId();
        }
    }
}
