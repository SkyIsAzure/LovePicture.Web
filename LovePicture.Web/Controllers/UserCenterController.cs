using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using LovePicture.Web.Extends;
using LovePicture.Model.MoClass;
using LovePicture.Model.Models;
using LovePicture.Com;
using System.IO;
using Microsoft.Extensions.Options;
using static LovePicture.Com.EnumHelper;

namespace LovePicture.Web.Controllers
{
    public class UserCenterController : BaseController
    {
        private readonly LovePicture_DbContext _db;
        private readonly MoSelfSetting _selfSetting;

        public UserCenterController(LovePicture_DbContext db, IOptions<MoSelfSetting> selfSetting)
        {
            _db = db;
            _selfSetting = selfSetting.Value;
        }

        #region 个人中心

        public IActionResult Index()
        {

            return View();
        }

        #region 用户记录日志
        /// <summary>
        /// 记录日志列表  page-codeId
        /// </summary>
        /// <returns></returns>
        public IActionResult UserLogs(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) { return BadRequest(); }

            #region  构造参数
            var paramArr = id.Split('-');
            if (paramArr.Length != 2) { return BadRequest(); }
            var page = Convert.ToInt32(paramArr[1]);
            var codeId = Convert.ToInt32(paramArr[0]);
            if (codeId != (int)EnumHelper.EmLogCode.登录 && codeId != (int)EnumHelper.EmLogCode.积分)
            {
                return BadRequest();
            }
            page = page <= 0 ? 1 : page;

            var pageOption = new MoPagerOption
            {
                CurrentPage = page,
                PageSize = 15,
                Total = 0,
                RouteUrl = $"/usercenter/userlogs/{codeId}",
                StyleNum = 1,

                JoinOperateCode = "-"
            };
            #endregion

            var userLogs = _db.ToUserLog.
                 Where(b => b.UserId == _MyUserInfo.Id && b.CodeId == codeId).AsEnumerable();
            pageOption.Total = userLogs.Count();
            userLogs = userLogs.OrderByDescending(b => b.Id).
                 Skip((pageOption.CurrentPage - 1) * pageOption.PageSize).
                 Take(pageOption.PageSize).
                 ToList();
            ViewBag.PagerOption = pageOption;

            var userLog = new ToUserLog
            {
                CodeId = codeId,
                Des = $"{Enum.GetName(typeof(EnumHelper.EmLogCode), codeId)}记录"
            };
            ViewData["userLog"] = userLog;

            return View(userLogs);
        }

        /// <summary>
        /// 获取用户记录日志
        /// </summary>
        /// <param name="codeNum">日志类型</param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult UserLog(int? codeId, int page = 1, int pageSize = 5)
        {
            var data = new MoLoveData();
            if (codeId == null) { return Json(data); }

            page = page <= 0 ? 1 : page;
            pageSize = pageSize > 20 ? 20 : pageSize;
            data.Data = _db.ToUserLog.
                Where(b => b.UserId == _MyUserInfo.Id && b.CodeId == codeId).
                OrderByDescending(b => b.Id).
                Skip((page - 1) * pageSize).
                Take(pageSize).
                ToList();
            data.IsOk = true;
            return Json(data);
        }
        #endregion

        #region 统计信息

        [HttpPost]
        public JsonResult UserStatis()
        {
            var data = new MoLoveData();
            var list = new List<dynamic>();

            //留图数（公有）
            var userContent = _db.ToContent.Where(b => b.UserId == _MyUserInfo.Id).AsEnumerable();
            var total1 = userContent.Count(b => b.Status == (int)EnumHelper.EmContentStatus.公有);
            list.Add(new
            {
                name = "留图数（公有）",
                total = $"{ total1}（张）"
            });
            //留图数（私有）
            var total2 = userContent.Count(b => b.Status == (int)EnumHelper.EmContentStatus.私有);
            list.Add(new
            {
                name = "留图数（私有）",
                total = $"{ total2}（张）"
            });
            //点赞数
            var total3 = userContent.Where(b => b.Status != (int)EnumHelper.EmContentStatus.删除).Sum(b => b.ZanNum);
            list.Add(new
            {
                name = "点赞数",
                total = $"{ total3}（个）"
            });
            //浏览数
            var total4 = userContent.Where(b => b.Status != (int)EnumHelper.EmContentStatus.删除).Sum(b => b.ReadNum);
            list.Add(new
            {
                name = "浏览数",
                total = $"{ total4}（次）"
            });
            //爱心积分
            var total5 = _MyUserInfo.LevelNum;
            list.Add(new
            {
                name = "爱心积分",
                total = $"{ total5}（分）"
            });
            data.Data = list;
            data.IsOk = true;
            return Json(data);
        }

