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
                var parameterName = context.Metadata.ParameterName;
                if (!string.IsNullOrEmpty(parameterName))
                {
                    if (string.Equals(parameterName, "id", StringComparison.OrdinalIgnoreCase) ||
                        parameterName.EndsWith("Id", StringComparison.OrdinalIgnoreCase))
                    {
                        return new BinderTypeModelBinder(typeof(HashidsModelBinder));
                    }
                }
            }

            return null;
        }
    }
}
