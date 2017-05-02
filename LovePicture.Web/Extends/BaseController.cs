using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using LovePicture.Model.MoClass;
using LovePicture.Web.Controllers;
using LovePicture.Model.Models;

namespace LovePicture.Web.Extends
{
    public class BaseController : Controller
    {
        public MoUserInfo _MyUserInfo;

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            _MyUserInfo = context.HttpContext.Session.Get<MoUserInfo>(context.HttpContext.Session.SessionKey());
            if (_MyUserInfo == null)
            {
                context.Result = new RedirectToActionResult(nameof(MemberController.Login), "Member", new { ReturnUrl = context.HttpContext.Request.Path });
            }

            ViewData["MyUserInfo"] = _MyUserInfo;

            base.OnActionExecuting(context);
        }
    }
}