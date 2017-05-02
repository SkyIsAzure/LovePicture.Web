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

        #region ��������

        public IActionResult Index()
        {

            return View();
        }

        #region �û���¼��־
        /// <summary>
        /// ��¼��־�б�  page-codeId
        /// </summary>
        /// <returns></returns>
        public IActionResult UserLogs(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) { return BadRequest(); }

            #region  �������
            var paramArr = id.Split('-');
            if (paramArr.Length != 2) { return BadRequest(); }
            var page = Convert.ToInt32(paramArr[1]);
            var codeId = Convert.ToInt32(paramArr[0]);
            if (codeId != (int)EnumHelper.EmLogCode.��¼ && codeId != (int)EnumHelper.EmLogCode.����)
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
                Des = $"{Enum.GetName(typeof(EnumHelper.EmLogCode), codeId)}��¼"
            };
            ViewData["userLog"] = userLog;

            return View(userLogs);
        }

        /// <summary>
        /// ��ȡ�û���¼��־
        /// </summary>
        /// <param name="codeNum">��־����</param>
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

        #region ͳ����Ϣ

        [HttpPost]
        public JsonResult UserStatis()
        {
            var data = new MoLoveData();
            var list = new List<dynamic>();

            //��ͼ�������У�
            var userContent = _db.ToContent.Where(b => b.UserId == _MyUserInfo.Id).AsEnumerable();
            var total1 = userContent.Count(b => b.Status == (int)EnumHelper.EmContentStatus.����);
            list.Add(new
            {
                name = "��ͼ�������У�",
                total = $"{ total1}���ţ�"
            });
            //��ͼ����˽�У�
            var total2 = userContent.Count(b => b.Status == (int)EnumHelper.EmContentStatus.˽��);
            list.Add(new
            {
                name = "��ͼ����˽�У�",
                total = $"{ total2}���ţ�"
            });
            //������
            var total3 = userContent.Where(b => b.Status != (int)EnumHelper.EmContentStatus.ɾ��).Sum(b => b.ZanNum);
            list.Add(new
            {
                name = "������",
                total = $"{ total3}������"
            });
            //�����
            var total4 = userContent.Where(b => b.Status != (int)EnumHelper.EmContentStatus.ɾ��).Sum(b => b.ReadNum);
            list.Add(new
            {
                name = "�����",
                total = $"{ total4}���Σ�"
            });
            //���Ļ���
            var total5 = _MyUserInfo.LevelNum;
            list.Add(new
            {
                name = "���Ļ���",
                total = $"{ total5}���֣�"
            });
            data.Data = list;
            data.IsOk = true;
            return Json(data);
        }

        #endregion

        #region �ϴ���¼

        [HttpPost]
        public JsonResult UserUp()
        {
            var data = new MoLoveData();
            //��ͼ�������У�
            var userContent = _db.ToContent.Where(b => b.UserId == _MyUserInfo.Id &&
                                     b.Status != (int)EnumHelper.EmContentStatus.ɾ��).
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

        #region �˻�����

        public IActionResult AccountSettings()
        {

            return View(_MyUserInfo);
        }

        #region �޸�ͷ��
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
            if (file == null) { this.MsgBox("��ѡ���ϴ���ͷ��ͼƬ��"); return View(_MyUserInfo); }

            var maxSize = 1024 * 1024 * 4;
            if (file.Length > maxSize)
            {
                this.MsgBox("ͷ��ͼƬ���ܴ���4M��"); return View(_MyUserInfo);
            }

            var fileExtend = file.FileName.Substring(file.FileName.LastIndexOf('.'));
            var fileNewName = $"{DateTime.Now.ToString("yyyyMMddHHmmssfff")}{fileExtend}";
            var path = Path.Combine(_selfSetting.UpHeadPhotoPath, fileNewName);
            using (var stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                await file.CopyToAsync(stream);
            }

            //��������
            var viewPath = $"{_selfSetting.ViewHeadPhotoPath}/{fileNewName}";

            var user = _db.ToUserInfo.Where(b => b.Id == _MyUserInfo.Id).SingleOrDefault();
            if (user == null) { this.MsgBox("�ϴ�ʧ�ܣ����Ժ����ԣ�"); return View(_MyUserInfo); }
            user.HeadPhoto = viewPath;
            user.LevelNum += (int)EmLevelNum.�޸�ͷ��;
            var result = await _db.SaveChangesAsync();
            if (result > 0)
            {
                _MyUserInfo.HeadPhoto = viewPath;
                _MyUserInfo.LevelNum = user.LevelNum;
                HttpContext.Session.Set<MoUserInfo>(HttpContext.Session.SessionKey(), _MyUserInfo);
                this.MsgBox("�ϴ��ɹ���");

                _db.ToUserLog.Add(new ToUserLog
                {
                    CodeId = (int)EmLogCode.����,
                    CreateTime = DateTime.Now,
                    Des = $"���޸�ͷ��  +{(int)EmLevelNum.�޸�ͷ��}",
                    UserId = _MyUserInfo.Id
                });
                await _db.SaveChangesAsync();

            }
            else { this.MsgBox("�ϴ�ʧ�ܣ����Ժ����ԣ�"); }
            return View(_MyUserInfo);
        }
        #endregion

        #region �޸Ļ�����Ϣ

        public IActionResult ModifyUser()
        {
            return View(_MyUserInfo);
        }

        [HttpPost]
        public async Task<IActionResult> ModifyUser(MoUserInfo moUserInfo)
        {
            if (moUserInfo.Id <= 0)
            {
                this.MsgBox("�޸�ʧ�ܣ����Ժ����ԡ�");
                return View(_MyUserInfo);
            }
            else if (string.IsNullOrWhiteSpace(moUserInfo.NickName))
            {
                this.MsgBox("�ǳƲ���Ϊ�գ�");
                return View(_MyUserInfo);
            }

            _MyUserInfo.NickName = moUserInfo.NickName;
            _MyUserInfo.Tel = moUserInfo.Tel;
            _MyUserInfo.Sex = moUserInfo.Sex;
            _MyUserInfo.Birthday = moUserInfo.Birthday;

            _MyUserInfo.Blog = moUserInfo.Blog;
            _MyUserInfo.Introduce = moUserInfo.Introduce;

            var user = _db.ToUserInfo.Where(b => b.Id == _MyUserInfo.Id).SingleOrDefault();
            if (user == null) { this.MsgBox("�޸�ʧ�ܣ����Ժ�����"); return View(_MyUserInfo); }

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
                this.MsgBox("�޸ĳɹ���");
            }
            else { this.MsgBox("�޸�ʧ�ܣ����Ժ����ԣ�"); }

            return View(_MyUserInfo);
        }

        #endregion

        #region �޸�����

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
                if (user == null) { this.MsgBox("�޸�ʧ�ܣ����Ժ�����"); return View(moRegisterUser); }
                user.UserPwd = PublicClass._Md5(moRegisterUser.UserPwd.Trim());
                var result = await _db.SaveChangesAsync();
                if (result > 0)
                {
                    HttpContext.Session.Set<MoUserInfo>(HttpContext.Session.SessionKey(), _MyUserInfo);
                    this.MsgBox("�޸ĳɹ���");
                }
                else { this.MsgBox("�޸�ʧ�ܣ����Ժ����ԣ�"); }
            }
            return View(moRegisterUser);
        }

        #endregion

        #endregion

        #region �ҵ����

        public IActionResult Modules()
        {
            var modules = _db.ToModule.Where(b => b.Status == (int)EnumHelper.EmModuleStatus.����).OrderByDescending(b => b.CreateTime).AsQueryable();
            return View(modules);
        }

        public IActionResult Module(string id)
        {

            #region  �������
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

            var contents = _db.ToContent.Where(b => b.UserId == _MyUserInfo.Id && b.ModuleId == moduleId && b.Status != (int)EnumHelper.EmContentStatus.ɾ��).AsEnumerable();
            pageOption.Total = contents.Count();
            contents = contents.OrderByDescending(b => b.CreateTime).Skip((pageOption.CurrentPage - 1) * pageOption.PageSize).Take(pageOption.PageSize).ToList();

            ViewData["module"] = module;
            ViewBag.PagerOption = pageOption;
            return View(contents);
        }

        #endregion

        #region �����ϴ�

        public IActionResult UpPhoto(int? id)
        {
            var content = new ToContent { };
            var module = _db.ToModule.Where(b => b.Status == (int)EnumHelper.EmModuleStatus.���� && b.Id == id).SingleOrDefault();
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
                var module = _db.ToModule.Where(b => b.Status == (int)EnumHelper.EmModuleStatus.���� && b.Id == content.ModuleId).SingleOrDefault();
                if (module == null) { return BadRequest(); }
                ViewData["module"] = module;

                if (string.IsNullOrWhiteSpace(content.Name)) { this.MsgBox("�������Ʊ��"); return View(content); }

                //ͼƬ
                var files = Request.Form.Files.Where(b =>
                      b.Name == "myPhoto" &&
                      b.ContentType.Contains("image")).AsEnumerable();

                var size = 1024 * 1024;
                var maxNum = 10;
                var maxSize = size * maxNum;
                var maxSingleNum = 4;
                var maxSingleSize = size * maxSingleNum;

                if (files == null) { this.MsgBox("��ѡ���ϴ���ͼƬ��"); return View(content); }
                else if (files.Count() >= 11) { this.MsgBox("ÿ���ϴ�ͼƬ���������ܳ���10�ţ�"); return View(content); }
                else if (files.Sum(b => b.Length) >= maxSize) { this.MsgBox($"ÿ���ϴ�ͼƬ�Ĵ�С���ܳ���{maxNum}M��"); return View(content); }
                else if (files.Any(b => b.Length >= maxSingleSize)) { this.MsgBox($"����ͼƬ�Ĵ�С���ܳ���{maxSingleNum}M��"); return View(content); }

                //������С������һ��
                var i = 1;
                var file = files.OrderBy(b => b.Length).FirstOrDefault();
                var fileExtend = file.FileName.Substring(file.FileName.LastIndexOf('.'));
                var fileNewName = $"{DateTime.Now.ToString("yyyyMMddHHmmssfff")}{i}{fileExtend}";
                var path = Path.Combine(_selfSetting.UpContentPhotoPath, fileNewName);
                using (var stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    await file.CopyToAsync(stream);
                }

                //��������
                content.MaxPic = $"{_selfSetting.ViewContentPhotoPath}/{fileNewName}";
                content.UserId = _MyUserInfo.Id;
                content.CreateTime = DateTime.Now;

                _db.Add(content);

                //����
                _db.ToUserLog.Add(new ToUserLog
                {
                    CodeId = (int)EmLogCode.����,
                    CreateTime = DateTime.Now,
                    Des = $"���ϴ�ͼƬ��  +{(int)EmLevelNum.�ϴ�ͼƬ}",
                    UserId = _MyUserInfo.Id
                });

                var dbUser = _db.ToUserInfo.SingleOrDefault(b => b.Id == _MyUserInfo.Id);
                dbUser.LevelNum += (int)EmLevelNum.�ϴ�ͼƬ;

                //��һ�ű������ļ�����
                _db.ToContentFiles.Add(new ToContentFiles
                {
                    ContentId = content.Id,
                    MaxPic = content.MaxPic,
                    MinPic = content.MinPic,
                    ZanNum = content.ZanNum
                });

                //��������ͼƬ���ļ�����
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
                else { this.MsgBox("����ʧ�ܣ����Ժ����ԣ�"); }

            }
            return View(content);
        }

        #endregion

        #region ��ȫ����

        public IActionResult AnQuanSettings()
        {

            return View();
        }

        #region  ��������

        public IActionResult SettingEmail()
        {
            return View();
        }

        //�����ʼ�
        [HttpPost]
        public async Task<IActionResult> SettingEmail(string email)
        {

            if (string.IsNullOrWhiteSpace(email)) { this.MsgBox("������"); return View(); }

            email = email.Trim();
            if (email.Length >= 50 || email.Length <= 3)
            {
                this.MsgBox("���䳤�Ȳ�����"); return View();
            }
            else if (!email.Contains("@"))
            {
                this.MsgBox("�����ʽ����ȷ��"); return View();
            }

            var timeOut = 30;
            var now = DateTime.Now.AddMinutes(timeOut);
            var expires = now.ToString("yyyy-MM-dd HH:mm");
            var token = PublicClass._Md5($"{expires}-{email}-{Request.Host.Host}-{_MyUserInfo.Id}");
            var appUrl = $"http://{Request.Host.Host}:{Request.Host.Port}";
            var comfirmUrl = $"{appUrl}/usercenter/confirmsettingemail?expire={expires}&token={token}&email={email}&t=0.9527{_MyUserInfo.Id}";

            //��ȡģ��
            var tpl = await PublicClass._GetHtmlTpl(EnumHelper.EmEmailTpl.SettingEmail, _selfSetting.EmailTplPath);
            if (string.IsNullOrWhiteSpace(tpl)) { this.MsgBox("���Ͱ��ʼ�ʧ�ܣ����Ժ����ԡ�"); return View(); }

            tpl = tpl.Replace("{name}", _MyUserInfo.NickName).
              Replace("{content}", $"������ʹ��<a href='{appUrl}'>����ͼ��</a>����󶨹��ܣ�������������ȷ�ϰ�����<a href='{comfirmUrl}'>{comfirmUrl}</a>��ע��õ�ַ��Чʱ��{timeOut}���ӡ�");
            //����
            var isOk = PublicClass._SendEmail(
               new Dictionary<string, string> {
                 { _MyUserInfo.NickName,email}
               },
               "����ͼ - ������",
               tpl);

            this.MsgBox(isOk ? "�Ѹ������䷢���˰�ȷ���ʼ������ռ�����ȷ�ϰ����ӵ�ַ��" : "���Ͱ��ʼ�ʧ�ܣ����Ժ����ԣ�");
            return View();
        }

        //ȷ�ϰ�
        public async Task<IActionResult> ConfirmSettingEmail(string expire, string token, string email, string t)
        {
            if (string.IsNullOrWhiteSpace(expire) || string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(email) || !email.Contains("@") || string.IsNullOrWhiteSpace(t))
            {
                return RedirectToAction(nameof(HomeController.Error), "home", new { msg = "��Ч������" });
            }

            if (!DateTime.TryParse(expire, out var expires)) { return RedirectToAction(nameof(HomeController.Error), "home", new { msg = "��Ч������" }); }
            else if (expires.AddMinutes(30) < DateTime.Now)
            {
                return RedirectToAction(nameof(HomeController.Error), "home", new { msg = "�����ѹ��ڣ����²�����" });
            }
            t = t.Replace("0.9527", "");
            var compareToken = PublicClass._Md5($"{expire}-{email}-{Request.Host.Host}-{t}");
            if (!token.Equals(compareToken)) { return RedirectToAction(nameof(HomeController.Error), "home", new { msg = "��֤ʧ�ܣ���Ч������" }); }

            var uid = Convert.ToInt32(t);
            if (uid != _MyUserInfo.Id) { return RedirectToAction(nameof(HomeController.Error), "home", new { msg = "��֤ʧ�ܣ����¼�����°󶨣�" }); }

            //����
            if (_db.ToUserInfo.Any(b => b.Email == email))
            {
                return RedirectToAction(nameof(HomeController.Error), "home", new { msg = "��ʧ�ܣ��������ѱ�ʹ�ù��ˡ�" });
            }

            var user = _db.ToUserInfo.Where(b => b.Id == uid && b.Status == (int)EnumHelper.EmUserStatus.����).SingleOrDefault();
            if (user == null)
            {
                return RedirectToAction(nameof(HomeController.Error), "home", new { msg = "��ʧ�ܣ���Ч������" });
            }
            user.Email = email;
            user.LevelNum += (int)EmLevelNum.������;

            _db.ToUserLog.Add(new ToUserLog
            {
                CodeId = (int)EmLogCode.����,
                CreateTime = DateTime.Now,
                Des = $"�������䡿  +{(int)EmLevelNum.������}",
                UserId = user.Id
            });
            var result = await _db.SaveChangesAsync();
            if (result > 0)
            {
                this.MsgBox("������ɹ���");
                _MyUserInfo.Email = email;
                //����ǵ�½״̬����Ҫ����session
                HttpContext.Session.Set<MoUserInfo>(HttpContext.Session.SessionKey(), _MyUserInfo);
            }
            else { this.MsgBox("��ʧ�ܣ��������ԣ�"); }
            return View();
        }

        #endregion

        #endregion
    }
}