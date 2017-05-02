using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LovePicture.Model.Models;
using LovePicture.Com;
using LovePicture.Web;
using LovePicture.Model.MoClass;
using static LovePicture.Com.EnumHelper;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;

namespace LovePicture.Web.Controllers
{
    /// <summary>
    /// �û�δ��¼�Ĳ���
    /// </summary>
    public class MemberController : Controller
    {
        private readonly LovePicture_DbContext _context;
        private readonly MoSelfSetting _selfSetting;
        private readonly IMemoryCache _cache;

        public MemberController(LovePicture_DbContext context, IOptions<MoSelfSetting> selfSetting, IMemoryCache cache)
        {
            _context = context;
            _selfSetting = selfSetting.Value;
            _cache = cache;
        }

        // GET: Member
        public IActionResult Login(string returnUrl = null)
        {
            //��ȡsession
            var userInfo = HttpContext.Session.Get<MoUserInfo>(HttpContext.Session.SessionKey());
            if (userInfo != null)
            {
                if (string.IsNullOrWhiteSpace(returnUrl)) { return RedirectToAction(nameof(HomeController.Index), "Home"); }
                else { Redirect(returnUrl); }
            }
            this.MsgBox(returnUrl, "returnUrl");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login([Bind("UserName,UserPwd,ReturnUrl")] MoLoginUser loginUser)
        {
            if (ModelState.IsValid)
            {
                #region ��֤
                var md5Pwd = PublicClass._Md5(loginUser.UserPwd.Trim());
                var userInfo = await _context.ToUserInfo.SingleOrDefaultAsync(b =>
                        b.UserName.Equals(loginUser.UserName, StringComparison.CurrentCultureIgnoreCase) &&
                        b.UserPwd.Equals(md5Pwd));
                if (userInfo == null)
                {
                    this.MsgBox("�˺Ż��������");
                    return View(loginUser);
                }
                else if (userInfo.Status == (int)EnumHelper.EmUserStatus.����)
                {
                    this.MsgBox("���˺��ѱ����ã���������Գ�������ע��һ���˺ţ�");
                    return View(loginUser);
                }
                #endregion

                #region ���µ�¼��Ϣ
                userInfo.Ips = this.GetUserIp();
                userInfo.LoginTime = DateTime.Now;
                userInfo.LevelNum += (int)EmLevelNum.��¼;

                //��¼session
                var moUserInfo = new MoUserInfo
                {
                    Id = userInfo.Id,
                    UserName = userInfo.UserName,
                    NickName = userInfo.NickName,
                    Addr = userInfo.Addr,
                    Birthday = userInfo.Birthday,

                    Blog = userInfo.Blog,
                    CreateTime = userInfo.CreateTime,
                    Email = userInfo.Email,
                    HeadPhoto = userInfo.HeadPhoto,
                    Introduce = userInfo.Introduce,

                    Ips = userInfo.Ips,
                    LevelNum = userInfo.LevelNum,
                    Sex = userInfo.Sex,
                    Tel = userInfo.Tel,
                    Status = userInfo.Status,

                    LoginTime = Convert.ToDateTime(userInfo.LoginTime)
                };
                HttpContext.Session.Set<MoUserInfo>(HttpContext.Session.SessionKey(), moUserInfo);

                if (!string.IsNullOrWhiteSpace(moUserInfo.Ips))
                {
                    _context.ToUserLog.Add(new ToUserLog
                    {
                        CodeId = (int)EmLogCode.��¼,
                        CreateTime = DateTime.Now,
                        Des = $"IP��{moUserInfo.Ips}����¼ʱ�䣺{moUserInfo.LoginTime.ToString("yyyy-MM-dd HH:mm")}",
                        UserId = userInfo.Id
                    });
                }

                _context.ToUserLog.Add(new ToUserLog
                {
                    CodeId = (int)EmLogCode.����,
                    CreateTime = DateTime.Now,
                    Des = $"����¼��  +{(int)EmLevelNum.��¼}",
                    UserId = userInfo.Id
                });

                await _context.SaveChangesAsync();

                if (string.IsNullOrWhiteSpace(loginUser.ReturnUrl)) { return RedirectToAction(nameof(HomeController.Index), "Home"); }
                else { return Redirect(loginUser.ReturnUrl); }
                #endregion
            }
            return View(loginUser);
        }

        /// <summary>
        /// ��ȡ�û���½��Ϣ
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public JsonResult GetLogin()
        {
            var data = new MoLoveData();

            var userInfo = HttpContext.Session.Get<MoUserInfo>(HttpContext.Session.SessionKey());
            if (userInfo != null)
            {
                data.Data = userInfo;
                data.IsOk = true;
            }
            data.Msg = data.IsOk ? "�ѵ�¼" : "δ��¼";
            return Json(data);
        }

        // GET: Member/Create
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register([Bind("UserName,UserPwd,ComfirmPwd")] MoRegisterUser loginUser)
        {
            if (ModelState.IsValid)
            {
                #region ��֤
                if (_context.ToUserInfo.Any(b => b.UserName.ToUpper() == loginUser.UserName.Trim().ToUpper()))
                {
                    this.MsgBox("�Ѿ�������ͬ���˺ţ�");
                    return View(loginUser);
                }
                #endregion

                #region ���

                ToUserInfo userInfo = new ToUserInfo();

                userInfo.UserName = loginUser.UserName.Trim();
                userInfo.UserPwd = PublicClass._Md5(loginUser.UserPwd.Trim());
                userInfo.NickName = userInfo.UserName;
                userInfo.Status = (int)EnumHelper.EmUserStatus.����;
                userInfo.CreateTime = DateTime.Now;
                userInfo.LevelNum = (int)EmLevelNum.ע��;

                userInfo.Ips = this.GetUserIp();
                userInfo.HeadPhoto = "/images/ailiutu_user.png";
                userInfo.Sex = false;

                _context.Add(userInfo);
                var result = await _context.SaveChangesAsync();
                if (result > 0)
                {
                    var moUserInfo = new MoUserInfo
                    {
                        Id = userInfo.Id,
                        UserName = userInfo.UserName,
                        NickName = userInfo.NickName,
                        Addr = userInfo.Addr,
                        Birthday = userInfo.Birthday,

                        Blog = userInfo.Blog,
                        CreateTime = userInfo.CreateTime,
                        Email = userInfo.Email,
                        HeadPhoto = userInfo.HeadPhoto,
                        Introduce = userInfo.Introduce,

                        Ips = userInfo.Ips,
                        LevelNum = userInfo.LevelNum,
                        Sex = userInfo.Sex,
                        Tel = userInfo.Tel,
                        Status = userInfo.Status,

                        LoginTime = DateTime.Now
                    };
                    HttpContext.Session.Set<MoUserInfo>(HttpContext.Session.SessionKey(), moUserInfo);

                    if (!string.IsNullOrWhiteSpace(moUserInfo.Ips))
                    {
                        _context.ToUserLog.Add(new ToUserLog
                        {
                            CodeId = (int)EmLogCode.��¼,
                            CreateTime = DateTime.Now,
                            Des = $"IP��{moUserInfo.Ips}����¼ʱ�䣺{moUserInfo.LoginTime.ToString("yyyy-MM-dd HH:mm")}",
                            UserId = userInfo.Id
                        });
                    }

                    _context.ToUserLog.Add(new ToUserLog
                    {
                        CodeId = (int)EmLogCode.����,
                        CreateTime = DateTime.Now,
                        Des = $"��ע�᡿  +{(int)EmLevelNum.ע��}",
                        UserId = userInfo.Id
                    });
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(HomeController.Index), "home");
                }
                #endregion

                this.MsgBox("ע��ʧ�ܣ����Ժ����ԡ�");
                return View(loginUser);
            }
            return View(loginUser);
        }

