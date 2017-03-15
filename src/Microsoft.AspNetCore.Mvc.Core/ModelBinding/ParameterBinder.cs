// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    public class ParameterBinder
    {
        public ParameterBinder(
            IModelMetadataProvider modelMetadataProvider,
            IModelBinderFactory modelBinderFactory,
            IObjectModelValidator validator)
        {
            ModelMetadataProvider = modelMetadataProvider;
            ModelBinderFactory = modelBinderFactory;
            Validator = validator;
        }

        public IModelMetadataProvider ModelMetadataProvider { get; }

        public IModelBinderFactory ModelBinderFactory { get; }

        public IObjectModelValidator Validator { get; }

        public Task<ModelBindingResult> BindModelAsync(
            ActionContext actionContext,
            IValueProvider valueProvider,
            ParameterDescriptor parameter)
        {
            return BindModelAsync(actionContext, valueProvider, parameter, value: null);
        }

        public async Task<ModelBindingResult> BindModelAsync(
            ActionContext actionContext,
            IValueProvider valueProvider,
            ParameterDescriptor parameter,
            object value)
        {
            if (parameter == null)
            {
                throw new ArgumentNullException(nameof(parameter));
            }

            if (actionContext == null)
            {
                throw new ArgumentNullException(nameof(actionContext));
            }

            if (valueProvider == null)
            {
                throw new ArgumentNullException(nameof(valueProvider));
            }

            var metadata = ModelMetadataProvider.GetMetadataForType(parameter.ParameterType);
            var binder = ModelBinderFactory.CreateBinder(new ModelBinderFactoryContext()
            {
                BindingInfo = parameter.BindingInfo,
                Metadata = metadata,
                CacheToken = parameter,
            });

            var modelBindingContext = DefaultModelBindingContext.CreateBindingContext(
                actionContext,
                valueProvider,
                metadata,
                parameter.BindingInfo,
                parameter.Name);
            modelBindingContext.Model = value;

            var parameterModelName = parameter.BindingInfo?.BinderModelName ?? metadata.BinderModelName;
            if (parameterModelName != null)
            {
                // The name was set explicitly, always use that as the prefix.
                modelBindingContext.ModelName = parameterModelName;
            }
            else if (modelBindingContext.ValueProvider.ContainsPrefix(parameter.Name))
            {
                // We have a match for the parameter name, use that as that prefix.
                modelBindingContext.ModelName = parameter.Name;
            }
            else
            {
                // No match, fallback to empty string as the prefix.
                modelBindingContext.ModelName = string.Empty;
            }

            await binder.BindModelAsync(modelBindingContext);

            var modelBindingResult = modelBindingContext.Result;
            if (modelBindingResult.IsModelSet)
            {
                Validator.Validate(
                    actionContext,
                    modelBindingContext.ValidationState,
                    modelBindingContext.ModelName,
                    modelBindingResult.Model);
            }

            return modelBindingResult;
        }
    }
}
