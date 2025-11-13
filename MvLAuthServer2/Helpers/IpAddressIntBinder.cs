using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Net;
using System.Threading.Tasks;

namespace MvLAuthServer2.Helpers
{
    public class IpAddressIntBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if (valueProviderResult == ValueProviderResult.None)
            {
                return Task.CompletedTask;
            }

            bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);

            var value = valueProviderResult.FirstValue;

            if (string.IsNullOrEmpty(value))
            {
                return Task.CompletedTask;
            }

            if (!IPAddress.TryParse(value, out var ip))
            {
                bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, "Invalid IP address");
                return Task.CompletedTask;
            }

            var bytes = ip.GetAddressBytes();
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            bindingContext.Result = ModelBindingResult.Success(BitConverter.ToUInt32(bytes, 0));
            return Task.CompletedTask;
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class BindIpAddressAsIntAttribute : ModelBinderAttribute
    {
        public BindIpAddressAsIntAttribute()
        {
            BinderType = typeof(IpAddressIntBinder);
        }
    }
}