        //ע��
        [HttpGet]
        public IActionResult LoginOut()
        {
            HttpContext.Session.Remove(HttpContext.Session.SessionKey());
            return RedirectToAction(nameof(MemberController.Login));
        }

        #region �������� 
        //����dll��ʽ���ǵģ�������ϵͳ�û�������Ͳ�������÷ֲ�ʽ����ʽ


        public IActionResult ForgetPassword()
        {
            return View();
        }

        /// <summary>
        /// �ύ������������
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgetPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) { this.MsgBox("������"); return View(); }

            email = email.Trim().ToLower();
            if (email.Length >= 50 || email.Length <= 3)
            {
                this.MsgBox("���䳤�Ȳ�����"); return View();
            }
            else if (!email.Contains("@"))
            {
                this.MsgBox("�����ʽ����ȷ��"); return View();
            }
            var user = await _context.ToUserInfo.SingleOrDefaultAsync(b => b.Email.ToLower() == email);
            if (user == null) { this.MsgBox("�����ڸð�������˺ţ�"); return View(); }
            else if (user.Status == (int)EnumHelper.EmUserStatus.����)
            {
                this.MsgBox("�ð�������˺��ѱ����ã�����ͨ�������ʼ�����841202396@qq.com��ϵ�ͷ���"); return View();
            }

            var timeOut = 10;
            var now = DateTime.Now.AddMinutes(timeOut);
            var expires = now.ToString("yyyy-MM-dd HH:mm");
            var token = PublicClass._Md5($"{expires}-{email}-{Request.Host.Host}");
            var appUrl = $"http://{Request.Host.Host}:{Request.Host.Port}";
            var comfirmUrl = $"{appUrl}/member/confirmpassword?expire={expires}&token={token}&email={email}&t=0.{now.ToString("ssfff")}";

            //��ȡģ��
            var tpl = await PublicClass._GetHtmlTpl(EnumHelper.EmEmailTpl.MsgBox, _selfSetting.EmailTplPath);
            if (string.IsNullOrWhiteSpace(tpl)) { this.MsgBox("���Ͱ��ʼ�ʧ�ܣ����Ժ����ԡ�"); return View(); }

