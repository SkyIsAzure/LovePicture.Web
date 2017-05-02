﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LovePicture.Web
{
    #region Controller扩展

    public static class ExtentionsClass
    {
        public static void MsgBox(this Controller controller, string msg, string key = "msgbox")
        {
            controller.ViewData[key] = msg;
        }

        #endregion

        #region ISession扩展

        public static string SessionKey(this ISession session)
        {
            return "MySession";
        }

        /// <summary>
        /// 设置session
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="session"></param>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public static bool Set<T>(this ISession session, string key, T val)
        {
            if (string.IsNullOrWhiteSpace(key) || val == null) { return false; }

            var strVal = JsonConvert.SerializeObject(val);
            var bb = Encoding.UTF8.GetBytes(strVal);
            session.Set(key, bb);
            return true;
        }

        /// <summary>
        /// 获取session
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="session"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static T Get<T>(this ISession session, string key)
        {
            var t = default(T);
            if (string.IsNullOrWhiteSpace(key)) { return t; }

            if (session.TryGetValue(key, out byte[] val))
            {
                var strVal = Encoding.UTF8.GetString(val);
                t = JsonConvert.DeserializeObject<T>(strVal);
            }
            return t;
        }

        #endregion

        #region 获取Ip

        public static string GetUserIp(this Controller controller)
        {
            return controller.HttpContext.Connection.RemoteIpAddress.ToString();
        }

        #endregion

        #region 格式化时间

        /// <summary>
        /// 获取据当前时间的时间间隔：如1年1月1日
        /// </summary>
        /// <param name="date"></param>
        /// <param name="yearNum"></param>
        /// <param name="monthNum"></param>
        /// <returns></returns>
        public static string FormatDateToNow(this DateTime date, int yearNum = 365, int monthNum = 31)
        {
            var subTime = DateTime.Now.Subtract(date);

            var dayNum = subTime.Days;

            var year = dayNum / yearNum;
            var month = dayNum % yearNum / monthNum;
            var day = dayNum % yearNum % monthNum;

            var str = year > 0 ? $"{year}年" : "";
            str += month > 0 ? $"{month}月" : "";
            str += day > 0 ? $"{day}天" : "1天";

            return str;
        }

        #endregion

        #region 格式化省略字符

        public static string FomartPhone(this string val, int startLen = 3, int endLen = 3)
        {
            if (string.IsNullOrWhiteSpace(val)) { return ""; }

            var len = val.Trim().Length;
            var start = string.Empty;
            var end = string.Empty;
            if (len > startLen) { start = val.Substring(0, startLen); } else { start = val; }
            if (len - endLen > startLen) { end = val.Substring(len - endLen, endLen); }
            return string.Format("{0}***{1}", start, end);
        }

        public static string FomartSubStr(this string val, int startLen = 20, string op = "...")
        {
            if (string.IsNullOrWhiteSpace(val)) { return ""; }
            val = val.Trim();
            if (val.Length < startLen) { return val; }
            else
            {
                val = val.Substring(0, startLen) + op;
            }
            return val;
        }

        #endregion

    }
}
