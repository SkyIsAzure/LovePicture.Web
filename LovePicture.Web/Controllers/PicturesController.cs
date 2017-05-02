using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using LovePicture.Model.Models;
using LovePicture.Web.Extends;
using LovePicture.Com;
using LovePicture.Model.MoClass;
using static LovePicture.Com.EnumHelper;
using Microsoft.EntityFrameworkCore;

namespace LovePicture.Web.Controllers
{
    public class PicturesController : Controller
    {
        private readonly LovePicture_DbContext _db;

        public PicturesController(LovePicture_DbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// ���ҳ
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public IActionResult Index(int? id)
        {
            try
            {
                #region  �������

                var page = id ?? 1;
                page = page <= 0 ? 1 : page;

                var pageOption = new MoPagerOption
                {
                    CurrentPage = page,
                    PageSize = 8,
                    Total = 0,
                    RouteUrl = $"/Pictures/Index",
                    StyleNum = 1,

                    JoinOperateCode = ""
                };
                #endregion

                var modules = _db.ToModule.Where(b => b.Status == (int)EnumHelper.EmModuleStatus.����).AsEnumerable();
                pageOption.Total = modules.Count();
                modules = modules.OrderByDescending(b => b.CreateTime).Skip((pageOption.CurrentPage - 1) * pageOption.PageSize).Take(pageOption.PageSize).ToList();

                ViewBag.PagerOption = pageOption;
                return View(modules);
            }
            catch (Exception ex)
            {
                return BadRequest();
            }
        }

        /// <summary>
        /// ָ����б�
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public IActionResult Pictures(string id)
        {
            try
            {
                #region  �������
                var paramArr = id.Split('-');
                if (paramArr.Length != 2) { return BadRequest(); }

                var page = Convert.ToInt32(paramArr[1]);
                var moduleId = Convert.ToInt32(paramArr[0]);

                var module = _db.ToModule.SingleOrDefault(b => b.Id == moduleId && b.Status == (int)EnumHelper.EmModuleStatus.����);
                if (module == null)
                {
                    return BadRequest();
                }
                page = page <= 0 ? 1 : page;

                var pageOption = new MoPagerOption
                {
                    CurrentPage = page,
                    PageSize = 16,
                    Total = 0,
                    RouteUrl = $"/Pictures/Pictures/{moduleId}",
                    StyleNum = 1,

                    JoinOperateCode = "-"
                };
                #endregion

                var contents = _db.ToContent.Where(b => b.ModuleId == moduleId && b.Status == (int)EnumHelper.EmContentStatus.����).AsEnumerable();
                pageOption.Total = contents.Count();
                contents = contents.OrderByDescending(b => b.CreateTime).Skip((pageOption.CurrentPage - 1) * pageOption.PageSize).Take(pageOption.PageSize).ToList();

                ViewData["module"] = module;
                ViewBag.PagerOption = pageOption;
                return View(contents);
            }
            catch (Exception ex)
            {
                return BadRequest();
            }
        }

        /// <summary>
        /// ���ӵ��޻��������
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<JsonResult> PicZanOrRead(int? id, int? tId)
        {
            var data = new MoLoveData();
            try
            {
                if (id == null || id <= 0 ||
                    tId == null || tId <= 0) { data.Msg = "����ʧ�ܣ�"; return Json(data); }

                var user = HttpContext.Session.Get<MoUserInfo>(HttpContext.Session.SessionKey());
                if (user == null) { data.Msg = "���ȵ�¼��"; return Json(data); }

                var content = _db.ToContent.SingleOrDefault(b => b.Id == id && b.Status == (int)EnumHelper.EmContentStatus.����);
                if (content == null) { data.Msg = "����ʧ�ܡ�"; return Json(data); }

                if (content.UserId == user.Id) { data.Msg = "���ܲ����Լ���ͼ"; return Json(data); }

                if (tId == 1)
                {
                    //����
                    content.ZanNum += 1;
                    data.Data = content.ZanNum;
                    if (content.ZanNum >= 1000000) { data.Msg = "�����ɹ���"; data.IsOk = true; return Json(data); }
                }
                else if (tId == 2)
                {
                    //���
                    content.ReadNum += 1;
                    data.Data = content.ReadNum;
                    if (content.ReadNum >= 1000000) { data.Msg = "�����ɹ���"; data.IsOk = true; return Json(data); }
                }

                data.IsOk = await _db.SaveChangesAsync() > 0;
                data.Msg = data.IsOk ? "�����ɹ���" : "����ʧ�ܣ�";
                if (data.IsOk && tId == 1)
                {
                    //���ӻ���
                    _db.ToUserLog.Add(new ToUserLog
                    {
                        CodeId = (int)EmLogCode.����,
                        CreateTime = DateTime.Now,
                        Des = $"ͼ��{content.Name.FomartSubStr(15)}���������ޡ�  +{(int)EmLevelNum.����}",
                        UserId = user.Id
                    });
                    var dbUser = _db.ToUserInfo.SingleOrDefault(b => b.Id == user.Id);
                    dbUser.LevelNum += (int)EmLevelNum.����;

                    var result = await _db.SaveChangesAsync();
                    if (result > 0) { user.LevelNum = dbUser.LevelNum; }
                }
            }
            catch (Exception ex)
            {
            }
            return Json(data);
        }

        /// <summary>
        /// �鿴ͼƬ
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<IActionResult> PicView(int? id)
        {
            try
            {
                if (id == null) { return BadRequest(); }

                var content = _db.ToContent.Include(b => b.ToContentFiles).SingleOrDefault(b => b.Id == id && b.Status != (int)EnumHelper.EmContentStatus.ɾ��);
                if (content == null) { return BadRequest(); }

                var user = HttpContext.Session.Get<MoUserInfo>(HttpContext.Session.SessionKey());
                if (content.Status == (int)EnumHelper.EmContentStatus.˽��)
                {
                    if (user == null || user.Id != content.UserId) { return BadRequest(); }
                }

                if (content.ReadNum <= 1000000)
                {
                    if (user != null)
                    {
                        if (user.Id != content.UserId) { content.ReadNum += 1; await _db.SaveChangesAsync(); }
                    }
                    else { content.ReadNum += 1; await _db.SaveChangesAsync(); }
                }

                return View(content);
            }
            catch (Exception ex)
            {
                return BadRequest();
            }
        }
    }
}