        #endregion

        #region 上传记录

        [HttpPost]
        public JsonResult UserUp()
        {
            var data = new MoLoveData();
            //留图数（公有）
            var userContent = _db.ToContent.Where(b => b.UserId == _MyUserInfo.Id &&
                                     b.Status != (int)EnumHelper.EmContentStatus.删除).
                                     OrderByDescending(b => b.CreateTime).Take(5).
                                     Select(b => new
                                     {
                                         Id = b.Id,
                                         Name = b.Name,
                                         ReadNum = b.ReadNum,
                                         ZanNum = b.ZanNum,
                                         MinPic = b.MinPic,

                                         MaxPic = b.MaxPic
                                     });
            data.Data = userContent;
            data.IsOk = true;
            return Json(data);
        }


        #endregion
        #endregion

        #region 账户设置

        public IActionResult AccountSettings()
        {

            return View(_MyUserInfo);
        }

        #region 修改头像
        public IActionResult UpHeadPhoto()
        {
            return View(_MyUserInfo);
        }

        [HttpPost]
        public async Task<IActionResult> UpHeadPhoto([Bind("Id")]MoUserInfo moUserInfo)
        {

            var file = Request.Form.Files.Where(b =>
                        b.Name == "myHeadPhoto" &&
                        b.ContentType.Contains("image")).
                        SingleOrDefault();
            if (file == null) { this.MsgBox("请选择上传的头像图片！"); return View(_MyUserInfo); }

            var maxSize = 1024 * 1024 * 4;
            if (file.Length > maxSize)
            {
                this.MsgBox("头像图片不能大于4M！"); return View(_MyUserInfo);
            }

            var fileExtend = file.FileName.Substring(file.FileName.LastIndexOf('.'));
            var fileNewName = $"{DateTime.Now.ToString("yyyyMMddHHmmssfff")}{fileExtend}";
            var path = Path.Combine(_selfSetting.UpHeadPhotoPath, fileNewName);
            using (var stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                await file.CopyToAsync(stream);
            }

            //更新数据
            var viewPath = $"{_selfSetting.ViewHeadPhotoPath}/{fileNewName}";

            var user = _db.ToUserInfo.Where(b => b.Id == _MyUserInfo.Id).SingleOrDefault();
            if (user == null) { this.MsgBox("上传失败，请稍后重试！"); return View(_MyUserInfo); }
            user.HeadPhoto = viewPath;
            user.LevelNum += (int)EmLevelNum.修改头像;
            var result = await _db.SaveChangesAsync();
            if (result > 0)
            {
                _MyUserInfo.HeadPhoto = viewPath;
                _MyUserInfo.LevelNum = user.LevelNum;
                HttpContext.Session.Set<MoUserInfo>(HttpContext.Session.SessionKey(), _MyUserInfo);
                this.MsgBox("上传成功！");

                _db.ToUserLog.Add(new ToUserLog
                {
                    CodeId = (int)EmLogCode.积分,
                    CreateTime = DateTime.Now,
                    Des = $"【修改头像】  +{(int)EmLevelNum.修改头像}",
                    UserId = _MyUserInfo.Id
                });
                await _db.SaveChangesAsync();

            }
            else { this.MsgBox("上传失败，请稍后重试！"); }
            return View(_MyUserInfo);
        }
        #endregion

        #region 修改基本信息

        public IActionResult ModifyUser()
        {
            return View(_MyUserInfo);
        }

