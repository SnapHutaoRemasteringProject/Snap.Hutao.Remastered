// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Web.Response;

public static class ResponseValidationServiceCollectionExtension
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddResponseValidation()
        {
            return services.AddTransient<ICommonResponseValidator<Response>, DefaultResponseValidator>();
        }
    }
}