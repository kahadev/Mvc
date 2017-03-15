// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public static class PagePropertyBinderFactory
    {
        public static Func<Page, object, Task> GetModelBinderFactory(
            ParameterBinder parameterBinder,
            CompiledPageActionDescriptor actionDescriptor)
        {
            if (parameterBinder == null)
            {
                throw new ArgumentNullException(nameof(parameterBinder));
            }

            if (actionDescriptor == null)
            {
                throw new ArgumentNullException(nameof(actionDescriptor));
            }

            var bindPropertiesOnPage = actionDescriptor.ModelTypeInfo == null;
            var propertiesToBind = GetPropertiesToBind(
                parameterBinder.ModelMetadataProvider,
                bindPropertiesOnPage ? actionDescriptor.PageTypeInfo : actionDescriptor.ModelTypeInfo);

            if (propertiesToBind.Count == 0)
            {
                return null;
            }

            return (page, model) =>
            {
                var pageContext = page.PageContext;
                var instance = bindPropertiesOnPage ? page : model;
                return BindPropertiesAsync(parameterBinder, pageContext, instance, propertiesToBind);
            };
        }

        private static async Task BindPropertiesAsync(
            ParameterBinder parameterBinder,
            PageContext pageContext,
            object instance,
            IList<PropertyBindingInfo> propertiesToBind)
        {
            var valueProvider = await GetCompositeValueProvider(pageContext);
            for (var i = 0; i < propertiesToBind.Count; i++)
            {
                var propertyBindingInfo = propertiesToBind[i];
                var modelBindingResult = await parameterBinder.BindModelAsync(
                    pageContext, 
                    valueProvider, 
                    propertyBindingInfo.ParameterDescriptor);
                if (modelBindingResult.IsModelSet)
                {
                    var modelMetadata = propertyBindingInfo.ModelMetadata;
                    var propertyHelper = propertyBindingInfo.PropertyHelper;
                    PropertyValueSetter.SetValue(
                        modelMetadata,
                        propertyHelper.Property,
                        propertyHelper.ValueSetter,
                        propertyHelper.ValueGetter,
                        instance,
                        modelBindingResult.Model);
                }
            }
        }

        private static IList<PropertyBindingInfo> GetPropertiesToBind(
            IModelMetadataProvider modelMetadataProvider,
            TypeInfo handlerSource)
        {
            var properties = PropertyHelper.GetProperties(type: handlerSource.AsType());
            if (properties.Length == 0)
            {
                return EmptyArray<PropertyBindingInfo>.Instance;
            }

            var propertyBindingInfo = new List<PropertyBindingInfo>();
            for (var i = 0; i < properties.Length; i++)
            {
                var propertyHelper = properties[i];
                var property = propertyHelper.Property;
                var attributes = property.GetCustomAttributes(inherit: true);
                var bindingInfo = BindingInfo.GetBindingInfo(attributes);
                if (bindingInfo == null)
                {
                    continue;
                }

                var parameterDescriptor = new ParameterDescriptor
                {
                    BindingInfo = bindingInfo,
                    Name = propertyHelper.Name,
                    ParameterType = property.PropertyType,
                };

                var modelMetadata = modelMetadataProvider.GetMetadataForType(property.PropertyType);
                propertyBindingInfo.Add(new PropertyBindingInfo(propertyHelper, parameterDescriptor, modelMetadata));
            }

            return propertyBindingInfo;
        }

        private static async Task<CompositeValueProvider> GetCompositeValueProvider(PageContext pageContext)
        {
            var factories = pageContext.ValueProviderFactories;
            var valueProviderFactoryContext = new ValueProviderFactoryContext(pageContext);
            for (var i = 0; i < factories.Count; i++)
            {
                var factory = factories[i];
                await factory.CreateValueProviderAsync(valueProviderFactoryContext);
            }

            return new CompositeValueProvider(valueProviderFactoryContext.ValueProviders);
        }

        private struct PropertyBindingInfo
        {
            public PropertyBindingInfo(
                PropertyHelper helper,
                ParameterDescriptor parameterDescriptor,
                ModelMetadata modelMetadata)
            {
                PropertyHelper = helper;
                ParameterDescriptor = parameterDescriptor;
                ModelMetadata = modelMetadata;
            }

            public PropertyHelper PropertyHelper { get; }

            public ParameterDescriptor ParameterDescriptor { get; }

            public ModelMetadata ModelMetadata { get; }
        }
    }
}