        [HttpPost]
        public async Task<IActionResult> ModifyUser(MoUserInfo moUserInfo)
        {
            if (moUserInfo.Id <= 0)
            {
                this.MsgBox("修改失败，请稍后重试。");
                return View(_MyUserInfo);
            }
            else if (string.IsNullOrWhiteSpace(moUserInfo.NickName))
            {
                this.MsgBox("昵称不能为空！");
                return View(_MyUserInfo);
            }

            _MyUserInfo.NickName = moUserInfo.NickName;
            _MyUserInfo.Tel = moUserInfo.Tel;
            _MyUserInfo.Sex = moUserInfo.Sex;
            _MyUserInfo.Birthday = moUserInfo.Birthday;

            _MyUserInfo.Blog = moUserInfo.Blog;
            _MyUserInfo.Introduce = moUserInfo.Introduce;

            var user = _db.ToUserInfo.Where(b => b.Id == _MyUserInfo.Id).SingleOrDefault();
            if (user == null) { this.MsgBox("修改失败，请稍后重试"); return View(_MyUserInfo); }

            user.NickName = _MyUserInfo.NickName;
            user.Tel = _MyUserInfo.Tel;
            user.Sex = _MyUserInfo.Sex;
            user.Birthday = _MyUserInfo.Birthday;

            user.Blog = _MyUserInfo.Blog;
            user.Introduce = _MyUserInfo.Introduce;

            var result = await _db.SaveChangesAsync();
            if (result > 0)
            {
                HttpContext.Session.Set<MoUserInfo>(HttpContext.Session.SessionKey(), _MyUserInfo);
                this.MsgBox("修改成功！");
            }
            else { this.MsgBox("修改失败，请稍后重试！"); }

            return View(_MyUserInfo);
        }

        #endregion

        #region 修改密码

        public IActionResult ModifyPwd()
        {
            return View(new MoRegisterUser { UserName = _MyUserInfo.UserName });
        }

        [HttpPost]
        public async Task<IActionResult> ModifyPwd([Bind("UserName,UserPwd,ComfirmPwd")]MoRegisterUser moRegisterUser)
        {
            if (ModelState.IsValid)
            {
                var user = _db.ToUserInfo.Where(b => b.Id == _MyUserInfo.Id).SingleOrDefault();
                if (user == null) { this.MsgBox("修改失败，请稍后重试"); return View(moRegisterUser); }
                user.UserPwd = PublicClass._Md5(moRegisterUser.UserPwd.Trim());
                var result = await _db.SaveChangesAsync();
                if (result > 0)
                {
                    HttpContext.Session.Set<MoUserInfo>(HttpContext.Session.SessionKey(), _MyUserInfo);
                    this.MsgBox("修改成功！");
                }
                else { this.MsgBox("修改失败，请稍后重试！"); }
            }
            return View(moRegisterUser);
        }

        #endregion

        #endregion

        #region 我的相册

        public IActionResult Modules()
        {
            var modules = _db.ToModule.Where(b => b.Status == (int)EnumHelper.EmModuleStatus.启用).OrderByDescending(b => b.CreateTime).AsQueryable();
            return View(modules);
        }

        public IActionResult Module(string id)
        {

            #region  构造参数
            var paramArr = id.Split('-');
            if (paramArr.Length != 2) { return BadRequest(); }

            var page = Convert.ToInt32(paramArr[1]);
            var moduleId = Convert.ToInt32(paramArr[0]);

            var module = _db.ToModule.SingleOrDefault(b => b.Id == moduleId);
            if (module == null)
            {
                return BadRequest();
            }
            page = page <= 0 ? 1 : page;

            var pageOption = new MoPagerOption
            {
                CurrentPage = page,
                PageSize = 15,
                Total = 0,
                RouteUrl = $"/usercenter/module/{moduleId}",
                StyleNum = 1,

                JoinOperateCode = "-"
            };
            #endregion

            var contents = _db.ToContent.Where(b => b.UserId == _MyUserInfo.Id && b.ModuleId == moduleId && b.Status != (int)EnumHelper.EmContentStatus.删除).AsEnumerable();
            pageOption.Total = contents.Count();
            contents = contents.OrderByDescending(b => b.CreateTime).Skip((pageOption.CurrentPage - 1) * pageOption.PageSize).Take(pageOption.PageSize).ToList();

            ViewData["module"] = module;
            ViewBag.PagerOption = pageOption;
            return View(contents);
        }

        #endregion

        #region 快速上传

        public IActionResult UpPhoto(int? id)
        {
            var content = new ToContent { };
            var module = _db.ToModule.Where(b => b.Status == (int)EnumHelper.EmModuleStatus.启用 && b.Id == id).SingleOrDefault();
            if (module == null) { return BadRequest(); }
            ViewData["module"] = module;

            content.ModuleId = module.Id;
            content.MaxPic = "/images/default.svg";

            return View(content);
        }

