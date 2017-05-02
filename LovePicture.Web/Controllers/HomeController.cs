using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using LovePicture.Model.MoClass;
using LovePicture.Model.Models;
using LovePicture.Com;
using Microsoft.Extensions.Caching.Memory;

namespace LovePicture.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly LovePicture_DbContext _db;
        private readonly IMemoryCache _cache;

        public HomeController(LovePicture_DbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }


        public IActionResult Index()
        {
     
            var contents = new List<ToContent>();
            try
            {
                var key = "5daytuijian";
                contents = _cache.Get<List<ToContent>>(key);
                if (contents == null || contents.Count <= 0)
                {
                    var nowDate = DateTime.Now.AddHours(-1);
                    var minDate = nowDate.AddDays(-5);
                    contents = _db.ToContent.Where(b => b.CreateTime >= minDate && b.CreateTime <= nowDate && b.Status == (int)EnumHelper.EmContentStatus.公有).
                        OrderByDescending(b => b.ZanNum).Take(8).ToList();

                    _cache.Set<List<ToContent>>(key, contents, TimeSpan.FromHours(1));
                }

            }
            catch (Exception ex)
            {
            }
            return View(contents);
        }

        /// <summary>
        /// 获取首页推荐的几个栏目
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public JsonResult GetTuiJianModules()
        {
            var data = new MoLoveData();

            try
            {
                var key = "tuijianmodules";
                data = _cache.Get<MoLoveData>(key);
                if (data == null)
                {
                    data = new MoLoveData();

                    var modules = _db.ToModule.Where(b => b.Status == (int)EnumHelper.EmModuleStatus.启用).
                         OrderBy(b => Guid.NewGuid()).
                         Take(4).
                         ToList();
                    data.Data = modules;
                    data.IsOk = true;
                    if (modules.Count > 0) { _cache.Set<MoLoveData>(key, data, TimeSpan.FromHours(5)); }
                }
            }
            catch (Exception ex)
            {
            }
            return Json(data);
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

        public IActionResult Error(string msg = null)
        {
            this.MsgBox(msg ?? "访问出问题了，开发人员正从火星赶回来修复，请耐心等待！");
            return View();
        }
    }
}
