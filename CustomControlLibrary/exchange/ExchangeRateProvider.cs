using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CustomControlLibrary
{
    /// <summary>
    /// Exchange currency provider.
    /// </summary>
    public class ExchangeRateProvider : IExchangeRateProvider
    {
        private readonly HttpClient _client = new();

        ///<inheritdoc/>
        public async Task<double> GetExchangeRate(string fromCurrencyId, string toCurrencyId, CancellationToken cancellationToken)
        {
            // Todo organize cache functionality
            try
            {
                var keyPair = $"{fromCurrencyId}_{toCurrencyId}";
                using var result = _client.GetStreamAsync($"https://free.currconv.com/api/v7/convert?q={keyPair}&compact=ultra&apiKey=fc23d453e3873a17e964", cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                {
                    return double.NaN;
                }

                var data = await JsonSerializer.DeserializeAsync<Dictionary<string, double>>(await result, cancellationToken: cancellationToken);
                return data[keyPair];
            }
            catch (Exception)
            {
                return double.NaN;
            }
        }
    }
}