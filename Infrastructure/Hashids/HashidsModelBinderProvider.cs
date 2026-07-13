using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace KiraTakip.Infrastructure.Hashids
{
    public class HashidsModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var modelType = context.Metadata.ModelType;
            if (modelType == typeof(int) || modelType == typeof(int?))
            {
                var name = context.Metadata.PropertyName ?? context.Metadata.ParameterName;
                if (!string.IsNullOrEmpty(name))
                {
                    if (string.Equals(name, "id", StringComparison.OrdinalIgnoreCase) ||
                        name.EndsWith("Id", StringComparison.OrdinalIgnoreCase))
                    {
                        return new BinderTypeModelBinder(typeof(HashidsModelBinder));
                    }
                }
            }

            return null;
        }
    }
}