        [HttpPost]
        public async Task<IActionResult> UpPhoto([Bind("ModuleId,Name,Des,Status")]ToContent content)
        {
            if (ModelState.IsValid)
            {
                var module = _db.ToModule.Where(b => b.Status == (int)EnumHelper.EmModuleStatus.启用 && b.Id == content.ModuleId).SingleOrDefault();
                if (module == null) { return BadRequest(); }
                ViewData["module"] = module;

                if (string.IsNullOrWhiteSpace(content.Name)) { this.MsgBox("描述名称必填！"); return View(content); }

                //图片
                var files = Request.Form.Files.Where(b =>
                      b.Name == "myPhoto" &&
                      b.ContentType.Contains("image")).AsEnumerable();

                var size = 1024 * 1024;
                var maxNum = 10;
                var maxSize = size * maxNum;
                var maxSingleNum = 4;
                var maxSingleSize = size * maxSingleNum;

                if (files == null) { this.MsgBox("请选择上传的图片！"); return View(content); }
                else if (files.Count() >= 11) { this.MsgBox("每次上传图片的数量不能超过10张！"); return View(content); }
                else if (files.Sum(b => b.Length) >= maxSize) { this.MsgBox($"每次上传图片的大小不能超过{maxNum}M！"); return View(content); }
                else if (files.Any(b => b.Length >= maxSingleSize)) { this.MsgBox($"单张图片的大小不能超过{maxSingleNum}M！"); return View(content); }

                //保存最小容量的一张
                var i = 1;
                var file = files.OrderBy(b => b.Length).FirstOrDefault();
                var fileExtend = file.FileName.Substring(file.FileName.LastIndexOf('.'));
                var fileNewName = $"{DateTime.Now.ToString("yyyyMMddHHmmssfff")}{i}{fileExtend}";
                var path = Path.Combine(_selfSetting.UpContentPhotoPath, fileNewName);
                using (var stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    await file.CopyToAsync(stream);
                }

                //更新数据
                content.MaxPic = $"{_selfSetting.ViewContentPhotoPath}/{fileNewName}";
                content.UserId = _MyUserInfo.Id;
                content.CreateTime = DateTime.Now;

                _db.Add(content);

                //积分
                _db.ToUserLog.Add(new ToUserLog
                {
                    CodeId = (int)EmLogCode.积分,
                    CreateTime = DateTime.Now,
                    Des = $"【上传图片】  +{(int)EmLevelNum.上传图片}",
                    UserId = _MyUserInfo.Id
                });

                var dbUser = _db.ToUserInfo.SingleOrDefault(b => b.Id == _MyUserInfo.Id);
                dbUser.LevelNum += (int)EmLevelNum.上传图片;

                //第一张保存在文件表中
                _db.ToContentFiles.Add(new ToContentFiles
                {
                    ContentId = content.Id,
                    MaxPic = content.MaxPic,
                    MinPic = content.MinPic,
                    ZanNum = content.ZanNum
                });

                //保存其他图片到文件表中
                foreach (var item in files.Where(b => b.FileName != file.FileName).Distinct())
                {
                    i++;
                    var fileExtend01 = item.FileName.Substring(item.FileName.LastIndexOf('.'));
                    var fileNewName01 = $"{DateTime.Now.ToString("yyyyMMddHHmmssfff")}{i}{fileExtend01}";
                    var path01 = Path.Combine(_selfSetting.UpContentPhotoPath, fileNewName01);
                    using (var stream = new FileStream(path01, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                    {
                        await item.CopyToAsync(stream);
                    }
                    _db.ToContentFiles.Add(new ToContentFiles
                    {
                        ContentId = content.Id,
                        MaxPic = $"{_selfSetting.ViewContentPhotoPath}/{fileNewName01}",
                        MinPic = null,
                        ZanNum = 0
                    });
                }

                var result = await _db.SaveChangesAsync();
                if (result > 0)
                {
                    return RedirectToAction(nameof(UserCenterController.Module), "usercenter", new { id = $"{module.Id}-1" });
                }
                else { this.MsgBox("保存失败，请稍后重试！"); }

            }
            return View(content);
        }

        #endregion

        #region 安全设置

        public IActionResult AnQuanSettings()
        {

            return View();
        }

        #region  设置邮箱

        public IActionResult SettingEmail()
        {
            return View();
        }

        //发送邮件
        [HttpPost]
        public async Task<IActionResult> SettingEmail(string email)
        {

            if (string.IsNullOrWhiteSpace(email)) { this.MsgBox("邮箱必填！"); return View(); }

            email = email.Trim();
            if (email.Length >= 50 || email.Length <= 3)
            {
                this.MsgBox("邮箱长度不符！"); return View();
            }
            else if (!email.Contains("@"))
            {
                this.MsgBox("邮箱格式不正确！"); return View();
            }

            var timeOut = 30;
            var now = DateTime.Now.AddMinutes(timeOut);
            var expires = now.ToString("yyyy-MM-dd HH:mm");
            var token = PublicClass._Md5($"{expires}-{email}-{Request.Host.Host}-{_MyUserInfo.Id}");
            var appUrl = $"http://{Request.Host.Host}:{Request.Host.Port}";
            var comfirmUrl = $"{appUrl}/usercenter/confirmsettingemail?expire={expires}&token={token}&email={email}&t=0.9527{_MyUserInfo.Id}";

            //读取模板
            var tpl = await PublicClass._GetHtmlTpl(EnumHelper.EmEmailTpl.SettingEmail, _selfSetting.EmailTplPath);
            if (string.IsNullOrWhiteSpace(tpl)) { this.MsgBox("发送绑定邮件失败，请稍后重试。"); return View(); }

            tpl = tpl.Replace("{name}", _MyUserInfo.NickName).
              Replace("{content}", $"您正在使用<a href='{appUrl}'>爱留图网</a>邮箱绑定功能，请点击以下链接确认绑定邮箱<a href='{comfirmUrl}'>{comfirmUrl}</a>；注意该地址有效时间{timeOut}分钟。");
            //发送
            var isOk = PublicClass._SendEmail(
               new Dictionary<string, string> {
                 { _MyUserInfo.NickName,email}
               },
               "爱留图 - 绑定邮箱",
               tpl);

            this.MsgBox(isOk ? "已给您邮箱发送了绑定确认邮件，请收件后点击确认绑定链接地址。" : "发送绑定邮件失败，请稍后重试！");
            return View();
        }

        //确认绑定
        public async Task<IActionResult> ConfirmSettingEmail(string expire, string token, string email, string t)
        {
            if (string.IsNullOrWhiteSpace(expire) || string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(email) || !email.Contains("@") || string.IsNullOrWhiteSpace(t))
            {
                return RedirectToAction(nameof(HomeController.Error), "home", new { msg = "无效的请求。" });
            }

            if (!DateTime.TryParse(expire, out var expires)) { return RedirectToAction(nameof(HomeController.Error), "home", new { msg = "无效的请求！" }); }
            else if (expires.AddMinutes(30) < DateTime.Now)
            {
                return RedirectToAction(nameof(HomeController.Error), "home", new { msg = "请求已过期，重新操作！" });
            }
            t = t.Replace("0.9527", "");
            var compareToken = PublicClass._Md5($"{expire}-{email}-{Request.Host.Host}-{t}");
            if (!token.Equals(compareToken)) { return RedirectToAction(nameof(HomeController.Error), "home", new { msg = "验证失败，无效的请求！" }); }

            var uid = Convert.ToInt32(t);
            if (uid != _MyUserInfo.Id) { return RedirectToAction(nameof(HomeController.Error), "home", new { msg = "验证失败，请登录后重新绑定！" }); }

            //处理
            if (_db.ToUserInfo.Any(b => b.Email == email))
            {
                return RedirectToAction(nameof(HomeController.Error), "home", new { msg = "绑定失败，该邮箱已被使用过了。" });
            }

            var user = _db.ToUserInfo.Where(b => b.Id == uid && b.Status == (int)EnumHelper.EmUserStatus.启用).SingleOrDefault();
            if (user == null)
            {
                return RedirectToAction(nameof(HomeController.Error), "home", new { msg = "绑定失败，无效的请求。" });
            }
            user.Email = email;
            user.LevelNum += (int)EmLevelNum.绑定邮箱;

            _db.ToUserLog.Add(new ToUserLog
            {
                CodeId = (int)EmLogCode.积分,
                CreateTime = DateTime.Now,
                Des = $"【绑定邮箱】  +{(int)EmLevelNum.绑定邮箱}",
                UserId = user.Id
            });
            var result = await _db.SaveChangesAsync();
            if (result > 0)
            {
                this.MsgBox("绑定邮箱成功！");
                _MyUserInfo.Email = email;
                //如果是登陆状态，需要更新session
                HttpContext.Session.Set<MoUserInfo>(HttpContext.Session.SessionKey(), _MyUserInfo);
            }
            else { this.MsgBox("绑定失败，请稍重试！"); }
            return View();
        }

        #endregion

        #endregion
    }
}