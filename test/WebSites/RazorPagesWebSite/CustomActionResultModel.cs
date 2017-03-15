// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RazorPagesWebSite
{
    public class CustomActionResultModel : PageModel
    {
        public string MethodName { get; set; }

        public IActionResult OnGet()
        {
            return View();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await Task.Delay(1);
            MethodName = nameof(OnPostAsync);
            return View();
        }

        public async Task OnGetCustomer()
        {
            await Task.Delay(1);
            MethodName = nameof(OnGetCustomer);
        }

        public async Task OnGetViewCustomerAsync()
        {
            await Task.Delay(1);
            MethodName = nameof(OnGetViewCustomerAsync);
        }

        public async Task<CustomActionResult> OnPostCustomActionResult()
        {
            await Task.Delay(1);
            return new CustomActionResult();
        }

        public CustomActionResult OnGetCustomActionResultAsync()
        {
            return new CustomActionResult();
        }
    }

    public class CustomActionResult : IActionResult
    {
        public Task ExecuteResultAsync(ActionContext context)
        {
            context.HttpContext.Response.StatusCode = 200;
            return context.HttpContext.Response.WriteAsync(nameof(CustomActionResult));
        }
    }
}