            tpl = tpl.Replace("{name}", "�𾴵��û�").
              Replace("{content}", $"������ʹ��<a href='{appUrl}'>����ͼ��</a>�����������빦�ܣ�������������ȷ�ϰ�����<a href='{comfirmUrl}'>{comfirmUrl}</a>��ע��õ�ַ��Чʱ��{timeOut}���ӡ�");
            //����
            var isOk = PublicClass._SendEmail(
               new Dictionary<string, string> {
                 { "�𾴵��û�",email}
               },
               "����ͼ - ��������",
               tpl);

            this.MsgBox(isOk ? "�Ѹ������䷢�������������ʼ������ռ����������������ӵ�ַ��" : "���Ͱ��ʼ�ʧ�ܣ����Ժ����ԣ�");

            return View();
        }

        /// <summary>
        /// ������������֪ͨ
        /// </summary>
        /// <returns></returns>
        public IActionResult ConfirmPassword(string expire, string token, string email, string t)
        {
            if (string.IsNullOrWhiteSpace(expire) || string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(email) || !email.Contains("@") || string.IsNullOrWhiteSpace(t))
            {
                return RedirectToAction(nameof(HomeController.Error), "home", new { msg = "��Ч������" });
            }
            else if (t.Length != 7)
            {
                return RedirectToAction(nameof(HomeController.Error), "home", new { msg = "��Ч������" });
            }

            email = email.Trim().ToLower();
            if (!DateTime.TryParse(expire, out var expires)) { return RedirectToAction(nameof(HomeController.Error), "home", new { msg = "��Ч������" }); }
            else if (expires.AddMinutes(30) < DateTime.Now)
            {
                return RedirectToAction(nameof(HomeController.Error), "home", new { msg = "�����ѹ��ڣ����²�����" });
            }

            var compareToken = PublicClass._Md5($"{expire}-{email}-{Request.Host.Host}");
            if (!token.Equals(compareToken)) { return RedirectToAction(nameof(HomeController.Error), "home", new { msg = "��֤ʧ�ܣ���Ч������" }); }

            var user = _context.ToUserInfo.SingleOrDefault(b => b.Email.ToLower() == email);
            if (user == null) { return RedirectToAction(nameof(HomeController.Error), "home", new { msg = "�����ڸð�������˺ţ�" }); }
            else if (user.Status == (int)EnumHelper.EmUserStatus.����)
            {
                return RedirectToAction(nameof(HomeController.Error), "home", new { msg = "�ð�������˺��ѱ����ã�����ͨ�������ʼ�����841202396@qq.com��ϵ�ͷ���" });
            }

            var key = $"checkConfirmPwd{email}";
            if (!_cache.TryGetValue<MoUserInfo>(key, out var result))
            {
                _cache.Set<MoUserInfo>(key, new MoUserInfo { Id = user.Id, Email = email }, TimeSpan.FromMinutes(10));
            }

            return View(new MoRegisterUser { UserName = email });
        }

        /// <summary>
        /// �ύ���õ�����
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPassword([Bind("UserName", "UserPwd", "ComfirmPwd")]MoRegisterUser registUser)
        {
            if (ModelState.IsValid)
            {
                if (string.IsNullOrWhiteSpace(registUser.UserPwd))
                {
                    this.MsgBox("���벻��Ϊ�գ�");
                    return View(registUser);
                }
                else if (string.IsNullOrWhiteSpace(registUser.ComfirmPwd))
                {
                    this.MsgBox("ȷ�����벻��Ϊ�գ�");
                    return View(registUser);
                }
                else if (registUser.UserPwd != registUser.ComfirmPwd)
                {
                    this.MsgBox("�����ȷ�����벻��ͬ��");
                    return View(registUser);
                }

                var key = $"checkConfirmPwd{registUser.UserName}";
                if (!_cache.TryGetValue<MoUserInfo>(key, out var checkUser))
                {
                    return RedirectToAction(nameof(HomeController.Error), "home", new { msg = "�����ѹ��ڣ����²�����" });
                }

                var user = _context.ToUserInfo.Where(b => b.Id == checkUser.Id && b.Email == checkUser.Email).SingleOrDefault();
                if (user == null)
                {
                    _cache.Remove(key);
                    return RedirectToAction(nameof(HomeController.Error), "home", new { msg = "���õ�����ʧ�ܣ����Ժ����ԣ�" });
                }

                user.UserPwd = PublicClass._Md5(registUser.UserPwd.Trim());
                var result = await _context.SaveChangesAsync();
                if (result > 0)
                {
                    _cache.Remove(key);
                    this.MsgBox("��������ɹ���");
                }
                else { this.MsgBox("��������ʧ�ܣ�"); }
            }
            return View(registUser);
        }

        #endregion
    }
}
