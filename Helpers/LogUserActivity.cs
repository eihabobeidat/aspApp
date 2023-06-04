using API.Extensions;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc.Filters;

namespace API.Helpers
{
    public class LogUserActivity : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync( ActionExecutingContext context, ActionExecutionDelegate next )
        {
            var resultContext = await next();
            if ( !resultContext.HttpContext.User.Identity.IsAuthenticated )
                return;
            var userId = resultContext.HttpContext.User.GetIdentity();
            var unitOfWork = resultContext.HttpContext.RequestServices.GetRequiredService<IUnitOfWork>();
            var user = await unitOfWork.UserRepository.GetUserByIdAsync(userId);
            user.LastActive = DateTime.UtcNow;
            try
            {
                await unitOfWork.Complete();
                return;
            }
            catch
            {
                return; //since it is okey not to be 100% accurate
            }

        }
    }
}
