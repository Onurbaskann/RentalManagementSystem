using System;
using System.Threading.Tasks;
using HashidsNet;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace KiraTakip.Infrastructure.Hashids
{
    public class HashidsModelBinder : IModelBinder
    {
        private readonly IHashids _hashids;

        public HashidsModelBinder(IHashids hashids)
        {
            _hashids = hashids;
        }

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            var modelName = bindingContext.ModelName;
            var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);

            if (valueProviderResult == ValueProviderResult.None)
            {
                return Task.CompletedTask;
            }

            bindingContext.ModelState.SetModelValue(modelName, valueProviderResult);

            var value = valueProviderResult.FirstValue;

            if (string.IsNullOrEmpty(value))
            {
                return Task.CompletedTask;
            }

            try
            {
                // Try decoding the hashid first
                var decoded = _hashids.Decode(value);
                if (decoded.Length > 0)
                {
                    bindingContext.Result = ModelBindingResult.Success(decoded[0]);
                    return Task.CompletedTask;
                }

                // Fallback: If it's a direct integer (e.g. legacy/testing routes)
                if (int.TryParse(value, out int intVal))
                {
                    bindingContext.Result = ModelBindingResult.Success(intVal);
                    return Task.CompletedTask;
                }
            }
            catch (Exception)
            {
                bindingContext.ModelState.TryAddModelError(modelName, "Geçersiz kimlik formatı.");
            }

            return Task.CompletedTask;
        }
    }
}
